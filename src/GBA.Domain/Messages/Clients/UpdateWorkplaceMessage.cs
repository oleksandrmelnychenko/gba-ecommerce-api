using GBA.Domain.Entities;

namespace GBA.Domain.Messages.Clients;

public sealed class UpdateWorkplaceMessage {
    public UpdateWorkplaceMessage(Workplace workplace) {
        Workplace = workplace;
    }

    public Workplace Workplace { get; }
}