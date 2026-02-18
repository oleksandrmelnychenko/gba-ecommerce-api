using System;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncAgreement {
    public string Name { get; set; }

    public string Number { get; set; }

    public string CurrencyCode { get; set; }

    public decimal PermissibleDebtAmount { get; set; }

    public int DebtDaysAllowedNumber { get; set; }

    public string OrganizationName { get; set; }

    public string TypePriceName { get; set; }

    public string PromotionalTypePriceName { get; set; }

    public bool IsManagementAccounting { get; set; }

    public bool IsAccounting { get; set; }

    public TypeSyncAgreement Type { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public long SourceCode { get; set; }

    public byte[] SourceId { get; set; }
}