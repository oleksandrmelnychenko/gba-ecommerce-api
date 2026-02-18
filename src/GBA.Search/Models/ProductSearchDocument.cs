using System.Collections.Generic;

namespace GBA.Search.Models;

public sealed class ProductSearchDocument {
    public string Id { get; set; } = string.Empty;
    public string NetUid { get; set; } = string.Empty;
    public string VendorCode { get; set; } = string.Empty;
    public string VendorCodeClean { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameUA { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DescriptionUA { get; set; } = string.Empty;
    public string SearchName { get; set; } = string.Empty;
    public string SearchNameUA { get; set; } = string.Empty;
    public string SearchDescription { get; set; } = string.Empty;
    public string SearchDescriptionUA { get; set; } = string.Empty;
    public string SearchSize { get; set; } = string.Empty;
    public string MainOriginalNumber { get; set; } = string.Empty;
    public string MainOriginalNumberClean { get; set; } = string.Empty;
    public List<string> OriginalNumbers { get; set; } = [];
    public List<string> OriginalNumbersClean { get; set; } = [];
    public string Size { get; set; } = string.Empty;
    public string SizeClean { get; set; } = string.Empty;
    public string Synonyms { get; set; } = string.Empty;
    public string NameStem { get; set; } = string.Empty;
    public string NameUAStem { get; set; } = string.Empty;
    public string DescriptionStem { get; set; } = string.Empty;
    public string DescriptionUAStem { get; set; } = string.Empty;
    public string FullText { get; set; } = string.Empty;
    public string FullTextStem { get; set; } = string.Empty;
    public string SynonymsStem { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = [];

    // Product details
    public string PackingStandard { get; set; } = string.Empty;
    public string OrderStandard { get; set; } = string.Empty;
    public string Ucgfea { get; set; } = string.Empty;
    public string Volume { get; set; } = string.Empty;
    public string Top { get; set; } = string.Empty;
    public double Weight { get; set; }
    public bool HasAnalogue { get; set; }
    public bool HasComponent { get; set; }
    public bool HasImage { get; set; }
    public string Image { get; set; } = string.Empty;
    public long MeasureUnitId { get; set; }

    // Availability
    public bool Available { get; set; }
    public double AvailableQtyUk { get; set; }
    public double AvailableQtyUkVat { get; set; }
    public double AvailableQtyPl { get; set; }
    public double AvailableQtyPlVat { get; set; }
    public double AvailableQty { get; set; }

    // Flags
    public bool IsForWeb { get; set; }
    public bool IsForSale { get; set; }
    public bool IsForZeroSale { get; set; }

    // Slug
    public long SlugId { get; set; }
    public string SlugNetUid { get; set; } = string.Empty;
    public string SlugUrl { get; set; } = string.Empty;
    public string SlugLocale { get; set; } = string.Empty;

    // Retail pricing (for anonymous users)
    public decimal RetailPrice { get; set; }
    public decimal RetailPriceVat { get; set; }
    public string RetailCurrencyCode { get; set; } = "UAH";

    public long UpdatedAt { get; set; }
}
