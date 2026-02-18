using System;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.EntityHelpers.Consignments;

public sealed class RemainingConsignment {
    public DateTime FromDate { get; set; }

    public string StorageName { get; set; }

    public string SupplierName { get; set; }

    public string InvoiceNumber { get; set; }

    public string ProductIncomeNumber { get; set; }

    public string OrganizationName { get; set; }

    public string CurrencyName { get; set; }

    public decimal NetPrice { get; set; }

    public decimal TotalNetPrice { get; set; }

    public decimal GrossPrice { get; set; }

    public decimal AccountingGrossPrice { get; set; }

    public double RemainingQty { get; set; }

    public double Weight { get; set; }

    public Product Product { get; set; }

    public int RowNumber { get; set; }

    public Guid ConsignmentItemNetId { get; set; }
}