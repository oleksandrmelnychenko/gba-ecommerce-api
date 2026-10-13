using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Transactions;
using Dapper;

namespace GBA.Services.Infrastructure.SalesMutations;

/// <summary>Serializes cart consumption with cart editing and expiry, on one enlisted connection.</summary>
public sealed class CartMutationScope : IDisposable {
    private readonly TransactionScope transaction;
    private readonly List<Action> afterCommit = new();
    private bool completed;

    /// <summary>Opens a fresh connection inside an owned transaction and takes the shared cart mutation lock.</summary>
    public CartMutationScope(IDbConnection connection) {
        if (Transaction.Current != null)
            throw new InvalidOperationException("Cart mutation must own its transaction.");
        if (connection.State != ConnectionState.Closed)
            throw new InvalidOperationException("Cart mutation requires a fresh connection.");
        transaction = new TransactionScope(TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.Serializable,
                Timeout = TimeSpan.FromMinutes(2) }, TransactionScopeAsyncFlowOption.Enabled);
        try {
            connection.Open();
            if (connection is not DbConnection)
                throw new InvalidOperationException("Cart mutation requires a database connection.");
            AcquireLock(connection);
        } catch { transaction.Dispose(); throw; }
    }

    /// <summary>Coordinates existing invoice transactions with cart writers before ledger or product locks.</summary>
    public static void AcquireLock(IDbConnection connection) {
        if (Transaction.Current == null) throw new InvalidOperationException("Cart locking requires a transaction.");
        if (connection.State == ConnectionState.Closed) connection.Open();
        int result = connection.ExecuteScalar<int>("""
                DECLARE @result int;
                EXEC @result=sys.sp_getapplock @Resource=N'gba-ecommerce:cart-mutation',
                    @LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=60000;
                SELECT @result;
                """);
        if (result < 0) throw new InvalidOperationException("Unable to lock cart mutation.");
    }

    /// <summary>Marks the writes successful; disposal still must commit before the result leaves its owning method.</summary>
    public T Complete<T>(T value) { transaction.Complete(); completed = true; return value; }
    /// <summary>Queues best-effort work for successful commit only.</summary>
    public void AfterCommit(Action action) => afterCommit.Add(action);
    /// <summary>Captures a notification value now, avoiding mutable loop-variable captures at commit.</summary>
    public void AfterCommit<T>(Action<T> action, T value) => afterCommit.Add(() => action(value));
    /// <summary>Commits or rolls back the transaction, then runs eligible best-effort notifications.</summary>
    public void Dispose() {
        transaction.Dispose();
        if (completed) foreach (Action action in afterCommit) {
            try { action(); }
            catch (Exception error) { NLog.LogManager.GetCurrentClassLogger().Error(error, "Post-commit cart notification failed."); }
        }
    }

    /// <summary>Locks an unconsumed item belonging to the exact client or workplace principal before editing it.</summary>
    public static void RequireOwnedItem(IDbConnection connection, long itemId, Guid principal, long productId) {
        if (itemId <= 0 || principal == Guid.Empty || productId <= 0)
            throw new ArgumentException("A cart item and product identity are required.");
        int matches = connection.ExecuteScalar<int>("""
            SELECT COUNT(*) FROM dbo.OrderItem i WITH(UPDLOCK,HOLDLOCK)
            JOIN dbo.ClientShoppingCart c WITH(UPDLOCK,HOLDLOCK) ON c.ID=i.ClientShoppingCartID
            JOIN dbo.ClientAgreement a ON a.ID=c.ClientAgreementID
            JOIN dbo.Client cl ON cl.ID=a.ClientID
            LEFT JOIN dbo.Workplace w ON w.ID=c.WorkplaceID
            WHERE i.ID=@itemId AND i.ProductID=@productId AND i.OrderID IS NULL AND i.Deleted=0
                AND c.Deleted=0 AND c.IsOffer=0 AND a.Deleted=0
                AND ((cl.NetUID=@principal AND c.WorkplaceID IS NULL) OR w.NetUID=@principal);
            """, new { itemId, principal, productId });
        if (matches != 1) throw new ArgumentException("The item is no longer in your shopping cart.");
    }
}
