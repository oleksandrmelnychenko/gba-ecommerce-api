using System;

namespace GBA.Domain.Messages.Countries;

public sealed class GetCountryByNetIdMessage {
    public GetCountryByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}