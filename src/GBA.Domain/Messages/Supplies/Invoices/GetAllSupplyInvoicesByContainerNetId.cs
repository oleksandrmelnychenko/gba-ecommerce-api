using System;

namespace GBA.Domain.Messages.Supplies;

public sealed class GetAllSupplyInvoicesByContainerNetId {
    public GetAllSupplyInvoicesByContainerNetId(Guid containerNetId) {
        ContainerNetId = containerNetId;
    }

    public Guid ContainerNetId { get; set; }
}