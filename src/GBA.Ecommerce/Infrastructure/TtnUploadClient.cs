#nullable enable

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GBA.Services.Infrastructure;
using GBA.Services.Infrastructure.SalesMutations;
using Microsoft.AspNetCore.Http;

namespace GBA.Ecommerce.Infrastructure;

/// <summary>Contains the intended public CRM URL and digest for a staged customer TTN file.</summary>
public sealed record TtnUploadResult(string Url, byte[] Sha256);

/// <summary>Represents a rejected or failed internal TTN operation.</summary>
public sealed class TtnUploadHttpException : Exception {
    /// <summary>Creates a TTN operation exception for the supplied status.</summary>
    public TtnUploadHttpException(HttpStatusCode statusCode, string message)
        : base(message) => StatusCode = statusCode;

    /// <summary>Gets the status returned by or mapped from the internal operation.</summary>
    public HttpStatusCode StatusCode { get; }
}

/// <summary>Stages, finalizes, and aborts customer TTN files through the internal sales endpoint.</summary>
public sealed class TtnUploadClient {
    /// <summary>Gets the maximum accepted TTN file size.</summary>
    public const long MaxFileSizeBytes = 50L * 1024 * 1024;

    /// <summary>Gets the request limit including multipart framing overhead.</summary>
    public const long MaxRequestSizeBytes = MaxFileSizeBytes + 64 * 1024;

    /// <summary>Gets the maximum amount buffered in memory before using disk.</summary>
    public const int MemoryBufferThresholdBytes = 64 * 1024;

    private const int _bufferSize = 81920;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Uri _allowedInternalBaseUri;
    private readonly string _internalApiKey;
    private readonly TimeSpan _requestTimeout;

    /// <summary>Creates a TTN client from the existing sales mutation configuration.</summary>
    public TtnUploadClient(
        IHttpClientFactory httpClientFactory,
        SalesMutationOutboxOptions options,
        SalesMutationInternalAuthOptions internalAuthOptions) {
        _httpClientFactory = httpClientFactory ??
            throw new ArgumentNullException(nameof(httpClientFactory));
        _allowedInternalBaseUri = (options ??
            throw new ArgumentNullException(nameof(options)))
            .GetValidatedAllowedInternalBaseUri();
        if (options.RequestTimeout <= TimeSpan.Zero)
            throw new InvalidOperationException(
                $"{nameof(SalesMutationOutboxOptions.RequestTimeout)} must be positive.");
        _requestTimeout = options.RequestTimeout;
        _internalApiKey = (internalAuthOptions ??
            throw new ArgumentNullException(nameof(internalAuthOptions)))
            .GetValidatedApiKey();
    }

    /// <summary>Stages one validated file outside the public CRM file tree.</summary>
    public async Task<TtnUploadResult> StageAsync(
        IFormFile file,
        Guid operationNetUid,
        CultureInfo culture,
        CancellationToken cancellationToken = default) {
        ValidateCommon(operationNetUid, culture);
        ArgumentNullException.ThrowIfNull(file);
        if (file.Length <= 0)
            throw new TtnUploadHttpException(
                HttpStatusCode.BadRequest,
                "A non-empty TTN file is required.");
        if (file.Length > MaxFileSizeBytes)
            throw new TtnUploadHttpException(
                HttpStatusCode.RequestEntityTooLarge,
                "The TTN file exceeds the 50 MiB limit.");
        if (!HasAllowedExtension(file.FileName))
            throw new TtnUploadHttpException(
                HttpStatusCode.UnsupportedMediaType,
                "Only PDF, JPEG, and PNG TTN files are allowed.");

        string bufferPath = Path.Combine(
            Path.GetTempPath(),
            $"gba-ttn-upload-{Guid.NewGuid():N}.tmp");
        await using FileStream bufferedFile = new(
            bufferPath,
            new FileStreamOptions {
                Mode = FileMode.CreateNew,
                Access = FileAccess.ReadWrite,
                Share = FileShare.None,
                BufferSize = _bufferSize,
                Options = FileOptions.Asynchronous |
                          FileOptions.SequentialScan |
                          FileOptions.DeleteOnClose
            });

        byte[] digest = await BufferAndHashAsync(
            file,
            bufferedFile,
            cancellationToken);
        bufferedFile.Position = 0;

        using StreamContent fileContent = new(bufferedFile);
        using MultipartFormDataContent formData = new();
        formData.Add(fileContent, "file", file.FileName);
        using HttpRequestMessage request = CreateRequest(
            operationNetUid,
            culture,
            "stage");
        request.Content = formData;
        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        string uploadedUrl = await ReadSuccessfulUrlAsync(response, cancellationToken);
        return new TtnUploadResult(uploadedUrl, digest);
    }

