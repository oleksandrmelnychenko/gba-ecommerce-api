using System;

namespace GBA.Domain.Messages.Sales.ShipmentLists;

public sealed class GetShipmentListByNetIdMessage {
    public GetShipmentListByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}