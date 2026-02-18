using System;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncOutcomePaymentOrder {
    public string DocumentNumber { get; set; }

    public DateTime DocumentDate { get; set; }

    public string OrganizationAccountName { get; set; }

    public decimal DocumentValue { get; set; }

    public string CurrencyCode { get; set; }

    public string Comment { get; set; }

    public decimal RateExchange { get; set; }

    public string ArticleCashExpendingName { get; set; }
}