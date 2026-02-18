using System;

namespace GBA.Domain.EntityHelpers.ProductAvailabilityModels;

public sealed class AvailabilityInvoiceModel {
    public double Qty { get; set; }

    public string OrderNumber { get; set; }

    public string SupplyInvoiceNumber { get; set; }

    public DateTime Created { get; set; }
}