using System;
using GBA.Domain.EntityHelpers.SalesModels.ChartOfSalesModels;

namespace GBA.Domain.Messages.Sales.Charts;

public sealed class GetChartInfoSalesByClientMessage {
    public GetChartInfoSalesByClientMessage(
        DateTime from,
        DateTime to,
        Guid netId,
        TypePeriodGrouping typePeriod) {
        From = from.Date;
        To = to.AddHours(23).AddMinutes(59).AddSeconds(59);
        NetId = netId;
        TypePeriod = typePeriod;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public Guid NetId { get; }

    public TypePeriodGrouping TypePeriod { get; }
}