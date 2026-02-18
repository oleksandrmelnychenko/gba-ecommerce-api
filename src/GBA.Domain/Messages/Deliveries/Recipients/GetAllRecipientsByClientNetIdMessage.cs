using System;

namespace GBA.Domain.Messages.Deliveries.Recipients;

public sealed class GetAllRecipientsByClientNetIdMessage {
    public GetAllRecipientsByClientNetIdMessage(Guid clientNetId) {
        ClientNetId = clientNetId;
    }

    public Guid ClientNetId { get; set; }
}