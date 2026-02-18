using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Supplies.Returns;

namespace GBA.Domain.Entities;

public sealed class Storage : EntityBase {
    public Storage() {
        ProductAvailabilities = new HashSet<ProductAvailability>();

        SaleReturnItems = new HashSet<SaleReturnItem>();

        ProductIncomes = new HashSet<ProductIncome>();

        DepreciatedOrders = new HashSet<DepreciatedOrder>();

        FromProductTransfers = new HashSet<ProductTransfer>();

        ToProductTransfers = new HashSet<ProductTransfer>();

        SupplyReturns = new HashSet<SupplyReturn>();

        ProductPlacements = new HashSet<ProductPlacement>();

        ProductLocations = new HashSet<ProductLocation>();

        ProductLocationsHistory = new HashSet<ProductLocationHistory>();

        Organizations = new HashSet<Organization>();

        ProductCapitalizations = new HashSet<ProductCapitalization>();

        Consignments = new HashSet<Consignment>();

        ReSales = new HashSet<ReSale>();
    }

    public string Name { get; set; }

    public string Locale { get; set; }

    public bool ForDefective { get; set; }

    public bool IsResale { get; set; }

    public bool ForVatProducts { get; set; }

    public bool AvailableForReSale { get; set; }

    public bool ForEcommerce { get; set; }

    public long? OrganizationId { get; set; }

    public int RetailPriority { get; set; }

    public Organization Organization { get; set; }

    public ICollection<ProductAvailability> ProductAvailabilities { get; set; }

    public ICollection<SaleReturnItem> SaleReturnItems { get; set; }

    public ICollection<ProductIncome> ProductIncomes { get; set; }

    public ICollection<DepreciatedOrder> DepreciatedOrders { get; set; }

    public ICollection<ProductTransfer> FromProductTransfers { get; set; }

    public ICollection<ProductTransfer> ToProductTransfers { get; set; }

    public ICollection<SupplyReturn> SupplyReturns { get; set; }

    public ICollection<ProductPlacement> ProductPlacements { get; set; }

    public ICollection<ProductLocation> ProductLocations { get; set; }

    public ICollection<ProductLocationHistory> ProductLocationsHistory { get; set; }

    public ICollection<Organization> Organizations { get; set; }

    public ICollection<ProductCapitalization> ProductCapitalizations { get; set; }

    public ICollection<Consignment> Consignments { get; set; }

    public ICollection<ReSale> ReSales { get; set; }
}