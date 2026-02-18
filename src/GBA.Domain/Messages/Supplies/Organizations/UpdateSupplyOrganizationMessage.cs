using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies.Organizations;

public sealed class UpdateSupplyOrganizationMessage {
    public UpdateSupplyOrganizationMessage(SupplyOrganization supplyOrganization) {
        SupplyOrganization = supplyOrganization;
    }

    public SupplyOrganization SupplyOrganization { get; set; }
}