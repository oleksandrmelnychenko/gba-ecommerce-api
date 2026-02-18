using System;
using GBA.Domain.EntityHelpers.SalesModels.ChartOfSalesModels;

namespace GBA.Domain.Messages.TotalDashboards;

public sealed class GetFilteredGroupedPaymentsByPeriodMessage {
    public GetFilteredGroupedPaymentsByPeriodMessage(
        DateTime? from,
        DateTime? to,
        TypePeriodGrouping period,
        Guid? netId) {
        From = from?.Date ?? DateTime.Now.Date.AddDays(-1);
        To = to?.Date.AddHours(23).AddMinutes(59).AddSeconds(59) ?? DateTime.Now.AddHours(23).AddMinutes(59).AddSeconds(59);
        Period = period;
        NetId = netId;
    }

    public DateTime From { get; }
    public DateTime To { get; }
    public TypePeriodGrouping Period { get; }
    public Guid? NetId { get; }
}