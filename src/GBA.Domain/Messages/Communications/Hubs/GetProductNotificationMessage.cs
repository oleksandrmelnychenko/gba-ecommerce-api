using System;

namespace GBA.Domain.Messages.Communications.Hubs;

public sealed class GetProductNotificationMessage {
    public GetProductNotificationMessage(long productId, Guid clientAgreementId) {
        ProductId = productId;

        ClientAgreementId = clientAgreementId;
    }

    public long ProductId { get; }

    public Guid ClientAgreementId { get; }
}