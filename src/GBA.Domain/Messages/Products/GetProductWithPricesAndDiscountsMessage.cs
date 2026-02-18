using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetProductWithPricesAndDiscountsMessage {
    public GetProductWithPricesAndDiscountsMessage(Guid productNetId, Guid clientAgreementNetId) {
        ProductNetId = productNetId;

        ClientAgreementNetId = clientAgreementNetId;
    }

    public Guid ProductNetId { get; set; }

    public Guid ClientAgreementNetId { get; set; }
}