namespace GBA.Domain.Messages.Clients.RetailClients;

public sealed class GetRetailClientPayedAmountBySaleIdMessage {
    public GetRetailClientPayedAmountBySaleIdMessage(long saleId) {
        SaleId = saleId;
    }

    public long SaleId { get; }
}