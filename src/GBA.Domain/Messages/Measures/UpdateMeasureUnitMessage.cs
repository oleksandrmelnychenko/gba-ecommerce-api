using GBA.Domain.Entities;

namespace GBA.Domain.Messages.Measures;

public sealed class UpdateMeasureUnitMessage {
    public UpdateMeasureUnitMessage(MeasureUnit measureUnit) {
        MeasureUnit = measureUnit;
    }

    public MeasureUnit MeasureUnit { get; set; }
}