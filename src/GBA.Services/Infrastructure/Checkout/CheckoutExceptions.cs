using System;

namespace GBA.Services.Infrastructure.Checkout;

public sealed class CheckoutConflictException : Exception {
    public const string InProgressMessage =
        "A checkout with this idempotency key is still being processed. Retry the request with the same key.";

    public const string CartChangedMessage =
        "The shopping cart changed during checkout. Review it and try again.";

    public CheckoutConflictException(string message) : base(message) { }
}

public sealed class CheckoutKeyReuseException : Exception {
    public const string DefaultMessage =
        "This idempotency key was already used for a different checkout request.";

    public CheckoutKeyReuseException() : base(DefaultMessage) { }
}
