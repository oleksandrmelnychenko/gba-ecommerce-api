using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetAllClientGroupMessage {
    public GetAllClientGroupMessage(Guid clientNetId) {
        ClientNetId = clientNetId;
    }

    public Guid ClientNetId { get; }
}