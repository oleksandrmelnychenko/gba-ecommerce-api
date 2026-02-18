using System;

namespace GBA.Domain.EntityHelpers.Consignments;

public sealed class OutcomeConsignmentInfo {
    public DateTime FromDate { get; set; }

    public string DocumentTypeName { get; set; }

    public string StorageName { get; set; }

    public string OrganizationName { get; set; }

    public string DocumentNumber { get; set; }

    public string ClientName { get; set; }

    public string ResponsibleName { get; set; }

    public decimal Price { get; set; }

    public double Qty { get; set; }

    public bool HasUpdateDataCarrier { get; set; }
}