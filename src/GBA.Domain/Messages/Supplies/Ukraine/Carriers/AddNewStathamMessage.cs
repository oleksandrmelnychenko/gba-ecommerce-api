using GBA.Domain.Entities.Carriers;

namespace GBA.Domain.Messages.Supplies.Ukraine.Carriers;

public sealed class AddNewStathamMessage {
    public AddNewStathamMessage(Statham statham) {
        Statham = statham;
    }

    public Statham Statham { get; }
}