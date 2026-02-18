using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Products.Transfers;

public sealed class ProductTransferItem : EntityBase {
    public ProductTransferItem() {
        ProductLocations = new HashSet<ProductLocation>();

        ConsignmentItemMovements = new HashSet<ConsignmentItemMovement>();

        ReSaleAvailabilities = new HashSet<ReSaleAvailability>();
    }

    public double Qty { get; set; }

    public string Reason { get; set; }

    public long ProductId { get; set; }

    public long ProductTransferId { get; set; }

    public long? ActReconciliationItemId { get; set; }

    public Product Product { get; set; }

    public ProductTransfer ProductTransfer { get; set; }

    public ActReconciliationItem ActReconciliationItem { get; set; }

    public ICollection<ProductLocation> ProductLocations { get; set; }

    public ICollection<ConsignmentItemMovement> ConsignmentItemMovements { get; set; }

    public ProductAvailability ProductAvailability { get; set; }

    public ICollection<ReSaleAvailability> ReSaleAvailabilities { get; set; }
}