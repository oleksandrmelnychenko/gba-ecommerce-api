using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetCurrentSaleByClientAgreementOrSaleNetIdMessage {
    public GetCurrentSaleByClientAgreementOrSaleNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}