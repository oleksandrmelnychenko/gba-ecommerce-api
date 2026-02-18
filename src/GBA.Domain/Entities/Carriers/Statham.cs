using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Carriers;

public sealed class Statham : EntityBase {
    public Statham() {
        StathamCars = new HashSet<StathamCar>();

        StathamPassports = new HashSet<StathamPassport>();

        TaxFrees = new HashSet<TaxFree>();

        Sads = new HashSet<Sad>();
    }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string MiddleName { get; set; }

    public ICollection<StathamCar> StathamCars { get; set; }

    public ICollection<StathamPassport> StathamPassports { get; set; }

    public ICollection<TaxFree> TaxFrees { get; set; }

    public ICollection<Sad> Sads { get; set; }
}