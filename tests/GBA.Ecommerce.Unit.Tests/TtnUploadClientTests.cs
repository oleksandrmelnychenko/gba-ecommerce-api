using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using GBA.Ecommerce.Infrastructure;
using GBA.Services.Infrastructure;
using GBA.Services.Infrastructure.SalesMutations;
using Microsoft.AspNetCore.Http;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class TtnUploadClientTests {
    private const string InternalApiKey =
        "test-only-ecommerce-internal-api-key-0123456789abcdef";

    [Fact]
    public async Task AllPhasesUseValidatedOriginNamedClientAndRequiredHeaders() {
        Guid operationNetUid = Guid.NewGuid();
        byte[] content = Encoding.UTF8.GetBytes("ttn-pdf-content");
        RecordingHandler handler = new(HttpStatusCode.OK);
        RecordingHttpClientFactory factory = new(handler);
        TtnUploadClient client = CreateClient(factory);

        TtnUploadResult result = await client.StageAsync(
            CreateFile("invoice.pdf", content),
            operationNetUid,
            CultureInfo.GetCultureInfo("uk"));
        string finalizedUrl = await client.FinalizeAsync(
            operationNetUid,
            CultureInfo.GetCultureInfo("uk"));
        await client.AbortAsync(
            operationNetUid,
            CultureInfo.GetCultureInfo("uk"));

        Assert.Equal([
            "https://crm.internal.example:8443/api/v1/uk/sales/save/ttn?phase=stage",
            "https://crm.internal.example:8443/api/v1/uk/sales/save/ttn?phase=finalize",
            "https://crm.internal.example:8443/api/v1/uk/sales/save/ttn?phase=abort"
        ], handler.Requests.Select(request => request.RequestUri.AbsoluteUri));
        Assert.All(handler.Requests, request => {
            Assert.Equal(operationNetUid.ToString("D"), request.IdempotencyKey);
            Assert.Equal(InternalApiKey, request.InternalApiKey);
        });
        Assert.Equal(3, factory.RequestedClientNames.Count);
        Assert.All(factory.RequestedClientNames, name =>
            Assert.Equal(SalesMutationOutboxDispatcher.HttpClientName, name));
        Assert.Equal(SHA256.HashData(content), result.Sha256);
        Assert.Contains(content, handler.Requests[0].RequestBody);
        Assert.Equal("https://crm.example/Data/Temp/file.pdf", result.Url);
        Assert.Equal(result.Url, finalizedUrl);
    }

    [Fact]
    public async Task RedirectResponseIsNotFollowedOrAccepted() {
        RecordingHandler handler = new(HttpStatusCode.Redirect) {
            RedirectLocation = new Uri("https://untrusted.example/upload")
        };
        RecordingHttpClientFactory factory = new(handler);
        TtnUploadClient client = CreateClient(factory);

        TtnUploadHttpException exception = await Assert.ThrowsAsync<TtnUploadHttpException>(() =>
            client.UploadAsync(
                CreateFile("invoice.pdf", Encoding.UTF8.GetBytes("content")),
                Guid.NewGuid(),
                CultureInfo.GetCultureInfo("uk")));

        Assert.Equal(HttpStatusCode.Redirect, exception.StatusCode);
        Assert.Single(handler.Requests);
        Assert.Equal(
            SalesMutationOutboxDispatcher.HttpClientName,
            Assert.Single(factory.RequestedClientNames));
    }

    [Fact]
    public async Task OversizedOrDisallowedFileIsRejectedBeforeHttpRequest() {
        RecordingHandler handler = new(HttpStatusCode.OK);
        RecordingHttpClientFactory factory = new(handler);
        TtnUploadClient client = CreateClient(factory);
        IFormFile oversized = new FormFile(
            new MemoryStream([0x25]),
            0,
            TtnUploadClient.MaxFileSizeBytes + 1,
            "file",
            "invoice.pdf");

        TtnUploadHttpException oversizedException =
            await Assert.ThrowsAsync<TtnUploadHttpException>(() =>
                client.StageAsync(
                    oversized,
                    Guid.NewGuid(),
                    CultureInfo.GetCultureInfo("uk")));
        TtnUploadHttpException extensionException =
            await Assert.ThrowsAsync<TtnUploadHttpException>(() =>
                client.StageAsync(
                    CreateFile("invoice.svg", Encoding.UTF8.GetBytes("<svg/>")),
                    Guid.NewGuid(),
                    CultureInfo.GetCultureInfo("uk")));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, oversizedException.StatusCode);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, extensionException.StatusCode);
        Assert.Empty(factory.RequestedClientNames);
        Assert.Empty(handler.Requests);
    }

    private static TtnUploadClient CreateClient(IHttpClientFactory factory) =>
        new(
            factory,
            new SalesMutationOutboxOptions {
                AllowedInternalBaseUri = "https://crm.internal.example:8443/",
                RequestTimeout = TimeSpan.FromSeconds(5)
            },
            new SalesMutationInternalAuthOptions { ApiKey = InternalApiKey });

    private static IFormFile CreateFile(string fileName, byte[] content) =>
        new FormFile(new MemoryStream(content), 0, content.Length, "file", fileName);

    private sealed class RecordingHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory {
        public List<string> RequestedClientNames { get; } = [];

        public HttpClient CreateClient(string name) {
            RequestedClientNames.Add(name);
            return new HttpClient(handler, disposeHandler: false);
        }
    }

    private sealed class RecordingHandler(HttpStatusCode statusCode) : HttpMessageHandler {
        public List<RecordedRequest> Requests { get; } = [];
        public Uri? RedirectLocation { get; init; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            string idempotencyKey = request.Headers
                .GetValues(SalesMutationRequestKey.HeaderName)
                .Single();
            string internalApiKey = request.Headers
                .GetValues(SalesMutationInternalAuthOptions.HeaderName)
                .Single();
            byte[] requestBody = request.Content == null
                ? []
                : await request.Content.ReadAsByteArrayAsync(cancellationToken);
            Requests.Add(new RecordedRequest(
                request.RequestUri!,
                idempotencyKey,
                internalApiKey,
                requestBody));
            HttpResponseMessage response = new(statusCode) {
                Content = new StringContent(
                    statusCode == HttpStatusCode.OK
                        ? "https://crm.example/Data/Temp/file.pdf"
                        : string.Empty)
            };
            response.Headers.Location = RedirectLocation;
            return response;
        }
    }

    private sealed record RecordedRequest(
        Uri RequestUri,
        string IdempotencyKey,
        string InternalApiKey,
        byte[] RequestBody);
}
