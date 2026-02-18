using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies.Organizations;

public sealed class AddNewSupplyOrganizationAgreementMessage {
    public AddNewSupplyOrganizationAgreementMessage(
        SupplyOrganizationAgreement agreement) {
        Agreement = agreement;
    }

    public SupplyOrganizationAgreement Agreement { get; }
}