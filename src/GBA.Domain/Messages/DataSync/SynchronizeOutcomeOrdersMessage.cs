using System;

namespace GBA.Domain.Messages.DataSync;

public sealed class SynchronizeOutcomeOrdersMessage {
    public SynchronizeOutcomeOrdersMessage(
        Guid userNetId,
        bool forAmg,
        DateTime from,
        DateTime to) {
        UserNetId = userNetId;
        ForAmg = forAmg;
        From = from.Date;
        To = to.Date.AddHours(23.99);
    }

    public Guid UserNetId { get; }

    public bool ForAmg { get; }
    public DateTime From { get; }
    public DateTime To { get; }
}