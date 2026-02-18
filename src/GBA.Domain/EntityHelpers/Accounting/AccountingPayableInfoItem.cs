using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.EntityHelpers.Accounting;

public sealed class AccountingPayableInfoItem {
    public long Id { get; set; }

    public SupplyOrganizationAgreement SupplyOrganizationAgreement { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public OutcomePaymentOrder OutcomePaymentOrder { get; set; }

    public AccountingPayableInfoItemType Type { get; set; }

    public decimal Amount { get; set; }

    public decimal EuroAmount { get; set; }
}