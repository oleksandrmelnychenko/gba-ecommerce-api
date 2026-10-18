using System;
using GBA.Domain.Entities.Sales;

namespace GBA.Services.Infrastructure.Checkout;

public sealed class CheckoutResult {
    public CheckoutResult(Sale sale, bool isReplay) {
        Sale = sale ?? throw new ArgumentNullException(nameof(sale));
        IsReplay = isReplay;
    }

    public Sale Sale { get; }

    public bool IsReplay { get; }
}
