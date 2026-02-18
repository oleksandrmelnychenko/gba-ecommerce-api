using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetAvgByClientAndProductMessage {
    public GetAvgByClientAndProductMessage(Guid clientNetId, Guid productNetId) {
        ClientNetId = clientNetId;

        ProductNetId = productNetId;
    }

    public Guid ClientNetId { get; set; }

    public Guid ProductNetId { get; set; }
}