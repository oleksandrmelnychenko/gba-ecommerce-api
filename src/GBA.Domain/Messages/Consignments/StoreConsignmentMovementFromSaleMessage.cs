namespace GBA.Domain.Messages.Consignments;

public sealed class StoreConsignmentMovementFromSaleMessage {
    public StoreConsignmentMovementFromSaleMessage(long saleId, object originalSender, string responseMessage) {
        SaleId = saleId;

        OriginalSender = originalSender;

        ResponseMessage = responseMessage;
    }

    public long SaleId { get; }

    public object OriginalSender { get; }

    public string ResponseMessage { get; }
}