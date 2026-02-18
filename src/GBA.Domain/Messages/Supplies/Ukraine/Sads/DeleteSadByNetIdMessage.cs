using System;

namespace GBA.Domain.Messages.Supplies.Ukraine.Sads;

public sealed class DeleteSadByNetIdMessage {
    public DeleteSadByNetIdMessage(Guid sadNetId, Guid userNetId) {
        SadNetId = sadNetId;

        UserNetId = userNetId;
    }

    public Guid SadNetId { get; }

    public Guid UserNetId { get; }
}