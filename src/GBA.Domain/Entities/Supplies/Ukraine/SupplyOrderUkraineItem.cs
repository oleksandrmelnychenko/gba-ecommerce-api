using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class SupplyOrderUkraineItem : EntityBase {
    public SupplyOrderUkraineItem() {
        ProductIncomeItems = new HashSet<ProductIncomeItem>();

        ProductPlacements = new HashSet<ProductPlacement>();

        DynamicProductPlacementRows = new HashSet<DynamicProductPlacementRow>();

        ActReconciliationItems = new HashSet<ActReconciliationItem>();
    }

    public bool IsFullyPlaced { get; set; }

    public bool NotOrdered { get; set; }

    public bool IsUpdated { get; set; }

    public double Qty { get; set; }

    public double QtyDifferent { get; set; }

    public double ToIncomeQty { get; set; }

    public double PlacedQty { get; set; }

    public double RemainingQty { get; set; }

    public double NetWeight { get; set; }

    public double GrossWeight { get; set; }

    public double TotalNetWeight { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal GrossUnitPrice { get; set; }

    public decimal AccountingGrossUnitPrice { get; set; }

    public decimal AccountingGrossUnitPriceLocal { get; set; }

    public decimal UnitPriceLocal { get; set; }

    public decimal GrossUnitPriceLocal { get; set; }

    public decimal ExchangeRateAmount { get; set; }

    public decimal NetPrice { get; set; }

    public decimal GrossPrice { get; set; }

    public decimal AccountingGrossPrice { get; set; }

    public decimal AccountingGrossPriceLocal { get; set; }

    public decimal NetPriceLocal { get; set; }

    public decimal GrossPriceLocal { get; set; }

    public long ProductId { get; set; }

    public long SupplyOrderUkraineId { get; set; }

    public long? SupplierId { get; set; }

    public long? ConsignmentItemId { get; set; }

    public long? PackingListPackageOrderItemId { get; set; }

    public long? ProductSpecificationId { get; set; }

    public decimal VatPercent { get; set; }

    public decimal VatAmount { get; set; }

    public decimal VatAmountLocal { get; set; }

    public double TotalGrossWeight { get; set; }

    public decimal UnitDeliveryAmount { get; set; }

    public decimal UnitDeliveryAmountLocal { get; set; }

    public decimal DeliveryAmount { get; set; }

    public decimal DeliveryAmountLocal { get; set; }

    public bool ProductIsImported { get; set; }

    public ProductSpecification ProductSpecification { get; set; }

    public Product Product { get; set; }

    public SupplyOrderUkraine SupplyOrderUkraine { get; set; }

    public Client Supplier { get; set; }

    public ConsignmentItem ConsignmentItem { get; set; }

    public PackingListPackageOrderItem PackingListPackageOrderItem { get; set; }

    public ICollection<ProductIncomeItem> ProductIncomeItems { get; set; }

    public ICollection<ProductPlacement> ProductPlacements { get; set; }

    public ICollection<DynamicProductPlacementRow> DynamicProductPlacementRows { get; set; }

    public ICollection<ActReconciliationItem> ActReconciliationItems { get; set; }

    public ProductIncomeItem ProductIncomeItem { get; set; }

    public decimal DeliveryExpenseAmount { get; set; }

    public decimal AccountingDeliveryExpenseAmount { get; set; }

    public decimal AccountingCost { get; set; }

    public decimal ManagementCost { get; set; }
}