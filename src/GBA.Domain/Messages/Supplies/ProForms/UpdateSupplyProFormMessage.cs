using System;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies;

public sealed class UpdateSupplyProFormMessage {
    public UpdateSupplyProFormMessage(Guid supplyOrderNetId, SupplyProForm supplyProForm) {
        SupplyOrderNetId = supplyOrderNetId;
        SupplyProForm = supplyProForm;
    }

    public SupplyProForm SupplyProForm { get; set; }

    public Guid SupplyOrderNetId { get; set; }
}