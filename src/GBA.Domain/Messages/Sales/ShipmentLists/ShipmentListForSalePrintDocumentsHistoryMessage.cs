using System;

namespace GBA.Domain.Messages.Sales.ShipmentLists;

public sealed class ShipmentListForSalePrintDocumentsHistoryMessage {
    public ShipmentListForSalePrintDocumentsHistoryMessage(
        string pathToFolder,
        Guid netId,
        Guid historyNetId) {
        PathToFolder = pathToFolder;
        NetId = netId;
        HistoryNetId = historyNetId;
    }

    public string PathToFolder { get; }
    public Guid NetId { get; }
    public Guid HistoryNetId { get; }
}