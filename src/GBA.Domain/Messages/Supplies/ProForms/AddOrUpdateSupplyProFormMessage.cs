using System;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies;

public sealed class AddOrUpdateSupplyProFormMessage {
    public AddOrUpdateSupplyProFormMessage(
        Guid supplyOrderNetId,
        SupplyProForm supplyProForm,
        Guid userNetId) {
        SupplyOrderNetId = supplyOrderNetId;

        SupplyProForm = supplyProForm;

        UserNetId = userNetId;
    }

    public Guid SupplyOrderNetId { get; set; }

    public SupplyProForm SupplyProForm { get; set; }

    public Guid UserNetId { get; }
}