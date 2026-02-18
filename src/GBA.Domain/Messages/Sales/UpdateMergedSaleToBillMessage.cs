using System;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Messages.Sales;

public sealed class UpdateMergedSaleToBillMessage {
    public UpdateMergedSaleToBillMessage(Sale sale, Guid userNetId) {
        Sale = sale;

        UserNetId = userNetId;
    }

    public Sale Sale { get; set; }

    public Guid UserNetId { get; set; }
}