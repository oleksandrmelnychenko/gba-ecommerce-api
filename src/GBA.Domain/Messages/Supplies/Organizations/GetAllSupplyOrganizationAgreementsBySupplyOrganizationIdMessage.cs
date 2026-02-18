namespace GBA.Domain.Messages.Supplies.Organizations;

public sealed class GetAllSupplyOrganizationAgreementsBySupplyOrganizationIdMessage {
    public GetAllSupplyOrganizationAgreementsBySupplyOrganizationIdMessage(long id) {
        Id = id;
    }

    public long Id { get; }
}