using System;
using System.Collections.Generic;

namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class ActReconciliation : EntityBase {
    public ActReconciliation() {
        ActReconciliationItems = new HashSet<ActReconciliationItem>();
    }

    public string Number { get; set; }

    public string Comment { get; set; }

    public DateTime FromDate { get; set; }

    public long ResponsibleId { get; set; }

    public long? SupplyOrderUkraineId { get; set; }

    public long? SupplyInvoiceId { get; set; }

    public User Responsible { get; set; }

    public SupplyOrderUkraine SupplyOrderUkraine { get; set; }

    public SupplyInvoice SupplyInvoice { get; set; }

    public ICollection<ActReconciliationItem> ActReconciliationItems { get; set; }
}