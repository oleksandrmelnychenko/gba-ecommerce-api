using System;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Messages.PaymentOrders.IncomePaymentOrders;

public sealed class AddNewIncomePaymentOrderFromTaxFreeMessage {
    public AddNewIncomePaymentOrderFromTaxFreeMessage(IncomePaymentOrder incomePaymentOrder, Guid taxFreeNetId, Guid userNetId) {
        IncomePaymentOrder = incomePaymentOrder;

        TaxFreeNetId = taxFreeNetId;

        UserNetId = userNetId;
    }

    public IncomePaymentOrder IncomePaymentOrder { get; }
    public Guid TaxFreeNetId { get; }
    public Guid UserNetId { get; }
}