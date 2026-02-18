using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.ReSales;

namespace GBA.Domain.Entities.Supplies.Returns;

public sealed class SupplyReturnItem : EntityBase {
    public SupplyReturnItem() {
        ConsignmentItemMovements = new HashSet<ConsignmentItemMovement>();

        ReSaleAvailabilities = new HashSet<ReSaleAvailability>();
    }

    public double Qty { get; set; }

    public double TotalNetWeight { get; set; }

    public decimal TotalNetPrice { get; set; }

    public long ProductId { get; set; }

    public long SupplyReturnId { get; set; }

    public long ConsignmentItemId { get; set; }

    public Product Product { get; set; }

    public SupplyReturn SupplyReturn { get; set; }

    public ConsignmentItem ConsignmentItem { get; set; }

    public ICollection<ConsignmentItemMovement> ConsignmentItemMovements { get; set; }

    public ICollection<ReSaleAvailability> ReSaleAvailabilities { get; set; }
}