    /// <summary>Compatibility alias for staging a TTN file.</summary>
    public Task<TtnUploadResult> UploadAsync(
        IFormFile file,
        Guid operationNetUid,
        CultureInfo culture,
        CancellationToken cancellationToken = default) =>
        StageAsync(file, operationNetUid, culture, cancellationToken);

    /// <summary>Publishes previously staged content after sale creation commits or replays.</summary>
    public async Task<string> FinalizeAsync(
        Guid operationNetUid,
        CultureInfo culture,
        CancellationToken cancellationToken = default) {
        ValidateCommon(operationNetUid, culture);
        using HttpRequestMessage request = CreateRequest(
            operationNetUid,
            culture,
            "finalize");
        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        return await ReadSuccessfulUrlAsync(response, cancellationToken);
    }

    /// <summary>Removes unpublished staged bytes while preserving the durable digest binding.</summary>
    public async Task AbortAsync(
        Guid operationNetUid,
        CultureInfo culture,
        CancellationToken cancellationToken = default) {
        ValidateCommon(operationNetUid, culture);
        using HttpRequestMessage request = CreateRequest(
            operationNetUid,
            culture,
            "abort");
        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        EnsureSuccess(response);
    }

    private HttpRequestMessage CreateRequest(
        Guid operationNetUid,
        CultureInfo culture,
        string phase) {
        Uri requestUri = new(
            _allowedInternalBaseUri,
            $"/api/v1/{Uri.EscapeDataString(culture.Name)}/sales/save/ttn?phase={phase}");
        HttpRequestMessage request = new(HttpMethod.Post, requestUri);
        SalesMutationRequestKey.AddTo(request, operationNetUid);
        request.Headers.TryAddWithoutValidation(
            SalesMutationInternalAuthOptions.HeaderName,
            _internalApiKey);
        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) {
        using CancellationTokenSource timeout =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_requestTimeout);
        using HttpClient httpClient = _httpClientFactory.CreateClient(
            SalesMutationOutboxDispatcher.HttpClientName);
        return await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseContentRead,
            timeout.Token);
    }

    private static async Task<string> ReadSuccessfulUrlAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken) {
        EnsureSuccess(response);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        string uploadedUrl = NormalizeUrl(responseBody);
        if (string.IsNullOrWhiteSpace(uploadedUrl))
            throw new TtnUploadHttpException(
                HttpStatusCode.BadGateway,
                "The TTN operation returned an empty file URL.");
        return uploadedUrl;
    }

    private static void EnsureSuccess(HttpResponseMessage response) {
        if (!response.IsSuccessStatusCode)
            throw new TtnUploadHttpException(
                response.StatusCode,
                $"TTN operation failed with HTTP {(int)response.StatusCode}.");
    }

    private static void ValidateCommon(
        Guid operationNetUid,
        CultureInfo culture) {
        ArgumentNullException.ThrowIfNull(culture);
        if (operationNetUid == Guid.Empty)
            throw new ArgumentException(
                "A non-empty TTN upload operation key is required.",
                nameof(operationNetUid));
        if (string.IsNullOrWhiteSpace(culture.Name))
            throw new ArgumentException("A route culture is required.", nameof(culture));
    }

    private static bool HasAllowedExtension(string fileName) {
        string normalizedFileName = (fileName ?? string.Empty).Replace('\\', '/');
        string extension = Path.GetExtension(Path.GetFileName(normalizedFileName))
            .ToLowerInvariant();
        return extension is ".pdf" or ".jpg" or ".jpeg" or ".png";
    }

    private static async Task<byte[]> BufferAndHashAsync(
        IFormFile file,
        FileStream destination,
        CancellationToken cancellationToken) {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using Stream source = file.OpenReadStream();
        byte[] buffer = new byte[_bufferSize];
        long totalLength = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(
                   buffer.AsMemory(0, buffer.Length),
                   cancellationToken)) > 0) {
            totalLength += bytesRead;
            if (totalLength > MaxFileSizeBytes)
                throw new TtnUploadHttpException(
                    HttpStatusCode.RequestEntityTooLarge,
                    "The TTN file exceeds the 50 MiB limit.");
            hash.AppendData(buffer, 0, bytesRead);
            await destination.WriteAsync(
                buffer.AsMemory(0, bytesRead),
                cancellationToken);
        }

        if (totalLength != file.Length)
            throw new TtnUploadHttpException(
                HttpStatusCode.BadRequest,
                "The TTN file length changed while it was buffered.");
        await destination.FlushAsync(cancellationToken);
        return hash.GetHashAndReset();
    }

    private static string NormalizeUrl(string responseBody) {
        string value = responseBody.Trim();
        if (value.Length < 2 || value[0] != '"') return value;

        try {
            return JsonSerializer.Deserialize<string>(value)?.Trim() ?? string.Empty;
        } catch (JsonException) {
            return value;
        }
    }
}
