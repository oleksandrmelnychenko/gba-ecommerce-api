using GBA.Domain.Entities;

namespace GBA.Domain.Messages.Measures;

public sealed class AddMeasureUnitMessage {
    public AddMeasureUnitMessage(MeasureUnit measureUnit) {
        MeasureUnit = measureUnit;
    }

    public MeasureUnit MeasureUnit { get; set; }
}