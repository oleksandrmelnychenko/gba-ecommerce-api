using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Clients.OrganizationClients;

public sealed class OrganizationClientAgreement : EntityBase {
    public OrganizationClientAgreement() {
        Sads = new HashSet<Sad>();

        IncomePaymentOrders = new HashSet<IncomePaymentOrder>();

        OutcomePaymentOrders = new HashSet<OutcomePaymentOrder>();

        AdvancePayments = new HashSet<AdvancePayment>();
    }

    public string Number { get; set; }

    public DateTime FromDate { get; set; }

    public long CurrencyId { get; set; }

    public long OrganizationClientId { get; set; }

    public long? TaxAccountingSchemeId { get; set; }

    public long? AgreementTypeCivilCodeId { get; set; }

    public Currency Currency { get; set; }

    public OrganizationClient OrganizationClient { get; set; }

    public TaxAccountingScheme TaxAccountingScheme { get; set; }

    public AgreementTypeCivilCode AgreementTypeCivilCode { get; set; }

    public ICollection<Sad> Sads { get; set; }

    public ICollection<IncomePaymentOrder> IncomePaymentOrders { get; set; }

    public ICollection<OutcomePaymentOrder> OutcomePaymentOrders { get; set; }

    public ICollection<AdvancePayment> AdvancePayments { get; set; }
}