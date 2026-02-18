using GBA.Domain.Entities.Agreements;

namespace GBA.Domain.Messages.Agreements;

public sealed class AddCalculationTypeMessage {
    public AddCalculationTypeMessage(CalculationType calculationType) {
        CalculationType = calculationType;
    }

    public CalculationType CalculationType { get; set; }
}