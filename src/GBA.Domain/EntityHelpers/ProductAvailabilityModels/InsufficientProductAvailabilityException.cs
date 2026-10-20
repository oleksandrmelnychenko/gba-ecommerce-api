using System;
using System.Globalization;

namespace GBA.Domain.EntityHelpers.ProductAvailabilityModels;

public sealed class InsufficientProductAvailabilityException : InvalidOperationException {
    public InsufficientProductAvailabilityException(
        long productAvailabilityId,
        long productId,
        long storageId,
        double requestedQty,
        double availableQty) : base(string.Format(
        CultureInfo.InvariantCulture,
        "Недостатньо вільного залишку: товар {0} на складі {1} має вільно {2}, потрібно {3}. " +
        "Ці одиниці вже зарезервувала інша операція; оновіть кошик та повторіть.",
        productId,
        storageId,
        availableQty,
        requestedQty)) {
        ProductAvailabilityId = productAvailabilityId;
        ProductId = productId;
        StorageId = storageId;
        RequestedQty = requestedQty;
        AvailableQty = availableQty;
    }

    public long ProductAvailabilityId { get; }

    public long ProductId { get; }

    public long StorageId { get; }

    public double RequestedQty { get; }

    public double AvailableQty { get; }
}
