using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetProductCoPurchaseRecommendationsMessage {
    public GetProductCoPurchaseRecommendationsMessage(
        Guid clientNetId,
        Guid productNetId,
        bool byRegion
    ) {
        ClientNetId = clientNetId;

        ProductNetId = productNetId;

        ByRegion = byRegion;
    }

    public Guid ClientNetId { get; set; }

    public Guid ProductNetId { get; set; }

    public bool ByRegion { get; set; }
}