using System;

namespace GBA.Domain.Messages.Products.ProductReservations;

public sealed class GetCurrentReservationsByProductAndClientAgreementNetIdsMessage {
    public GetCurrentReservationsByProductAndClientAgreementNetIdsMessage(
        Guid productNetId,
        Guid clientAgreementNetId) {
        ProductNetId = productNetId;

        ClientAgreementNetId = clientAgreementNetId;
    }

    public Guid ProductNetId { get; }

    public Guid ClientAgreementNetId { get; }
}