using System;

namespace GBA.Services.Infrastructure.Checkout;

public static class CheckoutIdempotencyKey {
    public const string HeaderName = "Idempotency-Key";

    public const string ReplayedHeaderName = "Idempotent-Replayed";

    public static bool TryParse(string value, out Guid key) {
        key = Guid.Empty;

        if (string.IsNullOrWhiteSpace(value) || value.Length > 64) return false;

        return Guid.TryParse(value.Trim(), out key) && key != Guid.Empty;
    }
}
