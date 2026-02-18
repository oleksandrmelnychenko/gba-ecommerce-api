using GBA.Domain.Entities.Carriers;

namespace GBA.Domain.Messages.Supplies.Ukraine.Carriers;

public sealed class UpdateStathamMessage {
    public UpdateStathamMessage(Statham statham) {
        Statham = statham;
    }

    public Statham Statham { get; }
}