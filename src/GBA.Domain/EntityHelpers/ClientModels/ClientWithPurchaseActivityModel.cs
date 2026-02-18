using System;

namespace GBA.Domain.EntityHelpers.ClientModels;

public sealed class ClientWithPurchaseActivityModel {
    public Guid ClientNetId { get; set; }

    public string ClientName { get; set; }

    public DateTime CreatedClient { get; set; }

    public double QtyDayFromLastOrder { get; set; }

    public bool IsExistAccount { get; set; }

    public string ManagerName { get; set; }

    public DateTime? LastSale { get; set; }
}