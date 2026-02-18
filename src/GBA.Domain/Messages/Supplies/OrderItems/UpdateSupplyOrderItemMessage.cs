using System;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies;

public sealed class UpdateSupplyOrderItemMessage {
    public UpdateSupplyOrderItemMessage(SupplyOrderItem supplyOrderItem, Guid updatedByNetId) {
        SupplyOrderItem = supplyOrderItem;

        UpdatedByNetId = updatedByNetId;
    }

    public SupplyOrderItem SupplyOrderItem { get; set; }

    public Guid UpdatedByNetId { get; set; }
}