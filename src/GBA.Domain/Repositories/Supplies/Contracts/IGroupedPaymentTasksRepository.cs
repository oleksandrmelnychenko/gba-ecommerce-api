using System;
using System.Collections.Generic;
using GBA.Domain.EntityHelpers.Accounting;
using GBA.Domain.EntityHelpers.Supplies;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IGroupedPaymentTasksRepository {
    IEnumerable<GroupedPaymentTask> GetGroupedPaymentTasksForCurrentDate(int limit);

    IEnumerable<GroupedPaymentTask> GetGroupedPaymentTasksForPastFromDate(int limit, DateTime fromDate);

    IEnumerable<GroupedPaymentTask> GetGroupedPaymentTasksForFutureFromDate(int limit, DateTime fromDate);

    GroupedPaymentTaskWithTotals GetGroupedPaymentTasksFiltered(DateTime from, DateTime to, Guid? organizationNetId, long limit, long offset, TypePaymentTask typePaymentTask);

    GroupedPaymentTaskWithTotals GetAvailableForPaymentGroupedPaymentTasksFiltered(DateTime from, DateTime to, Guid? organizationNetId, long limit, long offset);
}