using System.Collections.Generic;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Entities.Delivery;

public sealed class DeliveryRecipientAddress : EntityBase {
    public DeliveryRecipientAddress() {
        Sales = new HashSet<Sale>();
    }

    public string Value { get; set; }

    public string Department { get; set; }

    public string City { get; set; }

    public int Priority { get; set; }

    public long DeliveryRecipientId { get; set; }

    public DeliveryRecipient DeliveryRecipient { get; set; }

    public ICollection<Sale> Sales { get; set; }
}