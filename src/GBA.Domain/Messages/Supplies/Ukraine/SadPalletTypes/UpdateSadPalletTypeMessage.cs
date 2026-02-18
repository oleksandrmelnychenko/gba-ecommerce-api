using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.SadPalletTypes;

public sealed class UpdateSadPalletTypeMessage {
    public UpdateSadPalletTypeMessage(SadPalletType sadPalletType) {
        SadPalletType = sadPalletType;
    }

    public SadPalletType SadPalletType { get; }
}