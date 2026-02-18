using System;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Messages.Supplies.PackingLists;

public class UpdatePlacementPackingListMessage {
    public UpdatePlacementPackingListMessage(
        PackingList packingList,
        Guid userNetId,
        Guid invoiceNetId) {
        PackingList = packingList;
        UserNetId = userNetId;
        InvoiceNetId = invoiceNetId;
    }

    public PackingList PackingList { get; }
    public Guid UserNetId { get; }
    public Guid InvoiceNetId { get; }
}