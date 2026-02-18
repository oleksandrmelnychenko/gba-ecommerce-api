using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetRootClientBySubClientNetIdMessage {
    public GetRootClientBySubClientNetIdMessage(Guid subClientNetId) {
        SubClientNetId = subClientNetId;
    }

    public Guid SubClientNetId { get; set; }
}