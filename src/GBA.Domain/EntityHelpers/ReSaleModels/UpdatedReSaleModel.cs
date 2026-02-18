using System.Collections.Generic;
using System.Linq;
using GBA.Domain.Entities.ReSales;

namespace GBA.Domain.EntityHelpers.ReSaleModels;

public sealed class UpdatedReSaleModel {
    public ReSale ReSale { get; set; }

    public List<UpdatedReSaleItemModel> ReSaleItemModels { get; set; } = new();

    public double TotalQty {
        get {
            return ReSaleItemModels.Sum(x => x.QtyToReSale);
        }
    }

    public decimal TotalAmount {
        get {
            return ReSaleItemModels.Sum(x => x.Amount);
        }
    }

    public decimal TotalVat {
        get {
            return ReSaleItemModels.Sum(x => x.Vat);
        }
    }

    public double TotalWeight {
        get {
            return ReSaleItemModels.Sum(x => x.ConsignmentItem.Weight * x.QtyToReSale);
        }
    }
}