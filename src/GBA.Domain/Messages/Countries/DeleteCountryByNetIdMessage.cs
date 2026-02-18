using System;

namespace GBA.Domain.Messages.Countries;

public sealed class DeleteCountryByNetIdMessage {
    public DeleteCountryByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}