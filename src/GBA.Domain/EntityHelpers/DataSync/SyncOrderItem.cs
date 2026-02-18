using System;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncOrderItem {
    public string DocumentNumber { get; set; }

    public DateTime DocumentDate { get; set; }

    public long ProductCode { get; set; }

    public double Qty { get; set; }
}