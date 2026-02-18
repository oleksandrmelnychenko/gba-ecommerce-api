using System;

namespace GBA.Domain.Messages.Categories;

public sealed class GetCategoryByNetIdMessage {
    public GetCategoryByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}