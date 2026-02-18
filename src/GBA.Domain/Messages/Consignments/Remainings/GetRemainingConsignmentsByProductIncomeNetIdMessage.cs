using System;

namespace GBA.Domain.Messages.Consignments.Remainings;

public sealed class GetRemainingConsignmentsByProductIncomeNetIdMessage {
    public GetRemainingConsignmentsByProductIncomeNetIdMessage(Guid productIncomeNetId) {
        ProductIncomeNetId = productIncomeNetId;
    }

    public Guid ProductIncomeNetId { get; }
}