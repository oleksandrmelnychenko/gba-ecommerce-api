using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetSupplyDeliveryDocumentByNetIdMessage {
    public GetSupplyDeliveryDocumentByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}