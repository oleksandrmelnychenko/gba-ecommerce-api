using System;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Messages.Sales;

public sealed class AddSaleMessage {
    public AddSaleMessage(Sale sale, Guid userNetId) {
        Sale = sale;
        UserNetId = userNetId;
    }

    public Sale Sale { get; set; }

    public Guid UserNetId { get; set; }
}