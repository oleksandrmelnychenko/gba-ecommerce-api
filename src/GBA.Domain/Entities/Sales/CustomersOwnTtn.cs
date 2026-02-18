using System.Collections.Generic;

namespace GBA.Domain.Entities.Sales;

public sealed class CustomersOwnTtn : EntityBase {
    public CustomersOwnTtn() {
        Sales = new HashSet<Sale>();
    }

    public string Number { get; set; }

    public string TtnPDFPath { get; set; }

    public ICollection<Sale> Sales { get; set; }

    public bool IsEmpty() {
        return string.IsNullOrEmpty(Number) && string.IsNullOrEmpty(TtnPDFPath);
    }
}