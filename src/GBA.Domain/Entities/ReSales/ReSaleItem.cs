using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Entities.ReSales;

public sealed class ReSaleItem : EntityBase {
    public ReSaleItem() {
        ConsignmentItemMovements = new HashSet<ConsignmentItemMovement>();
    }

    public double Qty { get; set; }

    public long ReSaleId { get; set; }

    public decimal PricePerItem { get; set; }

    public decimal ExtraCharge { get; set; }

    public decimal ExchangeRate { get; set; }

    public long? ReSaleAvailabilityId { get; set; }

    public long ProductId { get; set; }

    public ReSale ReSale { get; set; }

    public ReSaleAvailability ReSaleAvailability { get; set; }

    public decimal TotalPrice { get; set; }

    public ICollection<ConsignmentItemMovement> ConsignmentItemMovements { get; set; }

    public Product Product { get; set; }

    public double RemainingQty { get; set; }

    public double Weight { get; set; }
}