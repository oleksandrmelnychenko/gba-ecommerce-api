using System.Collections.Generic;

namespace GBA.Domain.Entities.Clients.PackingMarkings;

public sealed class PackingMarkingPayment : EntityBase {
    public PackingMarkingPayment() {
        Clients = new HashSet<Client>();
    }

    public string Name { get; set; }

    public ICollection<Client> Clients { get; set; }
}