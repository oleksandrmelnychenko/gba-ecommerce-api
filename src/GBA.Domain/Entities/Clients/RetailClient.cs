using System.Collections.Generic;
using GBA.Domain.Entities.Ecommerce;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Entities.Clients;

public sealed class RetailClient : EntityBase {
    /// <summary>
    /// ctor().
    /// </summary>
    public RetailClient() {
        Sales = new HashSet<Sale>();

        MisplacedSales = new HashSet<MisplacedSale>();

        RetailClientPaymentImages = new HashSet<RetailClientPaymentImage>();
    }

    public string Name { get; set; }

    public string PhoneNumber { get; set; }

    public long? EcommerceRegionId { get; set; }

    public string ShoppingCartJson { get; set; }

    public EcommerceRegion EcommerceRegion { get; set; }

    public ICollection<Sale> Sales { get; set; }

    public ICollection<MisplacedSale> MisplacedSales { get; set; }

    public ICollection<RetailClientPaymentImage> RetailClientPaymentImages { get; set; }
}