using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales.OrderPackages;
using GBA.Domain.Entities.Sales.SaleMerges;

namespace GBA.Domain.Entities.Sales;

public sealed class Order : EntityBase {
    public Order() {
        OrderItems = new HashSet<OrderItem>();

        Sales = new HashSet<Sale>();

        OrderItemMerges = new HashSet<OrderItemMerged>();

        OrderPackages = new HashSet<OrderPackage>();
    }

    public OrderSource OrderSource { get; set; }

    public OrderStatus OrderStatus { get; set; }

    public long? UserId { get; set; }

    public long ClientAgreementId { get; set; }

    public long? ClientShoppingCartId { get; set; }

    public double TotalCount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TotalAmountLocal { get; set; }

    public decimal OverLordTotalAmount { get; set; }

    public decimal OverLordTotalAmountLocal { get; set; }
    public decimal TotalAmountEurToUah { get; set; }

    public bool IsMerged { get; set; }

    public decimal TotalVat { get; set; }

    public User User { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public ClientShoppingCart ClientShoppingCart { get; set; }

    public Sale Sale { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; }

    public ICollection<Sale> Sales { get; set; }

    public ICollection<OrderItemMerged> OrderItemMerges { get; set; }

    public ICollection<OrderPackage> OrderPackages { get; set; }
}