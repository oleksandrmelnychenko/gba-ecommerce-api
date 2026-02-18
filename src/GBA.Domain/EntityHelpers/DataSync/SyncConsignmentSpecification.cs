using System;

namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncConsignmentSpecification {
    public long ProductCode { get; set; }

    public DateTime? CustomsDeclarationDate { get; set; }

    public string NumberDeclarationDate { get; set; }

    public string ProductVendorCode { get; set; }

    public decimal Duty { get; set; }

    public decimal Vat { get; set; }

    public string SpecificationName { get; set; }

    public string SpecificationCode { get; set; }
}