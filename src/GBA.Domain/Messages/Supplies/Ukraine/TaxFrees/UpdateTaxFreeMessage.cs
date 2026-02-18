using System;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.TaxFrees;

public sealed class UpdateTaxFreeMessage {
    public UpdateTaxFreeMessage(TaxFree taxFree, Guid userNetId) {
        TaxFree = taxFree;

        UserNetId = userNetId;
    }

    public TaxFree TaxFree { get; }

    public Guid UserNetId { get; }
}