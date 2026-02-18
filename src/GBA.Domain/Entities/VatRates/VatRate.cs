using System.Collections.Generic;

namespace GBA.Domain.Entities.VatRates;

public sealed class VatRate : EntityBase {
    public VatRate() {
        Organizations = new HashSet<Organization>();
    }

    public double Value { get; set; }

    public ICollection<Organization> Organizations { get; set; }
}