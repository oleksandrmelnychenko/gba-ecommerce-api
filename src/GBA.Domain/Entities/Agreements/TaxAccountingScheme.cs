using System.Collections.Generic;
using GBA.Domain.Entities.Clients.OrganizationClients;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.EntityHelpers.Agreements;

namespace GBA.Domain.Entities.Agreements;

public sealed class TaxAccountingScheme : EntityBase {
    public TaxAccountingScheme() {
        Agreements = new HashSet<Agreement>();

        SupplyOrganizationAgreements = new HashSet<SupplyOrganizationAgreement>();

        OrganizationClientAgreements = new HashSet<OrganizationClientAgreement>();
    }

    public string CodeOneC { get; set; }

    public TaxBaseMoment PurchaseTaxBaseMoment { get; set; }

    public TaxBaseMoment SaleTaxBaseMoment { get; set; }

    public string NameUK { get; set; }

    public string NamePL { get; set; }

    public ICollection<Agreement> Agreements { get; set; }

    public ICollection<SupplyOrganizationAgreement> SupplyOrganizationAgreements { get; set; }

    public ICollection<OrganizationClientAgreement> OrganizationClientAgreements { get; set; }
}