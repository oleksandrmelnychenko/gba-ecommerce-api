using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GBA.Services.Infrastructure.SalesMutations;

public sealed class SalesMutationOutboxPublisher : ISalesMutationOutboxPublisher {
    private const int _maxOperationNameLength = 128;
    private const int _maxRequestUrlLength = 2048;
    private const int _maxPayloadBytes = 8 * 1024 * 1024;

    private readonly ISalesMutationOutboxStore _store;
    private readonly TimeProvider _timeProvider;

    public SalesMutationOutboxPublisher(
        ISalesMutationOutboxStore store,
        TimeProvider timeProvider) {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Guid> EnqueueAsync(
        string requestUrl,
        string frozenPayload,
        string operationName,
        CancellationToken cancellationToken = default) {
        SalesMutationOutboxMessage message = PrepareMessage(requestUrl, frozenPayload, operationName);
        await _store.EnqueueAsync(message, cancellationToken);
        return message.OperationNetUid;
    }

    public async Task<Guid> EnqueueAsync(
        IDbConnection connection,
        string requestUrl,
        string frozenPayload,
        string operationName,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(connection);
        SalesMutationOutboxMessage message = PrepareMessage(requestUrl, frozenPayload, operationName);
        await _store.EnqueueAsync(connection, message, cancellationToken);
        return message.OperationNetUid;
    }

    public async Task<Guid> EnqueueAsync(
        IDbConnection connection,
        string requestUrl,
        string frozenPayload,
        string operationName,
        Guid operationNetUid,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(connection);
        SalesMutationOutboxMessage message = PrepareMessage(
            requestUrl,
            frozenPayload,
            operationName,
            operationNetUid);
        await _store.EnqueueAsync(connection, message, cancellationToken);
        return message.OperationNetUid;
    }

    private SalesMutationOutboxMessage PrepareMessage(
        string requestUrl,
        string frozenPayload,
        string operationName,
        Guid? operationNetUid = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(frozenPayload);

        if (requestUrl.Length > _maxRequestUrlLength)
            throw new ArgumentException("The sales mutation URL is too long.", nameof(requestUrl));

        if (!Uri.TryCreate(requestUrl, UriKind.Absolute, out Uri requestUri) ||
            requestUri.Scheme is not ("http" or "https"))
            throw new ArgumentException("The sales mutation URL must be an absolute HTTP(S) URL.", nameof(requestUrl));

        operationName = operationName.Trim();
        if (operationName.Length > _maxOperationNameLength)
            throw new ArgumentException("The sales mutation operation name is too long.", nameof(operationName));

        SalesMutationRequestKey.PreparedSalesMutation prepared = operationNetUid.HasValue
            ? SalesMutationRequestKey.Prepare(operationName, frozenPayload, operationNetUid.Value)
            : SalesMutationRequestKey.Prepare(operationName, frozenPayload);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(prepared.Payload);

        if (payloadBytes.Length > _maxPayloadBytes)
            throw new ArgumentException("The sales mutation payload exceeds the durable outbox limit.", nameof(frozenPayload));

        DateTime utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        return new SalesMutationOutboxMessage {
            OperationNetUid = prepared.OperationNetUid,
            OperationName = operationName,
            RequestUrl = requestUri.AbsoluteUri,
            Payload = prepared.Payload,
            PayloadSha256 = SHA256.HashData(payloadBytes),
            Status = SalesMutationOutboxStatus.Pending,
            AttemptCount = 0,
            NextAttemptUtc = utcNow,
            CreatedUtc = utcNow
        };
    }
}
