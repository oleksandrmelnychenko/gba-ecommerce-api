using System;

namespace GBA.Services.Infrastructure.Checkout;

public sealed class CheckoutConflictException : Exception {
    public const string InProgressMessage =
        "A checkout with this idempotency key is still being processed. Retry the request with the same key.";

    public const string CartChangedMessage =
        "The shopping cart changed during checkout. Review it and try again.";

    public const string CartStockChangedMessage =
        "The shopping cart or the stock changed concurrently. Reload the cart and try again.";

    public string Code { get; }

    public CheckoutConflictException(string message, string code = null) : base(message) {
        Code = code ?? (message switch {
            InProgressMessage => CheckoutFailureDetails.InProgress,
            CartChangedMessage => CheckoutFailureDetails.CartChanged,
            CartStockChangedMessage => CheckoutFailureDetails.StockChanged,
            CheckoutExecutor.ConcurrentChangeMessage => CheckoutFailureDetails.ConcurrencyConflict,
            _ => CheckoutFailureDetails.Conflict
        });
    }
}

public sealed class CheckoutKeyReuseException : Exception {
    public const string DefaultMessage =
        "This idempotency key was already used for a different checkout request.";

    public CheckoutKeyReuseException() : base(DefaultMessage) { }
}
