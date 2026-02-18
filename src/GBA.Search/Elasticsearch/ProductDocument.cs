using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GBA.Search.Elasticsearch;

public sealed class ProductDocument {
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("netUid")]
    public string NetUid { get; set; } = "";

    [JsonPropertyName("vendorCode")]
    public string VendorCode { get; set; } = "";

    [JsonPropertyName("vendorCodeClean")]
    public string VendorCodeClean { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("nameUA")]
    public string NameUA { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("descriptionUA")]
    public string DescriptionUA { get; set; } = "";

    [JsonPropertyName("searchName")]
    public string SearchName { get; set; } = "";

    [JsonPropertyName("searchNameUA")]
    public string SearchNameUA { get; set; } = "";

    [JsonPropertyName("searchDescription")]
    public string SearchDescription { get; set; } = "";

    [JsonPropertyName("searchDescriptionUA")]
    public string SearchDescriptionUA { get; set; } = "";

    [JsonPropertyName("mainOriginalNumber")]
    public string MainOriginalNumber { get; set; } = "";

    [JsonPropertyName("mainOriginalNumberClean")]
    public string MainOriginalNumberClean { get; set; } = "";

    [JsonPropertyName("originalNumbers")]
    public List<string> OriginalNumbers { get; set; } = [];

    [JsonPropertyName("originalNumbersClean")]
    public List<string> OriginalNumbersClean { get; set; } = [];

    [JsonPropertyName("size")]
    public string Size { get; set; } = "";

    [JsonPropertyName("sizeClean")]
    public string SizeClean { get; set; } = "";

    [JsonPropertyName("packingStandard")]
    public string PackingStandard { get; set; } = "";

    [JsonPropertyName("orderStandard")]
    public string OrderStandard { get; set; } = "";

    [JsonPropertyName("ucgfea")]
    public string Ucgfea { get; set; } = "";

    [JsonPropertyName("volume")]
    public string Volume { get; set; } = "";

    [JsonPropertyName("top")]
    public string Top { get; set; } = "";

    [JsonPropertyName("weight")]
    public double Weight { get; set; }

    [JsonPropertyName("hasAnalogue")]
    public bool HasAnalogue { get; set; }

    [JsonPropertyName("hasComponent")]
    public bool HasComponent { get; set; }

    [JsonPropertyName("hasImage")]
    public bool HasImage { get; set; }

    [JsonPropertyName("image")]
    public string Image { get; set; } = "";

    [JsonPropertyName("measureUnitId")]
    public long MeasureUnitId { get; set; }

    [JsonPropertyName("available")]
    public bool Available { get; set; }

    [JsonPropertyName("availableQtyUk")]
    public double AvailableQtyUk { get; set; }

    [JsonPropertyName("availableQtyUkVat")]
    public double AvailableQtyUkVat { get; set; }

    [JsonPropertyName("availableQtyPl")]
    public double AvailableQtyPl { get; set; }

    [JsonPropertyName("availableQtyPlVat")]
    public double AvailableQtyPlVat { get; set; }

    [JsonPropertyName("availableQty")]
    public double AvailableQty { get; set; }

    [JsonPropertyName("isForWeb")]
    public bool IsForWeb { get; set; }

    [JsonPropertyName("isForSale")]
    public bool IsForSale { get; set; }

    [JsonPropertyName("isForZeroSale")]
    public bool IsForZeroSale { get; set; }

    [JsonPropertyName("slugId")]
    public long SlugId { get; set; }

    [JsonPropertyName("slugNetUid")]
    public string SlugNetUid { get; set; } = "";

    [JsonPropertyName("slugUrl")]
    public string SlugUrl { get; set; } = "";

    [JsonPropertyName("slugLocale")]
    public string SlugLocale { get; set; } = "";

    [JsonPropertyName("retailPrice")]
    public decimal RetailPrice { get; set; }

    [JsonPropertyName("retailPriceVat")]
    public decimal RetailPriceVat { get; set; }

    [JsonPropertyName("retailCurrencyCode")]
    public string RetailCurrencyCode { get; set; } = "UAH";

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
