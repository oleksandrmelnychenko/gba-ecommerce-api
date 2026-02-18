using System;

namespace GBA.Domain.Messages.Supplies.GroupedPaymentTasks;

public sealed class GetAllFutureGroupedPaymentTasksMessage {
    public GetAllFutureGroupedPaymentTasksMessage(int limit, DateTime fromDate) {
        Limit = limit;

        FromDate = fromDate;
    }

    public int Limit { get; set; }

    public DateTime FromDate { get; set; }
}