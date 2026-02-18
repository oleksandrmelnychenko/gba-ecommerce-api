using System;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Messages.Sales;

public sealed class UpdateOneTimeDiscountsOnSaleMessage {
    public UpdateOneTimeDiscountsOnSaleMessage(Sale sale, Guid userNetId) {
        Sale = sale;

        UserNetId = userNetId;
    }

    public Sale Sale { get; }

    public Guid UserNetId { get; }
}