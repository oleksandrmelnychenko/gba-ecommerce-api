using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Messages.Supplies.PackingLists;

public sealed class UpdatePackingListInvoiceDocumentsMessage {
    public UpdatePackingListInvoiceDocumentsMessage(PackingList packingList) {
        PackingList = packingList;
    }

    public PackingList PackingList { get; set; }
}