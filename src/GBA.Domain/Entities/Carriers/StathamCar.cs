using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Carriers;

public sealed class StathamCar : EntityBase {
    public StathamCar() {
        TaxFrees = new HashSet<TaxFree>();

        Sads = new HashSet<Sad>();
    }

    public double Volume { get; set; }

    public string Number { get; set; }

    public long StathamId { get; set; }

    public Statham Statham { get; set; }

    public ICollection<TaxFree> TaxFrees { get; set; }

    public ICollection<Sad> Sads { get; set; }
}