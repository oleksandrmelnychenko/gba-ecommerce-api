using System;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncBankRegister {
    public string BankAccountName { get; set; }

    public string BankAccountNumber { get; set; }

    public string OrganizationName { get; set; }

    public string CurrencyCode { get; set; }

    public decimal Value { get; set; }

    public DateTime? DateOpening { get; set; }

    public DateTime? DateClosing { get; set; }

    public string BankCode { get; set; }

    public string BankName { get; set; }

    public string BankNumber { get; set; }

    public string City { get; set; }

    public string Address { get; set; }
}