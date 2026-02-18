using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetProductMostPurchasedMessage {
    public GetProductMostPurchasedMessage(
        Guid clientNetId,
        bool byRegion
    ) {
        ClientNetId = clientNetId;

        ByRegion = byRegion;
    }

    public Guid ClientNetId { get; set; }

    public bool ByRegion { get; set; }
}