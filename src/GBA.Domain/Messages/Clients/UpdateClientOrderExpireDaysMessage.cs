using System;

namespace GBA.Domain.Messages.Clients;

public sealed class UpdateClientOrderExpireDaysMessage {
    public UpdateClientOrderExpireDaysMessage(Guid clientNetId, int expireDays) {
        ClientNetId = clientNetId;
        ExpireDays = expireDays;
    }

    public Guid ClientNetId { get; }
    public int ExpireDays { get; }
}