using GBA.Domain.Entities.Agreements;

namespace GBA.Domain.Messages.Agreements;

public sealed class UpdateCalculationTypeMessage {
    public UpdateCalculationTypeMessage(CalculationType calculationType) {
        CalculationType = calculationType;
    }

    public CalculationType CalculationType { get; set; }
}