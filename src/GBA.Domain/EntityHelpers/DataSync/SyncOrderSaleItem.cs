using System;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncOrderSaleItem {
    public byte[] OrderId { get; set; }

    public string OrderIdInString =>
        $"0x{BitConverter.ToString(OrderId).Replace("-", "")}";

    public DateTime OrderDateTime { get; set; }

    public string OrderNumber { get; set; }

    public bool IsSale { get; set; }

    public DateTime? SaleDateTime { get; set; }

    public string SaleNumber { get; set; }

    public string CurrencyCode { get; set; }

    public long ClientCode { get; set; }

    public long AgreementCode { get; set; }

    public string Organization { get; set; }

    public string Storage { get; set; }

    public decimal ExchangeRate { get; set; }

    public bool WithVat { get; set; }

    public decimal TotalValue { get; set; }

    public long ProductCode { get; set; }

    public string ProductName { get; set; }

    public string VendorCode { get; set; }

    public double Qty { get; set; }

    public SyncVatEnumFenix VatTypeFenix { get; set; }

    public SyncVatEnumAmg VatTypeAmg { get; set; }

    public decimal Price { get; set; }

    public decimal Vat { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Discount { get; set; }

    public string Specification { get; set; }

    public string OrderComment { get; set; }

    public string SaleComment { get; set; }
}