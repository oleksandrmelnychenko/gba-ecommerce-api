using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Entities.PaymentOrders;

public sealed class IncomePaymentOrderSale : EntityBase {
    public long? SaleId { get; set; }

    public long? ReSaleId { get; set; }

    public long IncomePaymentOrderId { get; set; }

    public decimal Amount { get; set; }

    public decimal OverpaidAmount { get; set; }

    public decimal ExchangeRate { get; set; }

    public Sale Sale { get; set; }

    public ReSale ReSale { get; set; }

    public IncomePaymentOrder IncomePaymentOrder { get; set; }
}