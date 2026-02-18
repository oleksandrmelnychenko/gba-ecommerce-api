namespace GBA.Domain.Messages.Consignments.TaxFreePackLists;

public sealed class StoreConsignmentMovementFromTaxFreePackListMessage {
    public StoreConsignmentMovementFromTaxFreePackListMessage(long taxFreePackListId) {
        TaxFreePackListId = taxFreePackListId;
    }

    public long TaxFreePackListId { get; }
}