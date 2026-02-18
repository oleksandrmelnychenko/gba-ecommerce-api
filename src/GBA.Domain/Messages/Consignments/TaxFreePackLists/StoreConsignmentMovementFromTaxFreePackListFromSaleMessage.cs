namespace GBA.Domain.Messages.Consignments.TaxFreePackLists;

public sealed class StoreConsignmentMovementFromTaxFreePackListFromSaleMessage {
    public StoreConsignmentMovementFromTaxFreePackListFromSaleMessage(long taxFreePackListId) {
        TaxFreePackListId = taxFreePackListId;
    }

    public long TaxFreePackListId { get; }
}