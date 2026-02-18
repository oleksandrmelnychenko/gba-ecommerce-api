namespace GBA.Domain.Messages.Sales;

public sealed class GetUpdateDataCarrierMessage {
    public GetUpdateDataCarrierMessage(long saleId) {
        SaleId = saleId;
    }

    public long SaleId { get; set; }
}