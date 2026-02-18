using System;
using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.Supplies;

public sealed class TaxFreesPrintingRequest {
    public TaxFreesPrintingRequest() {
        NetIds = new List<Guid>();
    }

    public IEnumerable<Guid> NetIds { get; set; }
}