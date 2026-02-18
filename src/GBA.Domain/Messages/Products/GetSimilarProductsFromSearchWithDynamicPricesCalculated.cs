using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetSimilarProductsFromSearchWithDynamicPricesCalculated {
    public GetSimilarProductsFromSearchWithDynamicPricesCalculated(string value, Guid clientAgreementNetId, Guid exceptNetId) {
        Value = value;

        ClientAgreementNetId = clientAgreementNetId;

        ExceptNetId = exceptNetId;
    }

    public string Value { get; }

    public Guid ClientAgreementNetId { get; }

    public Guid ExceptNetId { get; }
}