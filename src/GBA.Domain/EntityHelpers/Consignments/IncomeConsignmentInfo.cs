using System;

namespace GBA.Domain.EntityHelpers.Consignments;

public sealed class IncomeConsignmentInfo {
    public string StorageName { get; set; }

    public string SupplierName { get; set; }

    public string OrganizationName { get; set; }

    public DateTime? IncomeToStorageDate { get; set; }

    public string IncomeToStorageNumber { get; set; }

    public string IncomeInvoiceNumber { get; set; }

    public DateTime? IncomeInvoiceDate { get; set; }

    public decimal NetPrice { get; set; }

    public decimal TotalNetPrice { get; set; }

    public decimal GrossPrice { get; set; }

    public decimal AccountingGrossPrice { get; set; }

    public double Weight { get; set; }

    public double IncomeQty { get; set; }

    public double RemainingQty { get; set; }

    public string FromInvoiceNumber { get; set; }

    public DateTime? FromInvoiceDate { get; set; }

    public decimal? ReturnPrice { get; set; }

    public decimal? PriceDifference { get; set; }

    public decimal UnitPriceLocal { get; set; }

    public string Currency { get; set; }

    public decimal ExchangeRate { get; set; }

    public decimal? AccountingEurUnitPrice { get; set; }

    public decimal? ManagementEurUnitPrice { get; set; }
}