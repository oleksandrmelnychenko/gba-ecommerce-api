using System;
using GBA.Domain.Entities.Sales.Shipments;

namespace GBA.Domain.Messages.Sales.ShipmentLists;

public sealed class AddNewShipmentListMessage {
    public AddNewShipmentListMessage(ShipmentList shipmentList, Guid userNetId) {
        ShipmentList = shipmentList;

        UserNetId = userNetId;
    }

    public ShipmentList ShipmentList { get; }

    public Guid UserNetId { get; }
}