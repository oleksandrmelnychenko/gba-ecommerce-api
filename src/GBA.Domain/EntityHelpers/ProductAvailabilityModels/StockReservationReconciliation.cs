using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GBA.Domain.EntityHelpers.ProductAvailabilityModels;

public sealed class StockReservationReconciliationRow {
    public long OrderItemId { get; set; }

    public Guid OrderItemNetUid { get; set; }

    public long ProductId { get; set; }

    public double OrderItemQty { get; set; }

    public bool OrderItemDeleted { get; set; }

    public double ReservedQty { get; set; }

    public double ShippedQty { get; set; }

    public double Discrepancy => OrderItemDeleted ? ReservedQty : OrderItemQty - ReservedQty - ShippedQty;
}

public sealed record StockReservationMismatch(
    StockReservationReconciliationRow Row,
    double? PreviousDiscrepancy,
    bool IntroducedByOperation);

public static class StockReservationReconciliationPolicy {
    public const double Tolerance = 0.0000001d;

    public const string AuditEntityName = "StockReservationReconciliation";

    public static IReadOnlyList<StockReservationMismatch> Evaluate(
        IEnumerable<StockReservationReconciliationRow> before,
        IEnumerable<StockReservationReconciliationRow> after) {
        Dictionary<long, double> previous = (before ?? Enumerable.Empty<StockReservationReconciliationRow>())
            .Where(row => row != null)
            .GroupBy(row => row.OrderItemId)
            .ToDictionary(group => group.Key, group => group.First().Discrepancy);

        return (after ?? Enumerable.Empty<StockReservationReconciliationRow>())
            .Where(row => row != null && Math.Abs(row.Discrepancy) > Tolerance)
            .Select(row => {
                double? previousDiscrepancy = previous.TryGetValue(row.OrderItemId, out double value) ? value : null;
                bool introduced = !previousDiscrepancy.HasValue ||
                                  Math.Abs(row.Discrepancy - previousDiscrepancy.Value) > Tolerance;
                return new StockReservationMismatch(row, previousDiscrepancy, introduced);
            })
            .ToList();
    }

    public static string Describe(StockReservationReconciliationRow row) =>
        row.OrderItemDeleted
            ? string.Format(
                CultureInfo.InvariantCulture,
                "Позиція замовлення {0} (товар {1}) видалена, але має активні резерви {2}.",
                row.OrderItemId,
                row.ProductId,
                row.ReservedQty)
            : string.Format(
                CultureInfo.InvariantCulture,
                "Кількість у позиції замовлення {0} (товар {1}) = {2}, а активні резерви {3} + відвантажено {4} = {5}.",
                row.OrderItemId,
                row.ProductId,
                row.OrderItemQty,
                row.ReservedQty,
                row.ShippedQty,
                row.ReservedQty + row.ShippedQty);
}

public sealed class StockReservationMismatchException : InvalidOperationException {
    public StockReservationMismatchException(IReadOnlyList<StockReservationMismatch> mismatches)
        : base("Резерви не узгоджені з позиціями замовлення; операцію скасовано. " +
               string.Join(" ", (mismatches ?? Array.Empty<StockReservationMismatch>())
                   .Select(mismatch => StockReservationReconciliationPolicy.Describe(mismatch.Row)))) {
        Mismatches = mismatches ?? Array.Empty<StockReservationMismatch>();
    }

    public IReadOnlyList<StockReservationMismatch> Mismatches { get; }
}
