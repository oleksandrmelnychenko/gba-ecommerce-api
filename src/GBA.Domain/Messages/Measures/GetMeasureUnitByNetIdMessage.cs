using System;

namespace GBA.Domain.Messages.Measures;

public sealed class GetMeasureUnitByNetIdMessage {
    public GetMeasureUnitByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}