using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers;

public sealed class ProductForUpload {
    public ProductForUpload() {
        Pricings = new List<ProductForUploadPricing>();
    }

    public string VendorCode { get; set; }

    public string NewVendorCode { get; set; }

    public string Name { get; set; }

    public string NameUA { get; set; }

    public string NamePL { get; set; }

    public string Description { get; set; }

    public string DescriptionUA { get; set; }

    public string DescriptionPL { get; set; }

    public string Size { get; set; }

    public string PackingStandard { get; set; }

    public string OrderStandard { get; set; }

    public string UCGFEA { get; set; }

    public string Volume { get; set; }

    public string Top { get; set; }

    public string MeasureUnit { get; set; }

    public string ProductGroup { get; set; }

    public string MainOriginalNumber { get; set; }

    public double Weight { get; set; }

    public long MeasureUnitId { get; set; }

    public long ProductGroupId { get; set; }

    public long Id { get; set; }

    public bool IsForWeb { get; set; }

    public bool IsForSale { get; set; }

    public bool Skipped { get; set; }

    public List<ProductForUploadPricing> Pricings { get; set; }
}