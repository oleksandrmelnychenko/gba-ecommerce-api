using System;

namespace GBA.Domain.Messages.Regions;

public sealed class IsRegionCodeAvailableMessage {
    public IsRegionCodeAvailableMessage(string value, Guid netId) {
        Value = value;

        NetId = netId;
    }

    public string Value { get; set; }

    public Guid NetId { get; set; }
}