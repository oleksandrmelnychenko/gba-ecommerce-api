using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.EntityHelpers.GbaDataExportModels;

public class BaseProductExportModel {
    public Guid NetUid { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public bool Deleted { get; set; }
    public string VendorCode { get; set; }
    public string Name { get; set; }
    public string NameUA { get; set; }
    public string Description { get; set; }
    public bool IsForSale { get; set; }
    public bool IsForZeroSale { get; set; }
    public double Weight { get; set; }
    public string Top { get; set; }
    public string Volume { get; set; }
    public string Size { get; set; }
    public string PackingStandard { get; set; }
    public string DescriptionUA { get; set; }
    public string Standard { get; set; }
    public string MainOriginalNumber { get; set; }

    public long? SourceAmgCode { get; set; }

    public long? SourceFenixCode { get; set; }

    public Guid ParentGroupNetUid { get; set; }
    public MeasureUnitDto MeasureUnit { get; set; }
    public ProductSpecificationDto ProductSpecification { get; set; }
}

public sealed class ProductDto : BaseProductExportModel {
    public ProductGroupDto ProductGroup { get; set; }
    public List<ProductPricing> ProductPricings { get; set; } = new();
}