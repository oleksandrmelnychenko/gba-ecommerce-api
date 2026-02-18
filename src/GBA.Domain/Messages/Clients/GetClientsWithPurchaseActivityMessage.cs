using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetClientsWithPurchaseActivityMessage {
    public GetClientsWithPurchaseActivityMessage(
        bool forMyClients,
        Guid userNetId,
        long limit,
        long offset) {
        Limit = limit;
        Offset = offset;
        ForMyClients = forMyClients;
        UserNetId = userNetId;
    }

    public bool ForMyClients { get; }

    public Guid UserNetId { get; }

    public long Limit { get; }

    public long Offset { get; }
}