using System;

namespace GBA.Domain.Messages.Supplies.GroupedPaymentTasks;

public sealed class GetAllAvailableForPaymentGroupedPaymentTasksFilteredMessage {
    public GetAllAvailableForPaymentGroupedPaymentTasksFilteredMessage(DateTime from, DateTime to, Guid? organizationNetId, long limit, long offset) {
        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from;

        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddDays(7) : to.Date;

        OrganizationNetId = organizationNetId;

        Limit = limit <= 0 ? 20 : limit;

        Offset = offset < 0 ? 0 : offset;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public Guid? OrganizationNetId { get; }

    public long Limit { get; }

    public long Offset { get; }
}