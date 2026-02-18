namespace GBA.Domain.EntityHelpers;

public sealed class GroupedSpecificationByPackingList {
    public string SpecificationName { get; set; }

    public double TotalQty { get; set; }

    public string MeasureUnitNameUk { get; set; }

    public string MeasureUnitNamePl { get; set; }

    public string SpecificationCode { get; set; }
}