using System;

namespace GBA.Domain.EntityHelpers.TotalDashboards.SupplyInvoices;

public sealed class OrderedInvoiceModel {
    public Guid NetId { get; set; }

    public string Number { get; set; }

    public Guid OrderNetId { get; set; }

    public string OrderNumber { get; set; }

    public Guid? ProtocolNetId { get; set; }

    public string ProtocolNumber { get; set; }

    public string SupplierName { get; set; }

    public decimal NetPrice { get; set; }

    public bool IsShipped { get; set; }
}