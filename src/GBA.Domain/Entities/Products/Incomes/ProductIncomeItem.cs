using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Products.Incomes;

public sealed class ProductIncomeItem : EntityBase {
    public ProductIncomeItem() {
        ConsignmentItems = new HashSet<ConsignmentItem>();

        ConsignmentItemMovements = new HashSet<ConsignmentItemMovement>();

        ProductPlacements = new HashSet<ProductPlacement>();
    }

    public double Qty { get; set; }

    public double RemainingQty { get; set; }

    public long? SupplyOrderUkraineItemId { get; set; }

    public long? PackingListPackageOrderItemId { get; set; }

    public long? SaleReturnItemId { get; set; }

    public long? ActReconciliationItemId { get; set; }

    public long? ProductCapitalizationItemId { get; set; }

    public long ProductIncomeId { get; set; }

    public SupplyOrderUkraineItem SupplyOrderUkraineItem { get; set; }

    public PackingListPackageOrderItem PackingListPackageOrderItem { get; set; }

    public SaleReturnItem SaleReturnItem { get; set; }

    public ActReconciliationItem ActReconciliationItem { get; set; }

    public ProductCapitalizationItem ProductCapitalizationItem { get; set; }

    public ProductIncome ProductIncome { get; set; }

    public ICollection<ConsignmentItem> ConsignmentItems { get; set; }

    public ICollection<ConsignmentItemMovement> ConsignmentItemMovements { get; set; }

    public ICollection<ProductPlacement> ProductPlacements { get; set; }

    public ProductAvailability ProductAvailability { get; set; }

    public OrderProductSpecification OrderProductSpecification { get; set; }
}