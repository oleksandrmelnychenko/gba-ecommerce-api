using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Entities.Regions;

public sealed class RegionCode : EntityBase {
    public RegionCode() {
        Clients = new HashSet<Client>();
    }

    public long RegionId { get; set; }

    public string Value { get; set; }

    public string City { get; set; }

    public string District { get; set; }

    public Region Region { get; set; }

    public ICollection<Client> Clients { get; set; }
}