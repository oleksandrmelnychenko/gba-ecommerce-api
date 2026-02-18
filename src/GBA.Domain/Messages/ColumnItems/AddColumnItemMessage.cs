using System;
using GBA.Domain.FilterEntities;

namespace GBA.Domain.Messages.ColumnItems;

public sealed class AddColumnItemMessage {
    public AddColumnItemMessage(ColumnItem columnItem, Guid netId) {
        ColumnItem = columnItem;

        NetId = netId;
    }

    public ColumnItem ColumnItem { get; set; }

    public Guid NetId { get; set; }
}