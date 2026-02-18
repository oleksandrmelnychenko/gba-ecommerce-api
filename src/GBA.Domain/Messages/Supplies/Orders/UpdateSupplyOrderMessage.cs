using System;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies;

public sealed class UpdateSupplyOrderMessage {
    public UpdateSupplyOrderMessage(SupplyOrder supplyOrder, Guid updatedByNetId) {
        SupplyOrder = supplyOrder;

        UpdatedByNetId = updatedByNetId;
    }

    public SupplyOrder SupplyOrder { get; set; }

    public Guid UpdatedByNetId { get; set; }
}