using System;
using GBA.Domain.FilterEntities;

namespace GBA.Domain.Messages.ColumnItems;

public sealed class GetAllColumnItemsByTypeAndUserNetIdMessage {
    public GetAllColumnItemsByTypeAndUserNetIdMessage(FilterEntityType type, Guid netId) {
        Type = type;

        NetId = netId;
    }

    public FilterEntityType Type { get; set; }

    public Guid NetId { get; set; }
}