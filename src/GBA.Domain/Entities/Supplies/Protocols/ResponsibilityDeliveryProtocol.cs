using GBA.Common.Helpers;

namespace GBA.Domain.Entities.Supplies.Protocols;

public sealed class ResponsibilityDeliveryProtocol : EntityBase {
    public SupplyOrderStatus SupplyOrderStatus { get; set; }

    public long UserId { get; set; }

    public long SupplyOrderId { get; set; }

    public User User { get; set; }

    public SupplyOrder SupplyOrder { get; set; }
}