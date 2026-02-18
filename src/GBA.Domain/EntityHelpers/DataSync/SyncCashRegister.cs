namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncCashRegister {
    public string CashRegisterName { get; set; }

    public string CurrencyCode { get; set; }

    public string OrganizationName { get; set; }

    public decimal Value { get; set; }
}