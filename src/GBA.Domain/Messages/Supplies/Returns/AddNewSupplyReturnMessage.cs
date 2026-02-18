using System;
using GBA.Domain.Entities.Supplies.Returns;

namespace GBA.Domain.Messages.Supplies.Returns;

public sealed class AddNewSupplyReturnMessage {
    public AddNewSupplyReturnMessage(SupplyReturn supplyReturn, Guid userNetId) {
        SupplyReturn = supplyReturn;

        UserNetId = userNetId;
    }

    public SupplyReturn SupplyReturn { get; }

    public Guid UserNetId { get; }
}