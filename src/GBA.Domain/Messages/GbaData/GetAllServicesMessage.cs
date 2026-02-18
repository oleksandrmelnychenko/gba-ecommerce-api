using System;

namespace GBA.Domain.Messages.GbaData;

public sealed class GetAllServicesMessage {
    public GetAllServicesMessage(DateTime from, DateTime to) {
        From = from;
        To = to;
    }

    public DateTime From { get; }
    public DateTime To { get; }
}