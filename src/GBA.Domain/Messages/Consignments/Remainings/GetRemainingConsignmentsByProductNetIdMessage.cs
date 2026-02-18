using System;

namespace GBA.Domain.Messages.Consignments.Remainings;

public sealed class GetRemainingConsignmentsByProductNetIdMessage {
    public GetRemainingConsignmentsByProductNetIdMessage(Guid productNetId) {
        ProductNetId = productNetId;
    }

    public Guid ProductNetId { get; }
}