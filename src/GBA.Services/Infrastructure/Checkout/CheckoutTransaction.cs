using System;
using System.Transactions;

namespace GBA.Services.Infrastructure.Checkout;

public static class CheckoutTransaction {
    public static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

    public static TransactionScope Begin() {
        return new TransactionScope(
            TransactionScopeOption.RequiresNew,
            new TransactionOptions {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = Timeout
            },
            TransactionScopeAsyncFlowOption.Enabled);
    }

    public static T Suppressed<T>(Func<T> action) {
        ArgumentNullException.ThrowIfNull(action);

        using TransactionScope suppressed = new(
            TransactionScopeOption.Suppress,
            TransactionScopeAsyncFlowOption.Enabled);

        return action();
    }

    public static void RequireAmbient() {
        if (Transaction.Current == null)
            throw new InvalidOperationException("Checkout writes require the checkout transaction.");
    }
}
