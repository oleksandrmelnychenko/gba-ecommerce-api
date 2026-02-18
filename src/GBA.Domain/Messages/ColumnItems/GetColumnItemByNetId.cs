using System;

namespace GBA.Domain.Messages.ColumnItems;

public sealed class GetColumnItemByNetId {
    public GetColumnItemByNetId(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}