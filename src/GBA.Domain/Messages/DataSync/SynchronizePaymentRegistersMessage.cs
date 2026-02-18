using System;
using System.Collections.Generic;
using GBA.Domain.EntityHelpers.DataSync;

namespace GBA.Domain.Messages.DataSync;

public sealed class SynchronizePaymentRegistersMessage {
    public SynchronizePaymentRegistersMessage(
        IEnumerable<SyncEntityType> syncEntityTypes,
        Guid userNetId,
        bool forAmg) {
        SyncEntityTypes = syncEntityTypes;

        UserNetId = userNetId;

        ForAmg = forAmg;
    }

    public IEnumerable<SyncEntityType> SyncEntityTypes { get; }

    public Guid UserNetId { get; }

    public bool ForAmg { get; }
}