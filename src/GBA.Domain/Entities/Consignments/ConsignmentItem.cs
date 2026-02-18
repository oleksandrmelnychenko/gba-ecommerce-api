using System.Collections.Generic;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Supplies.Returns;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Consignments;

public sealed class ConsignmentItem : EntityBase {
    public ConsignmentItem() {
        ConsignmentItemMovements = new HashSet<ConsignmentItemMovement>();

        ChildConsignmentItems = new HashSet<ConsignmentItem>();

        SupplyReturnItems = new HashSet<SupplyReturnItem>();

        SupplyOrderUkraineCartItemReservations = new HashSet<SupplyOrderUkraineCartItemReservation>();

        SadItems = new HashSet<SadItem>();

        TaxFreePackListOrderItems = new HashSet<TaxFreePackListOrderItem>();

        SupplyOrderUkraineItems = new HashSet<SupplyOrderUkraineItem>();

        ProductReservations = new HashSet<ProductReservation>();

        ProductPlacements = new HashSet<ProductPlacement>();

        ReSaleAvailabilities = new HashSet<ReSaleAvailability>();
    }

    public bool IsReSaleAvailability { get; set; }

    public double Qty { get; set; }

    public double RemainingQty { get; set; }

    public double Weight { get; set; }

    public decimal Price { get; set; }

    public decimal NetPrice { get; set; }

    public decimal AccountingPrice { get; set; }

    public decimal DutyPercent { get; set; }

    public long ProductId { get; set; }

    public long ConsignmentId { get; set; }

    public long? RootConsignmentItemId { get; set; }

    public long ProductIncomeItemId { get; set; }

    public long ProductSpecificationId { get; set; }

    public decimal ExchangeRate { get; set; }

    public Product Product { get; set; }

    public Consignment Consignment { get; set; }

    public ConsignmentItem RootConsignmentItem { get; set; }

    public ProductIncomeItem ProductIncomeItem { get; set; }

    public ProductSpecification ProductSpecification { get; set; }

    public ICollection<ConsignmentItemMovement> ConsignmentItemMovements { get; set; }

    public ICollection<ConsignmentItem> ChildConsignmentItems { get; set; }

    public ICollection<SupplyReturnItem> SupplyReturnItems { get; set; }

    public ICollection<SupplyOrderUkraineCartItemReservation> SupplyOrderUkraineCartItemReservations { get; set; }

    public ICollection<SadItem> SadItems { get; set; }

    public ICollection<TaxFreePackListOrderItem> TaxFreePackListOrderItems { get; set; }

    public ICollection<SupplyOrderUkraineItem> SupplyOrderUkraineItems { get; set; }

    public ICollection<ProductReservation> ProductReservations { get; set; }

    public ICollection<ProductPlacement> ProductPlacements { get; set; }

    public ICollection<ReSaleAvailability> ReSaleAvailabilities { get; set; }

    public ProductAvailability ProductAvailability { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal TotalPriceWithExtraCharge { get; set; }

    public double QtyToReSale { get; set; }

    public Storage FromStorage { get; set; }
}