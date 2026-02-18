using System;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncSettlement {
    public DateTime FromDate { get; set; }

    public SyncSettlementType SettlementType { get; set; }

    public byte[] DocumentRef { get; set; }

    public bool TypeSettlement { get; set; }

    public decimal Value { get; set; }
}