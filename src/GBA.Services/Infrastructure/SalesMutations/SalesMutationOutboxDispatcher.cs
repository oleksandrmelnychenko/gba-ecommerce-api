using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GBA.Services.Infrastructure.SalesMutations;

public sealed class SalesMutationOutboxDispatcher : ISalesMutationOutboxDispatcher {
    public const string HttpClientName = "SalesMutationOutbox";

    private const string _polishRequestPath = "/api/v1/pl/sales/update/ecommerce";
    private const string _ukrainianRequestPath = "/api/v1/uk/sales/update/ecommerce";
    private const string _polishTtnFinalizePath = "/api/v1/pl/sales/save/ttn?phase=finalize";
    private const string _ukrainianTtnFinalizePath = "/api/v1/uk/sales/save/ttn?phase=finalize";

    private readonly ISalesMutationOutboxStore _store;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SalesMutationOutboxOptions _options;
    private readonly Uri _allowedInternalBaseUri;
    private readonly string _internalApiKey;
    private readonly TimeProvider _timeProvider;

    public SalesMutationOutboxDispatcher(
        ISalesMutationOutboxStore store,
        IHttpClientFactory httpClientFactory,
        SalesMutationOutboxOptions options,
        SalesMutationInternalAuthOptions internalAuthOptions,
        TimeProvider timeProvider) {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _allowedInternalBaseUri = _options.GetValidatedAllowedInternalBaseUri();
        _internalApiKey = (internalAuthOptions ??
            throw new ArgumentNullException(nameof(internalAuthOptions))).GetValidatedApiKey();
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SalesMutationDeliveryResult> ProcessNextAsync(CancellationToken cancellationToken) {
        DateTime utcNow = UtcNow();
        SalesMutationOutboxLease lease = await _store.ClaimNextAsync(
            utcNow,
            _options.LeaseDuration,
            cancellationToken);

        if (lease == null)
            return new SalesMutationDeliveryResult(SalesMutationDeliveryKind.None, Guid.Empty, 0);

        if (!PayloadContainsOperationKey(lease.Payload, lease.OperationNetUid))
            return await MarkDeadLetterAsync(
                lease,
                "Payload OperationNetUid does not match the durable outbox operation key.",
                null,
                SalesMutationDeliveryFailureKind.None,
                cancellationToken);

        if (!TryGetAllowedRequestUri(lease.RequestUrl, out Uri requestUri))
            return await MarkDeadLetterAsync(
                lease,
                "Sales mutation target is outside the configured internal origin or expected endpoint path.",
                null,
                SalesMutationDeliveryFailureKind.None,
                cancellationToken);

        try {
            using CancellationTokenSource timeout =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_options.RequestTimeout);
            using HttpClient httpClient = _httpClientFactory.CreateClient(HttpClientName);

            if (TryGetExpectedTtnUrl(lease, out string expectedTtnUrl)) {
                Uri finalizeUri = BuildTtnFinalizeUri(requestUri);
                using HttpRequestMessage finalizeRequest = CreateAuthenticatedRequest(
                    finalizeUri,
                    lease.OperationNetUid);
                using HttpResponseMessage finalizeResponse = await httpClient.SendAsync(
                    finalizeRequest,
                    HttpCompletionOption.ResponseContentRead,
                    timeout.Token);

                if (!finalizeResponse.IsSuccessStatusCode)
                    return await HandleHttpFailureAsync(
                        lease,
                        finalizeResponse.StatusCode,
                        "TTN finalize endpoint",
                        cancellationToken);

                string finalizeBody = await finalizeResponse.Content.ReadAsStringAsync(
                    timeout.Token);
                string finalizedUrl = NormalizeResponseUrl(finalizeBody);
                if (!string.Equals(finalizedUrl, expectedTtnUrl, StringComparison.Ordinal))
                    return await MarkDeadLetterAsync(
                        lease,
                        "TTN finalize response did not match the URL committed with the sale.",
                        (int)finalizeResponse.StatusCode,
                        SalesMutationDeliveryFailureKind.None,
                        cancellationToken);
            }

            using HttpRequestMessage request = CreateAuthenticatedRequest(
                requestUri,
                lease.OperationNetUid,
                lease.Payload);
            using HttpResponseMessage response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);

            int statusCode = (int) response.StatusCode;
            if (response.IsSuccessStatusCode)
                return await MarkCompletedAsync(lease, statusCode, cancellationToken);
            return await HandleHttpFailureAsync(
                lease,
                response.StatusCode,
                "sales mutation endpoint",
                cancellationToken);
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return await MarkRetryAsync(
                lease,
                $"Sales mutation request timed out after {_options.RequestTimeout.TotalSeconds:0.###} seconds.",
                null,
                SalesMutationDeliveryFailureKind.None,
                cancellationToken);
        } catch (HttpRequestException exception) {
            return await MarkRetryAsync(
                lease,
                $"Sales mutation transport failure ({exception.HttpRequestError}).",
                exception.StatusCode.HasValue ? (int) exception.StatusCode.Value : null,
                SalesMutationDeliveryFailureKind.None,
                cancellationToken);
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception exception) {
            return await MarkRetryAsync(
                lease,
                $"Unexpected sales mutation delivery failure ({exception.GetType().Name}).",
                null,
                SalesMutationDeliveryFailureKind.None,
                cancellationToken);
        }
    }

    private Task<SalesMutationDeliveryResult> MarkAuthenticationFailureAsync(
        SalesMutationOutboxLease lease,
        int statusCode,
        CancellationToken cancellationToken) {
        string error = $"Authentication HTTP {statusCode} from sales mutation endpoint.";
        if (lease.AuthenticationFailureCount >= _options.MaxAuthenticationDeliveryAttempts - 1)
            return MarkDeadLetterAsync(
                lease,
                error,
                statusCode,
                SalesMutationDeliveryFailureKind.Authentication,
                cancellationToken);

        return MarkRetryAsync(
            lease,
            error,
            statusCode,
            SalesMutationDeliveryFailureKind.Authentication,
            cancellationToken);
    }

    private Task<SalesMutationDeliveryResult> HandleHttpFailureAsync(
        SalesMutationOutboxLease lease,
        HttpStatusCode statusCode,
        string target,
        CancellationToken cancellationToken) {
        int numericStatusCode = (int)statusCode;
        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return MarkAuthenticationFailureAsync(
                lease,
                numericStatusCode,
                cancellationToken);

        if (IsTransient(statusCode))
            return MarkRetryAsync(
                lease,
                $"Transient HTTP {numericStatusCode} from {target}.",
                numericStatusCode,
                SalesMutationDeliveryFailureKind.None,
                cancellationToken);

        return MarkDeadLetterAsync(
            lease,
            $"Non-retryable HTTP {numericStatusCode} from {target}.",
            numericStatusCode,
            SalesMutationDeliveryFailureKind.None,
            cancellationToken);
    }

    private async Task<SalesMutationDeliveryResult> MarkCompletedAsync(
        SalesMutationOutboxLease lease,
        int statusCode,
        CancellationToken cancellationToken) {
        bool completed = await _store.CompleteAsync(
            lease.OperationNetUid,
            lease.LeaseToken,
            UtcNow(),
            cancellationToken);
        return new SalesMutationDeliveryResult(
            completed ? SalesMutationDeliveryKind.Completed : SalesMutationDeliveryKind.LeaseLost,
            lease.OperationNetUid,
            lease.AttemptCount,
            statusCode);
    }

    private async Task<SalesMutationDeliveryResult> MarkRetryAsync(
        SalesMutationOutboxLease lease,
        string error,
        int? statusCode,
        SalesMutationDeliveryFailureKind failureKind,
        CancellationToken cancellationToken) {
        DateTime utcNow = UtcNow();
        DateTime nextAttemptUtc = utcNow.Add(GetRetryDelay(lease.AttemptCount));
        bool retrying = await _store.RetryAsync(
            lease.OperationNetUid,
            lease.LeaseToken,
            utcNow,
            nextAttemptUtc,
            error,
            failureKind,
            cancellationToken);
        return new SalesMutationDeliveryResult(
            retrying ? SalesMutationDeliveryKind.Retrying : SalesMutationDeliveryKind.LeaseLost,
            lease.OperationNetUid,
            lease.AttemptCount,
            statusCode,
            retrying ? nextAttemptUtc : null);
    }

    private async Task<SalesMutationDeliveryResult> MarkDeadLetterAsync(
        SalesMutationOutboxLease lease,
        string error,
        int? statusCode,
        SalesMutationDeliveryFailureKind failureKind,
        CancellationToken cancellationToken) {
        bool deadLettered = await _store.DeadLetterAsync(
            lease.OperationNetUid,
            lease.LeaseToken,
            UtcNow(),
            error,
            failureKind,
            cancellationToken);
        return new SalesMutationDeliveryResult(
            deadLettered ? SalesMutationDeliveryKind.DeadLettered : SalesMutationDeliveryKind.LeaseLost,
            lease.OperationNetUid,
            lease.AttemptCount,
            statusCode);
    }

    private TimeSpan GetRetryDelay(int attemptCount) {
        int exponent = Math.Clamp(attemptCount - 1, 0, 20);
        double multiplier = Math.Pow(2d, exponent);
        double delayMilliseconds = Math.Min(
            _options.InitialRetryDelay.TotalMilliseconds * multiplier,
            _options.MaxRetryDelay.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(delayMilliseconds);
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    private bool TryGetAllowedRequestUri(string requestUrl, out Uri requestUri) {
        if (!Uri.TryCreate(requestUrl, UriKind.Absolute, out requestUri) ||
            !string.IsNullOrEmpty(requestUri.UserInfo) ||
            !string.IsNullOrEmpty(requestUri.Query) ||
            !string.IsNullOrEmpty(requestUri.Fragment) ||
            !string.Equals(requestUri.Scheme, _allowedInternalBaseUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(requestUri.IdnHost, _allowedInternalBaseUri.IdnHost, StringComparison.OrdinalIgnoreCase) ||
            requestUri.Port != _allowedInternalBaseUri.Port ||
            !IsExpectedRequestPath(requestUri.AbsolutePath)) {
            requestUri = null;
            return false;
        }

        return true;
    }

    private static bool IsExpectedRequestPath(string absolutePath) =>
        string.Equals(absolutePath, _ukrainianRequestPath, StringComparison.Ordinal) ||
        string.Equals(absolutePath, _polishRequestPath, StringComparison.Ordinal);

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout or (HttpStatusCode) 425 or (HttpStatusCode) 429 ||
        (int) statusCode >= 500;

    private HttpRequestMessage CreateAuthenticatedRequest(
        Uri requestUri,
        Guid operationNetUid,
        string payload = null) {
        HttpRequestMessage request = new(HttpMethod.Post, requestUri);
        if (payload != null)
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        request.Headers.Add(SalesMutationInternalAuthOptions.HeaderName, _internalApiKey);
        SalesMutationRequestKey.AddTo(request, operationNetUid);
        return request;
    }

    private Uri BuildTtnFinalizeUri(Uri salesMutationUri) =>
        new(
            _allowedInternalBaseUri,
            string.Equals(
                salesMutationUri.AbsolutePath,
                _polishRequestPath,
                StringComparison.Ordinal)
                ? _polishTtnFinalizePath
                : _ukrainianTtnFinalizePath);

    private static bool TryGetExpectedTtnUrl(
        SalesMutationOutboxLease lease,
        out string expectedTtnUrl) {
        expectedTtnUrl = null;
        if (!string.Equals(
                lease.OperationName,
                SalesMutationOperationNames.OrderInvoiceSaleUpdate,
                StringComparison.Ordinal))
            return false;

        try {
            using JsonDocument document = JsonDocument.Parse(lease.Payload);
            JsonProperty ttnProperty = document.RootElement
                .EnumerateObject()
                .FirstOrDefault(property => string.Equals(
                    property.Name,
                    "CustomersOwnTtn",
                    StringComparison.OrdinalIgnoreCase));
            if (ttnProperty.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return false;
            if (ttnProperty.Value.ValueKind != JsonValueKind.Object)
                return false;

            JsonProperty pathProperty = ttnProperty.Value
                .EnumerateObject()
                .FirstOrDefault(property => string.Equals(
                    property.Name,
                    "TtnPDFPath",
                    StringComparison.OrdinalIgnoreCase));
            if (pathProperty.Value.ValueKind != JsonValueKind.String)
                return false;

            expectedTtnUrl = pathProperty.Value.GetString()?.Trim();
            return !string.IsNullOrWhiteSpace(expectedTtnUrl);
        } catch (JsonException) {
            return false;
        }
    }

    private static string NormalizeResponseUrl(string responseBody) {
        string value = responseBody?.Trim() ?? string.Empty;
        if (value.Length < 2 || value[0] != '"') return value;
        try {
            return JsonSerializer.Deserialize<string>(value)?.Trim() ?? string.Empty;
        } catch (JsonException) {
            return value;
        }
    }

    private static bool PayloadContainsOperationKey(string payload, Guid expectedOperationNetUid) {
        try {
            using JsonDocument document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return false;

            JsonProperty operationProperty = document.RootElement
                .EnumerateObject()
                .FirstOrDefault(property => string.Equals(
                    property.Name,
                    SalesMutationRequestKey.BodyPropertyName,
                    StringComparison.OrdinalIgnoreCase));

            return operationProperty.Value.ValueKind == JsonValueKind.String &&
                   Guid.TryParse(operationProperty.Value.GetString(), out Guid operationNetUid) &&
                   operationNetUid == expectedOperationNetUid &&
                   operationNetUid != Guid.Empty;
        } catch (JsonException) {
            return false;
        }
    }
}
