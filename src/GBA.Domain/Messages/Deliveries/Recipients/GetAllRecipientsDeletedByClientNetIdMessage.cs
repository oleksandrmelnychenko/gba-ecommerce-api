using System;

namespace GBA.Domain.Messages.Deliveries.Recipients;

public sealed class GetAllRecipientsDeletedByClientNetIdMessage {
    public GetAllRecipientsDeletedByClientNetIdMessage(Guid clientNetId) {
        ClientNetId = clientNetId;
    }

    public Guid ClientNetId { get; set; }
}