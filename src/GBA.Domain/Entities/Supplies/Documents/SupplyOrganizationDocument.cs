namespace GBA.Domain.Entities.Supplies.Documents;

public sealed class SupplyOrganizationDocument : BaseDocument {
    public long SupplyOrganizationAgreementId { get; set; }

    public SupplyOrganizationAgreement SupplyOrganizationAgreement { get; set; }
}