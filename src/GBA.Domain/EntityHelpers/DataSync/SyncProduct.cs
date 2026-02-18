namespace GBA.Domain.EntityHelpers.DataSync;

public sealed class SyncProduct {
    public byte[] SourceId { get; set; }

    public byte[] ParentId { get; set; }

    public long Code { get; set; }

    public string VendorCode { get; set; }

    public string Name { get; set; }

    public string NameUa { get; set; }

    public string NamePl { get; set; }

    public string Description { get; set; }

    public string DescriptionUa { get; set; }

    public string DescriptionPl { get; set; }

    public string PackingStandard { get; set; }

    public string Standard { get; set; }

    public string OrderStandard { get; set; }

    public bool IsForSale { get; set; }

    public bool IsZeroForSale { get; set; }

    public string Size { get; set; }

    public string Top { get; set; }

    public double Weight { get; set; }

    public string Volume { get; set; }

    public string UCGFEA { get; set; }

    public string OriginalNumberCode { get; set; }

    public string OriginalNumberName { get; set; }

    public string MeasureUnitCode { get; set; }

    public string MeasureUnitName { get; set; }
}