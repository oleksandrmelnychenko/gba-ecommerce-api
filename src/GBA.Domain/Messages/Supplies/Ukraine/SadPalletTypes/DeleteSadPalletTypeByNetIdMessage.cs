using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.SadPalletTypes;

public sealed class DeleteSadPalletTypeByNetIdMessage {
    public DeleteSadPalletTypeByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}