using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies.Organizations;

public sealed class UpdateSupplyOrganizationAgreementMessage {
    public UpdateSupplyOrganizationAgreementMessage(
        SupplyOrganizationAgreement agreement) {
        Agreement = agreement;
    }

    public SupplyOrganizationAgreement Agreement { get; }
}