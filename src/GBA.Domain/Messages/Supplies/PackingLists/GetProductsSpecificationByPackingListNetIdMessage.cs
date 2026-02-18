using System;

namespace GBA.Domain.Messages.Supplies.PackingLists;

public sealed class GetProductsSpecificationByPackingListNetIdMessage {
    public GetProductsSpecificationByPackingListNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}