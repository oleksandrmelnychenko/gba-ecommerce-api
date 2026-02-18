using System;

namespace GBA.Domain.Messages.PaymentOrders.AdvancePayments;

public sealed class GetAllAdvancePaymentsFilteredMessage {
    public GetAllAdvancePaymentsFilteredMessage(DateTime fromDate, DateTime toDate) {
        FromDate = fromDate.Year.Equals(1) ? DateTime.Now.Date : fromDate.Date;
        ToDate = toDate.Year.Equals(1) ? DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : toDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public DateTime FromDate { get; }
    public DateTime ToDate { get; }
}