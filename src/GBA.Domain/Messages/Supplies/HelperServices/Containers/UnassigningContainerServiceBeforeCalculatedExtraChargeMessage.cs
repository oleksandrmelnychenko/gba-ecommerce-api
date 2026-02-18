using System;

namespace GBA.Domain.Messages.Supplies.HelperServices.Containers;

public sealed class UnassigningContainerServiceBeforeCalculatedExtraChargeMessage {
    public UnassigningContainerServiceBeforeCalculatedExtraChargeMessage(
        Guid supplyOrderNetId,
        Guid containerServiceNetId) {
        SupplyOrderNetId = supplyOrderNetId;
        ContainerServiceNetId = containerServiceNetId;
    }

    public Guid SupplyOrderNetId { get; }

    public Guid ContainerServiceNetId { get; }
}