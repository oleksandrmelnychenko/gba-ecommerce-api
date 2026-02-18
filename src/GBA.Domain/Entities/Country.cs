using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Entities;

public sealed class Country : EntityBase {
    public Country() {
        Clients = new HashSet<Client>();
    }

    public string Name { get; set; }

    public string Code { get; set; }

    public ICollection<Client> Clients { get; set; }
}