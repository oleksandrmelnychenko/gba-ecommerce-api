using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Entities.Delivery;

public sealed class DeliveryRecipient : EntityBase {
    public DeliveryRecipient() {
        Sales = new HashSet<Sale>();

        DeliveryRecipientAddresses = new HashSet<DeliveryRecipientAddress>();
    }

    public string FullName { get; set; }

    public int Priority { get; set; }

    public long ClientId { get; set; }

    public string MobilePhone { get; set; }

    public Client Client { get; set; }

    public ICollection<Sale> Sales { get; set; }

    public ICollection<DeliveryRecipientAddress> DeliveryRecipientAddresses { get; set; }
}