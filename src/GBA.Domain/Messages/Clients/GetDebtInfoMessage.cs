using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetDebtInfoMessage {
    public GetDebtInfoMessage(Guid clientNetId) {
        ClientNetId = clientNetId;
    }

    public Guid ClientNetId { get; set; }
}