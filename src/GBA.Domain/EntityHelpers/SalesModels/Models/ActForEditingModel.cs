using System.Collections.Generic;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.EntityHelpers.SalesModels.Models;

public sealed class ActForEditingModel {
    public ActForEditingModel() {
        historyInvoiceEdits = new HashSet<HistoryInvoiceEdit>();
    }

    public ICollection<HistoryInvoiceEdit> historyInvoiceEdits { get; set; }
}