using System.Linq;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncProductGroup {
    public byte[] SourceId { get; set; }

    public string Name { get; set; }

    public byte[] ParentSourceId { get; set; }

    public bool IsSubGroup { get; set; }

    public bool SourceIdsEqual(byte[] sourceId) {
        return sourceId != null && SourceId.SequenceEqual(sourceId);
    }

    public bool SourceIdsOrNameEqual(byte[] sourceAmgId, byte[] sourceFenixId, string name) {
        return SourceIdsEqual(sourceAmgId) || SourceIdsEqual(sourceFenixId) || Name.Equals(name);
    }

    public bool IsEntityEqual(ProductGroup productGroup) {
        return (SourceIdsEqual(productGroup.SourceAmgId) || SourceIdsEqual(productGroup.SourceFenixId)) && Name.Equals(productGroup.Name) &&
               IsSubGroup.Equals(productGroup.IsSubGroup);
    }
}