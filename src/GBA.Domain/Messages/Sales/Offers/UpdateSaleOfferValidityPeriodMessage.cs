using System;

namespace GBA.Domain.Messages.Sales.Offers;

public sealed class UpdateSaleOfferValidityPeriodMessage {
    public UpdateSaleOfferValidityPeriodMessage(Guid netId, int validDays) {
        NetId = netId;

        ValidDays = validDays;
    }

    public Guid NetId { get; }

    public int ValidDays { get; }
}