using GBA.Domain.Entities;

namespace GBA.Domain.Messages.Workplaces;

public sealed class AddWorkplaceMessage {
    public AddWorkplaceMessage(Workplace workplace) {
        Workplace = workplace;
    }

    public Workplace Workplace { get; }
}