using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetAllClientSubClientsByClientNetIdMessage {
    public GetAllClientSubClientsByClientNetIdMessage(Guid clientNetId) {
        ClientNetId = clientNetId;
    }

    public Guid ClientNetId { get; set; }
}