using System.Collections.Generic;

namespace GBA.Domain.Entities.Clients.PackingMarkings;

public sealed class PackingMarking : EntityBase {
    public PackingMarking() {
        Clients = new HashSet<Client>();
    }

    public string Name { get; set; }

    public ICollection<Client> Clients { get; set; }
}