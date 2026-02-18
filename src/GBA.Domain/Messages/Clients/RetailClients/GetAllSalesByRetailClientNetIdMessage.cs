using System;

namespace GBA.Domain.Messages.Clients.RetailClients;

public sealed class GetAllSalesByRetailClientNetIdMessage {
    public GetAllSalesByRetailClientNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}