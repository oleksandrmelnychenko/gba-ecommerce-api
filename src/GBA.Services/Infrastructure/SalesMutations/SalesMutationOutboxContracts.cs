using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace GBA.Services.Infrastructure.SalesMutations;

public static class SalesMutationOperationNames {
    public const string OrderInvoiceSaleUpdate = "Order invoice sale update";
    public const string RetailSaleUpdate = "Retail sale update";
    public const string QuickSaleUpdate = "Quick sale update";
}

public enum SalesMutationOutboxStatus : byte {
    Pending = 0,
    Leased = 1,
    Completed = 2,
    DeadLetter = 3
}

public enum SalesMutationDeliveryFailureKind : byte {
    None = 0,
    Authentication = 1
}

public sealed class SalesMutationOutboxMessage {
    public Guid OperationNetUid { get; init; }
    public string OperationName { get; init; }
    public string RequestUrl { get; init; }
    public string Payload { get; init; }
    public byte[] PayloadSha256 { get; init; }
    public SalesMutationOutboxStatus Status { get; init; }
    public int AttemptCount { get; init; }
    public int AuthenticationFailureCount { get; init; }
    public DateTime NextAttemptUtc { get; init; }
    public DateTime CreatedUtc { get; init; }
}

public sealed class SalesMutationOutboxLease {
    public Guid OperationNetUid { get; init; }
    public string OperationName { get; init; }
    public string RequestUrl { get; init; }
    public string Payload { get; init; }
    public Guid LeaseToken { get; init; }
    public int AttemptCount { get; init; }
    public int AuthenticationFailureCount { get; init; }
}

public sealed class SalesMutationOutboxStats {
    public int PendingCount { get; init; }
    public int LeasedCount { get; init; }
    public int DeadLetterCount { get; init; }
    public int AuthenticationFailureCount { get; init; }
    public DateTime? OldestPendingUtc { get; init; }
}

public interface ISalesMutationOutboxStore {
    Task EnsureSchemaAsync(CancellationToken cancellationToken);
    Task<SalesMutationOutboxMessage> GetAsync(
        Guid operationNetUid,
        CancellationToken cancellationToken);
    Task EnqueueAsync(SalesMutationOutboxMessage message, CancellationToken cancellationToken);
    Task EnqueueAsync(
        IDbConnection connection,
        SalesMutationOutboxMessage message,
        CancellationToken cancellationToken);
    Task<SalesMutationOutboxLease> ClaimNextAsync(
        DateTime utcNow,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);
    Task<bool> CompleteAsync(
        Guid operationNetUid,
        Guid leaseToken,
        DateTime utcNow,
        CancellationToken cancellationToken);
    Task<bool> RetryAsync(
        Guid operationNetUid,
        Guid leaseToken,
        DateTime utcNow,
        DateTime nextAttemptUtc,
        string lastError,
        SalesMutationDeliveryFailureKind failureKind,
        CancellationToken cancellationToken);
    Task<bool> DeadLetterAsync(
        Guid operationNetUid,
        Guid leaseToken,
        DateTime utcNow,
        string lastError,
        SalesMutationDeliveryFailureKind failureKind,
        CancellationToken cancellationToken);
    Task<int> DeleteCompletedBeforeAsync(DateTime cutoffUtc, CancellationToken cancellationToken);
    Task<SalesMutationOutboxStats> GetStatsAsync(CancellationToken cancellationToken);
}

public interface ISalesMutationOutboxPublisher {
    Task<Guid> EnqueueAsync(
        string requestUrl,
        string frozenPayload,
        string operationName,
        CancellationToken cancellationToken = default);
    Task<Guid> EnqueueAsync(
        IDbConnection connection,
        string requestUrl,
        string frozenPayload,
        string operationName,
        CancellationToken cancellationToken = default);
    Task<Guid> EnqueueAsync(
        IDbConnection connection,
        string requestUrl,
        string frozenPayload,
        string operationName,
        Guid operationNetUid,
        CancellationToken cancellationToken = default);
}

public enum SalesMutationDeliveryKind {
    None,
    Completed,
    Retrying,
    DeadLettered,
    LeaseLost
}

public readonly record struct SalesMutationDeliveryResult(
    SalesMutationDeliveryKind Kind,
    Guid OperationNetUid,
    int AttemptCount,
    int? StatusCode = null,
    DateTime? NextAttemptUtc = null);

public interface ISalesMutationOutboxDispatcher {
    Task<SalesMutationDeliveryResult> ProcessNextAsync(CancellationToken cancellationToken);
}
