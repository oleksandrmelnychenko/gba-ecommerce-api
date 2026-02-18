using System;

namespace GBA.Domain.Messages.Supplies.DeliveryProductProtocols;

public sealed class GetAllFilteredDeliveryProductProtocolMessage {
    public GetAllFilteredDeliveryProductProtocolMessage(
        DateTime from,
        DateTime to,
        string organization,
        string supplier,
        int limit,
        int offset) {
        From = from.Date;
        To = to.AddHours(23).AddMinutes(59).AddSeconds(59);
        Organization = organization ?? "";
        Supplier = supplier ?? "";
        Limit = limit;
        Offset = offset;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public string Organization { get; }

    public string Supplier { get; }

    public int Limit { get; }

    public int Offset { get; }
}