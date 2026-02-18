using System;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies;

public sealed class AddSupplyOrderMessage {
    public AddSupplyOrderMessage(SupplyOrder supplyOrder, Guid updatedByNetId) {
        SupplyOrder = supplyOrder;
        UpdatedByNetId = updatedByNetId;
    }

    public Guid UpdatedByNetId { get; set; }

    public SupplyOrder SupplyOrder { get; set; }
}