using System;

namespace GBA.Domain.Messages.Sales.ShipmentLists;

public sealed class ShipmentListForSalePrintDocumentsMessage {
    public ShipmentListForSalePrintDocumentsMessage(
        string pathToFolder,
        Guid netId) {
        PathToFolder = pathToFolder;
        NetId = netId;
    }

    public string PathToFolder { get; }
    public Guid NetId { get; }
}