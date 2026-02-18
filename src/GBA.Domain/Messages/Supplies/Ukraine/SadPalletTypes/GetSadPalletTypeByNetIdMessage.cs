using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.SadPalletTypes;

public sealed class GetSadPalletTypeByNetIdMessage {
    public GetSadPalletTypeByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}