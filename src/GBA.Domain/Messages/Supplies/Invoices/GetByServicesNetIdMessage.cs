using System;

namespace GBA.Domain.Messages.Supplies.Invoices;

public sealed class GetByServicesNetIdMessage {
    public GetByServicesNetIdMessage(
        Guid serviceNetId) {
        ServiceNetId = serviceNetId;
    }

    public Guid ServiceNetId { get; }
}