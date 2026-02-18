using System;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncConsignment {
    public byte[] DocumentId { get; set; }

    public string DocumentIdInString =>
        $"0x{BitConverter.ToString(DocumentId).Replace("-", "")}";

    public SyncConsignmentType TypeDocument { get; set; }

    public string DocumentNumber { get; set; }

    public DateTime DocumentDate { get; set; }

    public string DocumentArrivalNumber { get; set; }

    public DateTime? DocumentArrivalDate { get; set; }

    public decimal RateExchange { get; set; }

    public long? ClientCode { get; set; }

    public long? AgreementCode { get; set; }

    public string OrganizationName { get; set; }

    public string StorageName { get; set; }

    public string IncomeStorageName { get; set; }

    public string CurrencyCode { get; set; }

    public long ProductCode { get; set; }

    public string VendorCode { get; set; }

    public double Qty { get; set; }

    public double? IncomeQty { get; set; }

    public decimal NetValue { get; set; }

    public decimal TotalValue { get; set; }

    public decimal Value { get; set; }

    public decimal CustomsRate { get; set; }

    public decimal CustomsValue { get; set; }

    public decimal Vat { get; set; }

    public SyncVatEnumFenix VatTypeFenix { get; set; }

    public SyncVatEnumAmg VatTypeAmg { get; set; }

    public double WeightPer { get; set; }

    public double WeightBruttoPer { get; set; }

    public decimal TotalSpendAmount { get; set; }

    public string UKTVEDCode { get; set; }

    public string UKTVEDName { get; set; }

    public bool IsImported { get; set; }

    public decimal Rate { get; set; }

    public decimal DocumentValue { get; set; }

    public string Comment { get; set; }

    public decimal TotalVat { get; set; }
}