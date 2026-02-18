using System;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Messages.Sales;

public sealed class UpdateSaleMessage {
    public UpdateSaleMessage(Sale sale, Guid updatedByNetId) {
        Sale = sale;
        UpdatedByNetId = updatedByNetId;
    }

    public Sale Sale { get; set; }

    public Guid UpdatedByNetId { get; set; }
}