using System.Collections.Generic;

namespace GBA.Domain.Entities.Sales;

public sealed class SaleInvoiceNumber : EntityBase {
    public SaleInvoiceNumber() {
        Sales = new HashSet<Sale>();
    }

    public string Number { get; set; }

    public ICollection<Sale> Sales { get; set; }
}