using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class TaxFreePackListOrderItem : EntityBase {
    public TaxFreePackListOrderItem() {
        TaxFreeItems = new HashSet<TaxFreeItem>();
    }

    public double NetWeight { get; set; }

    public double TotalNetWeight { get; set; }

    public double Qty { get; set; }

    public double UnpackedQty { get; set; }

    public double PackageSize { get; set; }

    public double Coef { get; set; }

    public int MaxQtyPerTF { get; set; }

    public decimal UnitPriceLocal { get; set; }

    public long OrderItemId { get; set; }

    public long TaxFreePackListId { get; set; }

    public long? ConsignmentItemId { get; set; }

    public OrderItem OrderItem { get; set; }

    public TaxFreePackList TaxFreePackList { get; set; }

    public ConsignmentItem ConsignmentItem { get; set; }

    public ICollection<TaxFreeItem> TaxFreeItems { get; set; }
}