using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetComponentsByProductNetIdMessage {
    public GetComponentsByProductNetIdMessage(Guid productNetId, Guid clientAgreementNetId) {
        ProductNetId = productNetId;

        ClientAgreementNetId = clientAgreementNetId;
    }

    public Guid ProductNetId { get; }

    public Guid ClientAgreementNetId { get; }
}