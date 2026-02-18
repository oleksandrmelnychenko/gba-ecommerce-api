using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.DepreciatedOrders;

public sealed class DepreciatedOrderItem : EntityBase {
    public DepreciatedOrderItem() {
        ProductLocations = new HashSet<ProductLocation>();

        ProductLocationsHistory = new HashSet<ProductLocationHistory>();

        ConsignmentItemMovements = new HashSet<ConsignmentItemMovement>();

        ReSaleAvailabilities = new HashSet<ReSaleAvailability>();
    }

    public decimal PerUnitPrice { get; set; }

    public double Qty { get; set; }

    public string Reason { get; set; }

    public long ProductId { get; set; }

    public long DepreciatedOrderId { get; set; }

    public long? ActReconciliationItemId { get; set; }

    public Product Product { get; set; }

    public DepreciatedOrder DepreciatedOrder { get; set; }

    public ActReconciliationItem ActReconciliationItem { get; set; }

    public ICollection<ProductLocation> ProductLocations { get; set; }
    public ICollection<ProductLocationHistory> ProductLocationsHistory { get; set; }

    public ICollection<ConsignmentItemMovement> ConsignmentItemMovements { get; set; }

    public ICollection<ReSaleAvailability> ReSaleAvailabilities { get; set; }
}