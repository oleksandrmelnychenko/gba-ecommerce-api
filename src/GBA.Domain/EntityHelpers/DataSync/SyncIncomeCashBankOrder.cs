using System;
using GBA.Domain.EntityHelpers.Accounting;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncIncomeCashBankOrder {
    public byte[] OrderId { get; set; }

    public string OrderIdInString =>
        $"0x{BitConverter.ToString(OrderId).Replace("-", "")}";

    public string Number { get; set; }

    public DateTime FromDate { get; set; }

    public string DocumentArrivalNumber { get; set; }

    public DateTime? DocumentArrivalDate { get; set; }

    public DateTime? PaymentDate { get; set; }

    public bool IsAccounting { get; set; }

    public bool IsManagementAccounting { get; set; }

    public string Organization { get; set; }

    public string RecipientOrganization { get; set; }

    public string PaymentPurpose { get; set; }

    public string CashPaymentRegister { get; set; }

    public string OrganizationPaymentRegister { get; set; }

    public string ClientPaymentRegister { get; set; }

    public OperationType TypeOperation { get; set; }

    public long? ClientCode { get; set; }

    public long? AgreementCode { get; set; }

    public long CurrencyCode { get; set; }

    public decimal TotalValue { get; set; }

    public decimal TotalVat { get; set; }

    public string FromUser { get; set; }

    public string Responsible { get; set; }

    public string Comment { get; set; }

    public SyncVatEnumFenix? VatTypeFenix { get; set; }

    public SyncVatEnumAmg? VatTypeAmg { get; set; }

    public string ArticlesOfMoneyAccounts { get; set; }
}