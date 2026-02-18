using System.Collections.Generic;

namespace GBA.Domain.Entities.Clients;

public sealed class ClientGroup : EntityBase {
    public ClientGroup() {
        Workplaces = new HashSet<Workplace>();
    }

    public string Name { get; set; }

    public long ClientId { get; set; }

    public Client Client { get; set; }

    public ICollection<Workplace> Workplaces { get; set; }
}