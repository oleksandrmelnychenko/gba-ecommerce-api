using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Products;

public sealed class ProductPlacement : EntityBase {
    public ProductPlacement() {
        ProductLocations = new HashSet<ProductLocation>();

        ProductLocationsHistory = new HashSet<ProductLocationHistory>();

        FromProductPlacementMovements = new HashSet<ProductPlacementMovement>();

        ToProductPlacementMovements = new HashSet<ProductPlacementMovement>();

        SupplyOrderUkraineCartItemReservationProductPlacements = new HashSet<SupplyOrderUkraineCartItemReservationProductPlacement>();
    }

    public double Qty { get; set; }

    public string StorageNumber { get; set; }

    public string RowNumber { get; set; }

    public string CellNumber { get; set; }

    public long ProductId { get; set; }

    public long StorageId { get; set; }

    public long? PackingListPackageOrderItemId { get; set; }

    public long? SupplyOrderUkraineItemId { get; set; }

    public long? ProductIncomeItemId { get; set; }

    public long? ConsignmentItemId { get; set; }

    public bool IsOriginal { get; set; }
    public bool IsHistorySet { get; set; }

    public Product Product { get; set; }

    public Storage Storage { get; set; }

    public PackingListPackageOrderItem PackingListPackageOrderItem { get; set; }

    public SupplyOrderUkraineItem SupplyOrderUkraineItem { get; set; }

    public ProductIncomeItem ProductIncomeItem { get; set; }

    public ConsignmentItem ConsignmentItem { get; set; }

    public ICollection<ProductLocation> ProductLocations { get; set; }
    public ICollection<ProductLocationHistory> ProductLocationsHistory { get; set; }

    public ICollection<ProductPlacementMovement> FromProductPlacementMovements { get; set; }

    public ICollection<ProductPlacementMovement> ToProductPlacementMovements { get; set; }

    public ICollection<SupplyOrderUkraineCartItemReservationProductPlacement> SupplyOrderUkraineCartItemReservationProductPlacements { get; set; }
}