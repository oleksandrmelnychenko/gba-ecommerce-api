using System;

namespace GBA.Domain.Entities.Supplies.Documents;

public sealed class SupplyOrderDeliveryDocument : BaseDocument {
    public bool IsNotified { get; set; }

    public bool IsReceived { get; set; }

    public bool IsProcessed { get; set; }

    public string Comment { get; set; }

    public DateTime ProcessedDate { get; set; } = DateTime.Now;

    public long UserId { get; set; }

    public long SupplyOrderId { get; set; }

    public long SupplyDeliveryDocumentId { get; set; }

    public User User { get; set; }

    public SupplyOrder SupplyOrder { get; set; }

    public SupplyDeliveryDocument SupplyDeliveryDocument { get; set; }
}