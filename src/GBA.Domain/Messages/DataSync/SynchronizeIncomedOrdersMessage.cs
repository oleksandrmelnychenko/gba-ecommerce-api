using System;

namespace GBA.Domain.Messages.DataSync;

public sealed class SynchronizeIncomedOrdersMessage {
    public SynchronizeIncomedOrdersMessage(
        Guid userNetId,
        bool forAmg) {
        UserNetId = userNetId;
        ForAmg = forAmg;
    }

    public Guid UserNetId { get; }

    public bool ForAmg { get; }
}