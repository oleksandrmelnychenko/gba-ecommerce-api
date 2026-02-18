using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.EntityHelpers;

public sealed class ProductStorageInfo {
    public ProductStorageInfo() {
        ProductPlacements = new List<ProductPlacement>();
    }

    public Product Product { get; set; }

    public Client Supplier { get; set; }

    public PackingListPackageOrderItem PackingListPackageOrderItem { get; set; }

    public SupplyOrderUkraineItem SupplyOrderUkraineItem { get; set; }

    public DateTime FromDate { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal UnitPriceLocal { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TotalAmountLocal { get; set; }

    public double Qty { get; set; }

    public double RemainingQty { get; set; }

    public double GrossWeight { get; set; }

    public double TotalGrossWeight { get; set; }

    public List<ProductPlacement> ProductPlacements { get; set; }
}