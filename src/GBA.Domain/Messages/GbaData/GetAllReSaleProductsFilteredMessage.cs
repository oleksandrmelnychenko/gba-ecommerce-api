using System;

namespace GBA.Domain.Messages.GbaData;

public sealed class GetAllReSaleProductsFilteredMessage {
    public GetAllReSaleProductsFilteredMessage(DateTime fromDate, DateTime toDate) {
        FromDate = fromDate;
        ToDate = toDate;
    }

    public DateTime FromDate { get; }
    public DateTime ToDate { get; }
}