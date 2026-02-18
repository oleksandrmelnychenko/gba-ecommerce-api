using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetProductRecommendationsMessage {
    public GetProductRecommendationsMessage(Guid clientNetId) {
        ClientNetId = clientNetId;
    }

    public Guid ClientNetId { get; }
}