using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Carriers;

public sealed class StathamPassport : EntityBase {
    public StathamPassport() {
        TaxFrees = new HashSet<TaxFree>();

        Sads = new HashSet<Sad>();
    }

    public string PassportSeria { get; set; }

    public string PassportNumber { get; set; }

    public string PassportIssuedBy { get; set; }

    public string City { get; set; }

    public string Street { get; set; }

    public string HouseNumber { get; set; }

    public DateTime PassportIssuedDate { get; set; }

    public DateTime PassportCloseDate { get; set; }

    public long StathamId { get; set; }

    public Statham Statham { get; set; }

    public ICollection<TaxFree> TaxFrees { get; set; }

    public ICollection<Sad> Sads { get; set; }
}