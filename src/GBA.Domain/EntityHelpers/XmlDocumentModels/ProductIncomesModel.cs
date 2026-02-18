using System.Collections.Generic;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.EntityHelpers.XmlDocumentModels;

public sealed class ProductIncomesModel {
    public ProductIncomesModel() {
        SupplyOrders = new List<SupplyOrder>();

        SupplyOrderUkraines = new List<SupplyOrderUkraine>();
    }

    public List<SupplyOrder> SupplyOrders { get; }

    public List<SupplyOrderUkraine> SupplyOrderUkraines { get; }
}