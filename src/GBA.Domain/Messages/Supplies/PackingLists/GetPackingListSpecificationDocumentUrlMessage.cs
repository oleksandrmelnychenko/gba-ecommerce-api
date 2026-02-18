using System;

namespace GBA.Domain.Messages.Supplies.PackingLists;

public sealed class GetPackingListSpecificationDocumentUrlMessage {
    public GetPackingListSpecificationDocumentUrlMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}