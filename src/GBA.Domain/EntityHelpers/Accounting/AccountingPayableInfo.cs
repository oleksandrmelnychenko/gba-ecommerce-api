using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.Accounting;

public sealed class AccountingPayableInfo {
    public AccountingPayableInfo() {
        PriceTotals = new List<PriceTotal>();

        AccountingPayableInfoItems = new List<AccountingPayableInfoItem>();
    }

    public decimal TotalEuroAmount { get; set; }

    public List<PriceTotal> PriceTotals { get; set; }

    public List<AccountingPayableInfoItem> AccountingPayableInfoItems { get; set; }
}