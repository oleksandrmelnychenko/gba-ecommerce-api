using System;

namespace GBA.Domain.Messages.Categories;

public sealed class DeleteCategoryMessage {
    public DeleteCategoryMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}