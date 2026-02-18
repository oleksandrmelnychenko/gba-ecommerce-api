using System;

namespace GBA.Domain.Messages.Products.ProductAvailabilities;

public sealed class GetAllProductAvailabilitiesMessage {
    public GetAllProductAvailabilitiesMessage(
        Guid netId,
        Guid clientAgreementNetId,
        Guid saleNetId) {
        NetId = netId;
        ClientAgreementNetId = clientAgreementNetId;
        SaleNetId = saleNetId;
    }

    public Guid NetId { get; }
    public Guid ClientAgreementNetId { get; }
    public Guid SaleNetId { get; }
}