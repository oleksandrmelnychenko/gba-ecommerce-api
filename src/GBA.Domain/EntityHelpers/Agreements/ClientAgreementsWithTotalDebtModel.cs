using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.EntityHelpers.Agreements;

public sealed class ClientAgreementsWithTotalDebtModel {
    public ClientAgreementsWithTotalDebtModel() {
        ClientAgreements = new List<ClientAgreement>();
    }

    public List<ClientAgreement> ClientAgreements { get; set; }

    public decimal TotalEuro { get; set; }

    public decimal TotalLocal { get; set; }
}