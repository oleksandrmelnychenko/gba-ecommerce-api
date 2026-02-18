using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Entities.Regions;

public sealed class Region : EntityBase {
    public Region() {
        Clients = new HashSet<Client>();

        RegionCodes = new HashSet<RegionCode>();
    }

    public string Name { get; set; }

    public ICollection<Client> Clients { get; set; }

    public ICollection<RegionCode> RegionCodes { get; set; }
}