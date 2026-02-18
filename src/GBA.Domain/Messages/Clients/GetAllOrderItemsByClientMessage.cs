using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetAllOrderItemsByClientMessage {
    public GetAllOrderItemsByClientMessage(
        Guid clientNetId) {
        ClientNetId = clientNetId;
    }

    public Guid ClientNetId { get; }
}