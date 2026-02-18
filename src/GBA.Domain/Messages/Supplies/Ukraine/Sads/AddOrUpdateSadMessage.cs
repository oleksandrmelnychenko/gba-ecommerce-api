using System;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.Sads;

public sealed class AddOrUpdateSadMessage {
    public AddOrUpdateSadMessage(Sad sad, Guid userNetId) {
        Sad = sad;

        UserNetId = userNetId;
    }

    public Sad Sad { get; set; }

    public Guid UserNetId { get; }
}