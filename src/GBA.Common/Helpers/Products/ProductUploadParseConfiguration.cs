using System.Collections.Generic;

namespace GBA.Common.Helpers;

public sealed class ProductUploadParseConfiguration {
    public ProductUploadParseConfiguration() {
        PriceConfigurations = new List<ProductUploadPriceConfiguration>();
    }

    public ProductUploadMode Mode { get; set; }

    public int StartRow { get; set; }

    public int EndRow { get; set; }

    public int VendorCode { get; set; }

    public int NewVendorCode { get; set; }

    public int NameRU { get; set; }

    public int NameUA { get; set; }

    public int NamePL { get; set; }

    public int DescriptionRU { get; set; }

    public int DescriptionUA { get; set; }

    public int DescriptionPL { get; set; }

    public int ProductGroup { get; set; }

    public int MeasureUnit { get; set; }

    public int Weight { get; set; }

    public int MainOriginalNumber { get; set; }

    public int Top { get; set; }

    public int OrderStandard { get; set; }

    public int PackingStandard { get; set; }

    public int Size { get; set; }

    public int Volume { get; set; }

    public int UCGFEA { get; set; }

    public int IsForWeb { get; set; }

    public int IsForSale { get; set; }

    public bool WithNewVendorCode { get; set; }

    public bool WithNameRU { get; set; }

    public bool WithNameUA { get; set; }

    public bool WithNamePL { get; set; }

    public bool WithDescriptionRU { get; set; }

    public bool WithDescriptionUA { get; set; }

    public bool WithDescriptionPL { get; set; }

    public bool WithProductGroup { get; set; }

    public bool WithMeasureUnit { get; set; }

    public bool WithWeight { get; set; }

    public bool WithMainOriginalNumber { get; set; }

    public bool WithTop { get; set; }

    public bool WithOrderStandard { get; set; }

    public bool WithPackingStandard { get; set; }

    public bool WithSize { get; set; }

    public bool WithVolume { get; set; }

    public bool WithUCGFEA { get; set; }

    public bool WithIsForWeb { get; set; }

    public bool WithIsForSale { get; set; }

    public bool WithPrices { get; set; }

    public List<ProductUploadPriceConfiguration> PriceConfigurations { get; set; }
}