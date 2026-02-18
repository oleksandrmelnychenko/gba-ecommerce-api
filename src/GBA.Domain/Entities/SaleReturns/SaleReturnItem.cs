using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Entities.SaleReturns;

public sealed class SaleReturnItem : EntityBase {
    public SaleReturnItem() {
        SaleReturnItemProductPlacements = new HashSet<SaleReturnItemProductPlacement>();
    }

    public string StatusName { get; set; }

    public double Qty { get; set; }

    public SaleReturnItemStatus SaleReturnItemStatus { get; set; }

    public bool IsMoneyReturned { get; set; }

    public long StorageId { get; set; }

    public long OrderItemId { get; set; }

    public long SaleReturnId { get; set; }

    public long CreatedById { get; set; }

    public long? UpdatedById { get; set; }

    public long? MoneyReturnedById { get; set; }

    public decimal ExchangeRateAmount { get; set; }

    public decimal Amount { get; set; }

    public decimal AmountLocal { get; set; }

    public decimal VatAmount { get; set; }

    public decimal VatAmountLocal { get; set; }

    public DateTime? MoneyReturnedAt { get; set; }

    public Storage Storage { get; set; }

    public OrderItem OrderItem { get; set; }

    public SaleReturn SaleReturn { get; set; }

    public User CreatedBy { get; set; }

    public User UpdatedBy { get; set; }

    public User MoneyReturnedBy { get; set; }
    public ProductIncomeItem ProductIncomeItem { get; set; }
    public ICollection<SaleReturnItemProductPlacement> SaleReturnItemProductPlacements { get; set; }
}