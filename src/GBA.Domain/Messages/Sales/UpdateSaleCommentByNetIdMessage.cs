using System;

namespace GBA.Domain.Messages.Sales;

public sealed class UpdateSaleCommentByNetIdMessage {
    public UpdateSaleCommentByNetIdMessage(Guid netId, string comment) {
        NetId = netId;
        Comment = comment;
    }

    public Guid NetId { get; }
    public string Comment { get; }
}