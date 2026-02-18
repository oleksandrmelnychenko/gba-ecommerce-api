using GBA.Domain.Entities;

namespace GBA.Domain.Messages.Organizations;

public sealed class AddOrganizationMessage {
    public AddOrganizationMessage(Organization organization) {
        Organization = organization;
    }

    public Organization Organization { get; set; }
}