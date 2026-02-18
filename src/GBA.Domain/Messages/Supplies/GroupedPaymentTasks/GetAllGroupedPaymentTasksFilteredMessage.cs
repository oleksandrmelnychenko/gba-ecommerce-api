using System;
using GBA.Domain.EntityHelpers.Supplies;

namespace GBA.Domain.Messages.Supplies.GroupedPaymentTasks;

public sealed class GetAllGroupedPaymentTasksFilteredMessage {
    public GetAllGroupedPaymentTasksFilteredMessage(
        DateTime from,
        DateTime to,
        Guid? organizationNetId,
        long limit,
        long offset,
        TypePaymentTask typePaymentTask) {
        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from;

        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddDays(7) : to.Date;

        OrganizationNetId = organizationNetId;

        TypePaymentTask = typePaymentTask;

        Limit = limit <= 0 ? 20 : limit;

        Offset = offset < 0 ? 0 : offset;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public Guid? OrganizationNetId { get; }

    public TypePaymentTask TypePaymentTask { get; }

    public long Limit { get; }

    public long Offset { get; }
}