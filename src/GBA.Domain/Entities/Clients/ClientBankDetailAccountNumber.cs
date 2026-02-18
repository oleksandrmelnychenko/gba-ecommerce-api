namespace GBA.Domain.Entities.Clients;

public sealed class ClientBankDetailAccountNumber : EntityBase {
    public string AccountNumber { get; set; }

    public long CurrencyId { get; set; }

    public Currency Currency { get; set; }
}