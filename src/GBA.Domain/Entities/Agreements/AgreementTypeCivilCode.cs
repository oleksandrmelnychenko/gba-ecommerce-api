using System.Collections.Generic;
using GBA.Domain.Entities.Clients.OrganizationClients;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Entities.Agreements;

public sealed class AgreementTypeCivilCode : EntityBase {
    public AgreementTypeCivilCode() {
        Agreements = new HashSet<Agreement>();

        SupplyOrganizationAgreements = new HashSet<SupplyOrganizationAgreement>();

        OrganizationClientAgreements = new HashSet<OrganizationClientAgreement>();
    }

    public string CodeOneC { get; set; }

    public string NameUK { get; set; }

    public string NamePL { get; set; }

    public ICollection<Agreement> Agreements { get; set; }

    public ICollection<SupplyOrganizationAgreement> SupplyOrganizationAgreements { get; set; }

    public ICollection<OrganizationClientAgreement> OrganizationClientAgreements { get; set; }
}