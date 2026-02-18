using System.Collections.Generic;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Clients.OrganizationClients;

public sealed class OrganizationClient : EntityBase {
    public OrganizationClient() {
        OrganizationClientAgreements = new HashSet<OrganizationClientAgreement>();

        Sads = new HashSet<Sad>();

        IncomePaymentOrders = new HashSet<IncomePaymentOrder>();

        OutcomePaymentOrders = new HashSet<OutcomePaymentOrder>();
    }

    public string FullName { get; set; }

    public string Address { get; set; }

    public string Country { get; set; }

    public string City { get; set; }

    public string NIP { get; set; }

    public decimal MarginAmount { get; set; }

    public ICollection<OrganizationClientAgreement> OrganizationClientAgreements { get; set; }

    public ICollection<Sad> Sads { get; set; }

    public ICollection<IncomePaymentOrder> IncomePaymentOrders { get; set; }

    public ICollection<OutcomePaymentOrder> OutcomePaymentOrders { get; set; }
}