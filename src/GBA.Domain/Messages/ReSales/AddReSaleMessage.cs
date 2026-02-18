using System;
using GBA.Domain.EntityHelpers.ReSaleModels;

namespace GBA.Domain.Messages.ReSales;

public sealed class AddReSaleMessage {
    public AddReSaleMessage(ReSaleWithReSaleAvailabilityModel reSale, Guid userNetId) {
        ReSale = reSale;
        UserNetId = userNetId;
    }

    public ReSaleWithReSaleAvailabilityModel ReSale { get; }
    public Guid UserNetId { get; }
}