using System.Collections.Generic;

namespace GBA.Domain.Entities.Clients;

public sealed class ClientBankDetails : EntityBase {
    public ClientBankDetails() {
        Clients = new HashSet<Client>();
    }

    public string BankAndBranch { get; set; }

    public string BankAddress { get; set; }

    public string Swift { get; set; }

    public string BranchCode { get; set; }

    public long? AccountNumberId { get; set; }

    public long? ClientBankDetailIbanNoId { get; set; }

    public ClientBankDetailAccountNumber AccountNumber { get; set; }

    public ClientBankDetailIbanNo ClientBankDetailIbanNo { get; set; }

    public ICollection<Client> Clients { get; set; }
}