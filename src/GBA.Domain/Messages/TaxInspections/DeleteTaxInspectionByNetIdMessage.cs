using System;

namespace GBA.Domain.Messages.TaxInspections;

public sealed class DeleteTaxInspectionByNetIdMessage {
    public DeleteTaxInspectionByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}