using System;
using System.Transactions;
using NLog;

namespace GBA.Common.Helpers;

public static class TransactionalEffects {
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public static void AfterCommit(Action action) {
        ArgumentNullException.ThrowIfNull(action);

        Transaction transaction = Transaction.Current;

        if (transaction == null) {
            action();
            return;
        }

        transaction.TransactionCompleted += (_, completed) => {
            if (completed.Transaction?.TransactionInformation.Status != TransactionStatus.Committed) return;

            try {
                using TransactionScope suppressed = new(
                    TransactionScopeOption.Suppress,
                    TransactionScopeAsyncFlowOption.Enabled);

                action();
            } catch (Exception exception) {
                _logger.Error(exception, "A post-commit effect failed");
            }
        };
    }
}
