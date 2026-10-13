using System;
using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Sales;

namespace GBA.Services.Infrastructure.SalesMutations;

/// <summary>A new checkout attempt failed business validation and its owning scope rolled back before return.</summary>
public sealed class LegacyCheckoutRejectedException(string message, Exception innerException = null)
    : ArgumentException(message, innerException);

/// <summary>Commits legacy checkout writes and their permanent replay receipt together.</summary>
public sealed class LegacyCheckoutExecutor(
    IDbConnectionFactory connections, ISalesCreationLedgerStore ledger, TimeProvider timeProvider) {
    /// <summary>Binds an explicit operation key to its authenticated principal, mode and stable inbound fields.</summary>
    public static SalesCreationRequest Request(Guid operationNetUid, Guid principal, string operation,
        bool mode, object stablePayload) {
        if (operationNetUid == Guid.Empty || principal == Guid.Empty)
            throw new ArgumentException("Explicit checkout operation and principal identities are required.");
        return new SalesCreationRequest {
            OperationNetUid = operationNetUid, PrincipalNetUid = principal, ClientNetUid = principal,
            OperationName = operation, ModeFlag = mode,
            RequestFingerprint = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(stablePayload))
        };
    }

    /// <summary>Returns the immutable receipt or commits the callback and receipt on one enlisted connection.</summary>
    /// <remarks>The caller owns notifications after this method returns; nested ambient transactions are refused.</remarks>
    public async Task<(Sale Sale, bool IsReplay)> ExecuteAsync(SalesCreationRequest request,
        Func<IDbConnection, Sale> create) {
        // This method owns commit, so callers can safely notify only after it returns.
        if (System.Transactions.Transaction.Current != null)
            throw new InvalidOperationException("Checkout must own its transaction.");
        using IDbConnection connection = connections.NewSqlConnection();
        using var mutation = new CartMutationScope(connection);
        SalesCreationLedgerRegistration registration = await ledger.RegisterAsync(
            connection, request, timeProvider.GetUtcNow().UtcDateTime, default);
        SalesCreationRequestKey.EnsureMatches(request, registration.Entry);
        if (!registration.WasInserted) {
            Sale replay = JsonSerializer.Deserialize<Sale>(registration.Entry.ResponsePayload ?? "null");
            if (replay == null || replay.NetUid == Guid.Empty)
                throw new InvalidOperationException("The checkout replay receipt is invalid.");
            return mutation.Complete((replay, true));
        }
        Sale sale;
        try { sale = create(connection); }
        catch (ArgumentException error) {
            // Scope disposal must succeed before this typed rejection can reach the caller.
            // Disposal/rollback failure supersedes it and therefore cannot be marked definitive.
            throw new LegacyCheckoutRejectedException(error.Message, error);
        }
        if (sale == null || sale.Id <= 0 || sale.NetUid == Guid.Empty)
            throw new InvalidOperationException("The created sale could not be reloaded.");
        await ledger.CompleteAsync(connection, request, JsonSerializer.Serialize(sale),
            timeProvider.GetUtcNow().UtcDateTime, default);
        return mutation.Complete((sale, false));
    }
}
