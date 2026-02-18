using System;

namespace GBA.Domain.Messages.DataSync;

public sealed class GetDataSyncInfoMessage {
    public GetDataSyncInfoMessage(
        bool forAmg,
        DateTime from,
        DateTime to) {
        ForAmg = forAmg;
        From = from.Date;
        To = to.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public bool ForAmg { get; }
    public DateTime From { get; }
    public DateTime To { get; }
}