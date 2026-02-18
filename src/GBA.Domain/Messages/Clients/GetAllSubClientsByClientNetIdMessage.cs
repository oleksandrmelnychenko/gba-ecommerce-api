using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetAllSubClientsByClientNetIdMessage {
    public GetAllSubClientsByClientNetIdMessage(Guid clientNetId) {
        ClientNetId = clientNetId;
    }

    public Guid ClientNetId { get; set; }
}