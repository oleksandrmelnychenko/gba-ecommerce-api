using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetCountSupplyInvoicesByContainerNetId {
    public GetCountSupplyInvoicesByContainerNetId(Guid containerNetId) {
        ContainerNetId = containerNetId;
    }

    public Guid ContainerNetId { get; set; }
}