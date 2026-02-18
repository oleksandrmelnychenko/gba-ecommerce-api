using System;

namespace GBA.Domain.Messages.GbaData;

public sealed class GetProductsFilteredMessage {
    public GetProductsFilteredMessage(DateTime dateFrom, DateTime dateTo, int limit, int offset) {
        DateFrom = dateFrom;
        DateTo = dateTo;
        Limit = limit;
        Offset = offset;
    }

    public DateTime DateFrom { get; }
    public DateTime DateTo { get; }
    public int Limit { get; }
    public int Offset { get; }
}