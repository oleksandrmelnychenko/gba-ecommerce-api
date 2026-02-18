using System;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class GenericDocument {
    public long TempId { get; set; }

    public DateTime SynchronizationDate { get; set; }

    public string Number { get; set; }

    public string Type { get; set; }

    public decimal Amount { get; set; }

    public Currency Currency { get; set; }

    // Can be Client (Client), Client (Supplier), SupplyOrganization
    public Client Client { get; set; }

    public SupplyOrganization SupplyOrganization { get; set; }

    public SupplyOrganizationAgreement SupplyOrganizationAgreement { get; set; }

    // Same with this one
    public ClientAgreement ClientAgreement { get; set; }

    public Organization Organization { get; set; }

    public ContractorType ContractorType { get; set; }

    public int TotalQty { get; set; }
}