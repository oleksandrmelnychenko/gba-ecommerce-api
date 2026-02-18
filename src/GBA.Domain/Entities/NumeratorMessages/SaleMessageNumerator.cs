using System.Collections.Generic;

namespace GBA.Domain.Entities.NumeratorMessages;

public sealed class SaleMessageNumerator : EntityBase {
    public SaleMessageNumerator() {
        CountSaleMessages = new HashSet<CountSaleMessage>();
    }

    public long CountMessage { get; set; }

    public bool Transfered { get; set; }

    public ICollection<CountSaleMessage> CountSaleMessages { get; set; }
}