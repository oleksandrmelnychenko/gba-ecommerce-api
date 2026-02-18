using System;

namespace GBA.Domain.Messages.TaxInspections;

public sealed class GetTaxInspectionByNetIdMessage {
    public GetTaxInspectionByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}