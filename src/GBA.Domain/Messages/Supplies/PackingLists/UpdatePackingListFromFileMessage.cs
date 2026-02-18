using System;

namespace GBA.Domain.Messages.Supplies.PackingLists;

public sealed class UpdatePackingListFromFileMessage {
    public UpdatePackingListFromFileMessage(string pathToFile, Guid packingListNetId, Guid supplyOrderNetId) {
        PathToFile = pathToFile;

        PackingListNetId = packingListNetId;

        SupplyOrderNetId = supplyOrderNetId;
    }

    public string PathToFile { get; }

    public Guid PackingListNetId { get; }

    public Guid SupplyOrderNetId { get; }
}