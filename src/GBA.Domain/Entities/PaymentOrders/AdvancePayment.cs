using System;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Clients.OrganizationClients;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.PaymentOrders;

public sealed class AdvancePayment : EntityBase {
    public DateTime FromDate { get; set; }

    public decimal Amount { get; set; }

    public decimal VatAmount { get; set; }

    public double VatPercent { get; set; }

    public string Number { get; set; }

    public string Comment { get; set; }

    public long UserId { get; set; }

    public long OrganizationId { get; set; }

    public long? TaxFreeId { get; set; }

    public long? SadId { get; set; }

    public long? ClientAgreementId { get; set; }

    public long? OrganizationClientAgreementId { get; set; }

    public User User { get; set; }

    public Organization Organization { get; set; }

    public TaxFree TaxFree { get; set; }

    public Sad Sad { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public OrganizationClientAgreement OrganizationClientAgreement { get; set; }
}