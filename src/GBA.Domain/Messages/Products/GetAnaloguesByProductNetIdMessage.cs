using System;

namespace GBA.Domain.Messages.Products;

public sealed class GetAnaloguesByProductNetIdMessage {
    public GetAnaloguesByProductNetIdMessage(Guid productNetId, Guid clientAgreementNetId) {
        ProductNetId = productNetId;

        ClientAgreementNetId = clientAgreementNetId;
    }

    public Guid ProductNetId { get; }

    public Guid ClientAgreementNetId { get; }
}