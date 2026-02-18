using System;

namespace GBA.Domain.Messages.Supplies.Organizations;

public sealed class GetAllSupplyOrganizationsFromSearchMessage {
    public GetAllSupplyOrganizationsFromSearchMessage(
        string value,
        Guid? organizationNetId) {
        Value = string.IsNullOrEmpty(value) ? string.Empty : value;
        OrganizationNetId = organizationNetId;
    }

    public string Value { get; }

    public Guid? OrganizationNetId { get; }
}