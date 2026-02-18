using GBA.Domain.Entities;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncMeasureUnit {
    public string Code { get; set; }

    public string Name { get; set; }

    public string FullName { get; set; }

    public bool IsDataEqual(MeasureUnit measureUnit) {
        return Code.Equals(measureUnit.CodeOneC) && Name.Equals(measureUnit.Name) && FullName.Equals(measureUnit.Description);
    }
}