using System.Collections.Generic;
using GBA.Common.Helpers.StockStateStorage;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Entities.Sales;

public sealed class StockStateStorage : EntityBase {
    public StockStateStorage() {
        ProductAvailabilityDataHistory = new HashSet<ProductAvailabilityDataHistory>();
    }

    public ChangeTypeOrderItem ChangeTypeOrderItem { get; set; }
    public double QtyHistory { get; set; }
    public double TotalRowQty { get; set; }
    public double TotalReservedUK { get; set; }
    public double TotalCartReservedUK { get; set; }
    public long? ProductId { get; set; }
    public long? SaleId { get; set; }
    public long? UserId { get; set; }
    public long? SaleNumberId { get; set; }
    public Product Product { get; set; }
    public Sale Sale { get; set; }
    public SaleNumber SaleNumber { get; set; }
    public User User { get; set; }
    public ICollection<ProductAvailabilityDataHistory> ProductAvailabilityDataHistory { get; set; }
}