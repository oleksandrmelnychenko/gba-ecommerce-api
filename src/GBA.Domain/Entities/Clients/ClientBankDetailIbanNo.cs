namespace GBA.Domain.Entities.Clients;

public sealed class ClientBankDetailIbanNo : EntityBase {
    public string IBANNO { get; set; }

    public long CurrencyId { get; set; }

    public Currency Currency { get; set; }
}