using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetClientsNotToBuyAnythingMessage {
    public GetClientsNotToBuyAnythingMessage(
        DateTime from,
        DateTime to,
        string value) {
        From = from;
        To = to.AddHours(23).AddMinutes(59).AddSeconds(59);
        Value = value;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public string Value { get; }
}