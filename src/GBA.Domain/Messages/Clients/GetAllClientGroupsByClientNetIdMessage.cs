using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetAllClientGroupsByClientNetIdMessage {
    public GetAllClientGroupsByClientNetIdMessage(Guid clientNetId) {
        ClientNetId = clientNetId;
    }

    public Guid ClientNetId { get; }
}