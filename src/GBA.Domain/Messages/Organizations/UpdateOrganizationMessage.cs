using GBA.Domain.Entities;

namespace GBA.Domain.Messages.Organizations;

public sealed class UpdateOrganizationMessage {
    public UpdateOrganizationMessage(Organization organization) {
        Organization = organization;
    }

    public Organization Organization { get; set; }
}