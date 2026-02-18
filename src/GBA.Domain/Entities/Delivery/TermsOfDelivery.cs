using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Entities.Delivery;

public sealed class TermsOfDelivery : EntityBase {
    public TermsOfDelivery() {
        Clients = new HashSet<Client>();
    }

    public string Name { get; set; }

    public ICollection<Client> Clients { get; set; }
}