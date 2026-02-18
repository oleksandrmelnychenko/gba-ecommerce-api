using System;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.EntityHelpers.Consignments;

public sealed class AvailableConsignment {
    public long ConsignmentItemId { get; set; }

    public double RemainingQty { get; set; }

    public string ProductIncomeNumber { get; set; }

    public DateTime FromDate { get; set; }

    public Client Supplier { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public Organization Organization { get; set; }
}