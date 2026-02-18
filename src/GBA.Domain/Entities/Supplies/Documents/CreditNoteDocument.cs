using System;

namespace GBA.Domain.Entities.Supplies.Documents;

public sealed class CreditNoteDocument : BaseDocument {
    public long SupplyOrderId { get; set; }

    public decimal Amount { get; set; }

    public string Number { get; set; }

    public string Comment { get; set; }

    public DateTime FromDate { get; set; }

    public SupplyOrder SupplyOrder { get; set; }
}