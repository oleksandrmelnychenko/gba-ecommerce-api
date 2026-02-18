using System;

namespace GBA.Domain.EntityHelpers.Consignments;

public sealed class MovementConsignmentInfo {
    public string IncomeDocumentNumber { get; set; }

    public DateTime? IncomeDocumentFromDate { get; set; }

    public string DocumentType { get; set; }

    public string DocumentNumber { get; set; }

    public DateTime DocumentFromDate { get; set; }

    public string ClientName { get; set; }

    public string StorageName { get; set; }

    public string OrganizationName { get; set; }

    public string Responsible { get; set; }

    public decimal Price { get; set; }

    public decimal AccountingPrice { get; set; }

    public decimal Discount { get; set; }

    public double IncomeQty { get; set; }

    public double OutcomeQty { get; set; }

    public string Comment { get; set; }

    public bool IsEdited { get; set; }
}