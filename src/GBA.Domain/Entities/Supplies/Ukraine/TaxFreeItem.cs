using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;

namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class TaxFreeItem : EntityBase {
    public TaxFreeItem() {
        ConsignmentItemMovements = new HashSet<ConsignmentItemMovement>();
    }

    public double Qty { get; set; }

    public string Comment { get; set; }

    public double TotalNetWeight { get; set; }

    public decimal UnitPriceWithVat { get; set; }

    public decimal TotalWithVat { get; set; }

    public decimal VatAmountPl { get; set; }

    public decimal TotalWithVatPl { get; set; }

    public decimal UnitPricePL { get; set; }

    public decimal TotalWithoutVatPl { get; set; }

    public long TaxFreeId { get; set; }

    public long? SupplyOrderUkraineCartItemId { get; set; }

    public long? TaxFreePackListOrderItemId { get; set; }

    public TaxFree TaxFree { get; set; }

    public SupplyOrderUkraineCartItem SupplyOrderUkraineCartItem { get; set; }

    public TaxFreePackListOrderItem TaxFreePackListOrderItem { get; set; }

    public ICollection<ConsignmentItemMovement> ConsignmentItemMovements { get; set; }
}