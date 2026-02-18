using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Supplies.Protocols;

public sealed class SupplyOrderUkrainePaymentDeliveryProtocol : EntityBase {
    public decimal Value { get; set; }

    public double Discount { get; set; }

    public long SupplyOrderUkrainePaymentDeliveryProtocolKeyId { get; set; }

    public long UserId { get; set; }

    public long SupplyOrderUkraineId { get; set; }

    public long? SupplyPaymentTaskId { get; set; }

    public bool IsAccounting { get; set; }

    public SupplyOrderUkrainePaymentDeliveryProtocolKey SupplyOrderUkrainePaymentDeliveryProtocolKey { get; set; }

    public User User { get; set; }

    public SupplyPaymentTask SupplyPaymentTask { get; set; }

    public SupplyOrderUkraine SupplyOrderUkraine { get; set; }
}