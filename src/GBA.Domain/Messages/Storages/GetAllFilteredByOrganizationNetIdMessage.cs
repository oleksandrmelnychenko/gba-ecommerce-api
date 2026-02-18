using System;

namespace GBA.Domain.Messages.Storages;

public sealed class GetAllFilteredByOrganizationNetIdMessage {
    public GetAllFilteredByOrganizationNetIdMessage(Guid organizationNetId, bool skipDefective) {
        OrganizationNetId = organizationNetId;

        SkipDefective = skipDefective;
    }

    public Guid OrganizationNetId { get; }

    public bool SkipDefective { get; }
}