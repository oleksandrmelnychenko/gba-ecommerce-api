using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetAllSubClientsSalesByClientNetIdMessage {
    public GetAllSubClientsSalesByClientNetIdMessage(Guid clientNetId) {
        ClientNetId = clientNetId;
    }

    public Guid ClientNetId { get; set; }
}