using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.SadPalletTypes;

public sealed class AddNewSadPalletTypeMessage {
    public AddNewSadPalletTypeMessage(SadPalletType sadPalletType) {
        SadPalletType = sadPalletType;
    }

    public SadPalletType SadPalletType { get; }
}