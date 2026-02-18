using System;

namespace GBA.Domain.Messages.Supplies.Organizations;

public sealed class GetAllSupplyOrganizationsMessage {
    public GetAllSupplyOrganizationsMessage(
        Guid? organizationNetId) {
        OrganizationNetId = organizationNetId;
    }

    public Guid? OrganizationNetId { get; }
}