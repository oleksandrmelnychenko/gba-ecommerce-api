using System.Text.Json.Serialization;

namespace GBA.Services.Infrastructure.Checkout;

/// <summary>Only known rolled-back failures prove that this checkout created no order.
/// Pending and reused keys carry no such claim; their committed receipt must be recovered.</summary>
public sealed record CheckoutFailureDetails(
    string Code,
    bool Retryable,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] bool? OrderCreated) {
    public const string InProgress = "checkout_in_progress";
    public const string StockChanged = "checkout_stock_changed";
    public const string CartChanged = "checkout_cart_changed";
    public const string ReservationConflict = "checkout_reservation_conflict";
    public const string ConcurrencyConflict = "checkout_concurrency_conflict";
    public const string KeyReused = "checkout_key_reused";
    public const string Conflict = "checkout_conflict";

    public static CheckoutFailureDetails From(CheckoutConflictException exception) => exception.Code switch {
        InProgress => new(InProgress, true, null),
        ConcurrencyConflict => new(ConcurrencyConflict, true, false),
        StockChanged => new(StockChanged, false, false),
        CartChanged => new(CartChanged, false, false),
        ReservationConflict => new(ReservationConflict, false, false),
        _ => new(Conflict, false, null)
    };

    public static CheckoutFailureDetails ReusedKey() => new(KeyReused, false, null);
}
