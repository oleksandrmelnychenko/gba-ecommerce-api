using System;

namespace GBA.Services.Infrastructure.Checkout;

public sealed class CheckoutAttempt {
    public CheckoutAttempt(Guid idempotencyKey, byte[] requestFingerprint) {
        if (idempotencyKey == Guid.Empty)
            throw new ArgumentException("An idempotency key is required.", nameof(idempotencyKey));
        if (requestFingerprint is not { Length: CheckoutFingerprint.Length })
            throw new ArgumentException("A SHA-256 request fingerprint is required.", nameof(requestFingerprint));

        IdempotencyKey = idempotencyKey;
        RequestFingerprint = requestFingerprint;
    }

    public Guid IdempotencyKey { get; }

    public byte[] RequestFingerprint { get; }
}
