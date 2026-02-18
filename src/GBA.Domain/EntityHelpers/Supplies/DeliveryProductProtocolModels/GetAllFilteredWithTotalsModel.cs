using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;

namespace GBA.Domain.EntityHelpers.Supplies.DeliveryProductProtocolModels;

public sealed class GetAllFilteredWithTotalsModel {
    public IEnumerable<DeliveryProductProtocol> DeliveryProductProtocols { get; set; }

    public double TotalQty { get; set; }
}