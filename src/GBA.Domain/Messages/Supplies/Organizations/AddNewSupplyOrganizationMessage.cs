using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies.Organizations;

public sealed class AddNewSupplyOrganizationMessage {
    public AddNewSupplyOrganizationMessage(SupplyOrganization supplyOrganization) {
        SupplyOrganization = supplyOrganization;
    }

    public SupplyOrganization SupplyOrganization { get; set; }
}