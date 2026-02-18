using System;

namespace GBA.Domain.Messages.Clients.RetailClients;

public sealed class GetShoppingCartByRetailClientNetIdMessage {
    public GetShoppingCartByRetailClientNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}