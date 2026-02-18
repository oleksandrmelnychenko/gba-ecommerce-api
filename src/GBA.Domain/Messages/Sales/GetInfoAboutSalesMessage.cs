using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetInfoAboutSalesMessage {
    public GetInfoAboutSalesMessage(
        DateTime from,
        DateTime to,
        bool forMySales,
        Guid userNetId,
        Guid? netIdOrganization,
        Guid? netIdManager) {
        From = from.Date;
        To = to.AddHours(23).AddMinutes(59).AddSeconds(59);
        ForMySales = forMySales;
        UserNetId = userNetId;
        NetIdOrganization = netIdOrganization;
        NetIdManager = netIdManager;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public bool ForMySales { get; }

    public Guid UserNetId { get; }

    public Guid? NetIdOrganization { get; }

    public Guid? NetIdManager { get; }
}