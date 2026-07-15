#nullable enable

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace GBA.Services.Infrastructure.SalesMutations;

/// <summary>Immutable identity and fingerprint of one inbound sales-creation request.</summary>
public sealed class SalesCreationRequest {
    public Guid OperationNetUid { get; init; }
    public string OperationName { get; init; } = string.Empty;
    public Guid PrincipalNetUid { get; init; }
    public Guid ClientNetUid { get; init; }
    public bool ModeFlag { get; init; }
    public byte[] RequestFingerprint { get; init; } = [];
}

/// <summary>Durable sales-creation receipt retained independently from dispatch outbox cleanup.</summary>
public sealed class SalesCreationLedgerEntry {
    public Guid OperationNetUid { get; init; }
    public string OperationName { get; init; } = string.Empty;
    public Guid PrincipalNetUid { get; init; }
    public Guid ClientNetUid { get; init; }
    public bool ModeFlag { get; init; }
    public byte[] RequestFingerprint { get; init; } = [];
    public string? ResponsePayload { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime? CompletedUtc { get; init; }
}

/// <summary>Result of serializably reserving an idempotency key.</summary>
/// <param name="WasInserted">Whether the current transaction reserved the key.</param>
/// <param name="Entry">The inserted or pre-existing immutable binding.</param>
public readonly record struct SalesCreationLedgerRegistration(
    bool WasInserted,
    SalesCreationLedgerEntry Entry);

/// <summary>Raised when a caller reuses a key for a different creation request.</summary>
public sealed class SalesCreationIdempotencyConflictException(string message) : Exception(message);

/// <summary>Persistence contract for permanent sales-creation idempotency receipts.</summary>
public interface ISalesCreationLedgerStore {
    Task EnsureSchemaAsync(CancellationToken cancellationToken);

    Task<SalesCreationLedgerEntry?> GetAsync(
        Guid operationNetUid,
        CancellationToken cancellationToken);

    Task<SalesCreationLedgerRegistration> RegisterAsync(
        IDbConnection connection,
        SalesCreationRequest request,
        DateTime createdUtc,
        CancellationToken cancellationToken);

    Task CompleteAsync(
        IDbConnection connection,
        SalesCreationRequest request,
        string responsePayload,
        DateTime completedUtc,
        CancellationToken cancellationToken);
}
