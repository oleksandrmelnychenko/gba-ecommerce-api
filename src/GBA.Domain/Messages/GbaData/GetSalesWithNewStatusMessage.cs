using System;

namespace GBA.Domain.Messages.GbaData;

public class GetSalesWithNewStatusMessage {
    public GetSalesWithNewStatusMessage(DateTime dateFrom, DateTime dateTo) {
        DateFrom = dateFrom;
        DateTo = dateTo;
    }

    public DateTime DateFrom { get; }
    public DateTime DateTo { get; }
}