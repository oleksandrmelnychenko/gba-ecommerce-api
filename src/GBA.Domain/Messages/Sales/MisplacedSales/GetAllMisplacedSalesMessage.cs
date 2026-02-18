using System;

namespace GBA.Domain.Messages.Sales.MisplacedSales;

public sealed class GetAllMisplacedSalesMessage {
    public GetAllMisplacedSalesMessage(
        string phone,
        DateTime? from,
        DateTime? to,
        bool isAccepted,
        Guid netId) {
        Phone = phone;
        From = from ?? DateTime.UtcNow.Date;
        To = to ?? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
        IsAccepted = isAccepted;
        NetId = netId;
    }

    public string Phone { get; }
    public DateTime From { get; }
    public DateTime To { get; }
    public bool IsAccepted { get; }
    public Guid NetId { get; }
}