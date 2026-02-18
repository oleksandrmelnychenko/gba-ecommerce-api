using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Entities.Sales;

public sealed class MisplacedSale : EntityBase {
    public MisplacedSale() {
        OrderItems = new HashSet<OrderItem>();
    }

    public MisplacedSaleStatus MisplacedSaleStatus { get; set; }

    public long? SaleId { get; set; }

    public long? UserId { get; set; }

    public long RetailClientId { get; set; }

    public RetailClient RetailClient { get; set; }

    public User User { get; set; }

    public Sale Sale { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; }

    public bool WithSales { get; set; }
}