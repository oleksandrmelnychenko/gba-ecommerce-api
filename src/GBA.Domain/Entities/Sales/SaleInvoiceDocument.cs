using System.Collections.Generic;
using GBA.Common.Helpers;

namespace GBA.Domain.Entities.Sales;

public sealed class SaleInvoiceDocument : EntityBase {
    public SaleInvoiceDocument() {
        Sales = new HashSet<Sale>();
    }

    public SalePaymentType PaymentType { get; set; }

    public ClientPaymentType ClientPaymentType { get; set; }

    public string City { get; set; }

    public decimal Vat { get; set; }

    public decimal ShippingAmount { get; set; }

    public decimal ShippingAmountWithoutVat { get; set; }

    public decimal ShippingAmountEur { get; set; }

    public decimal ShippingAmountEurWithoutVat { get; set; }

    public decimal ExchangeRateAmount { get; set; }

    public ICollection<Sale> Sales { get; set; }
}