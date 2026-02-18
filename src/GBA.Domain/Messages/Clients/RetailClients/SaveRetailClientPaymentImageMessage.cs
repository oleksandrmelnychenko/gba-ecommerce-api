using System;

namespace GBA.Domain.Messages.Clients.RetailClients;

public sealed class SaveRetailClientPaymentImageMessage {
    public SaveRetailClientPaymentImageMessage(string imgUrl, Guid retailClientNetId, Guid saleNetId) {
        ImgUrl = imgUrl;
        RetailClientNetId = retailClientNetId;
        SaleNetId = saleNetId;
    }

    public string ImgUrl { get; }
    public Guid RetailClientNetId { get; }
    public Guid SaleNetId { get; }
}