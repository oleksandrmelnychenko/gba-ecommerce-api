using System.Linq;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncDiscount {
    public bool IsActive { get; set; }

    public byte[] ProductGroupSourceId { get; set; }

    public decimal Discount { get; set; }

    public bool SourceIdsEqual(byte[] sourceId) {
        return sourceId != null && ProductGroupSourceId.SequenceEqual(sourceId);
    }
}