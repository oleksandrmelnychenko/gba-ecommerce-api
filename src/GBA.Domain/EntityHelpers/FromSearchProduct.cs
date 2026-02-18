using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.EntityHelpers;

public sealed class FromSearchProduct {
    public FromSearchProduct() {
        ProductPricings = new List<ProductPricing>();
    }

    public long Id { get; set; }

    /// <summary>
    /// Search result row number for ordering. Used internally, not serialized to API response.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public long SearchRowNumber { get; set; }

    public Guid NetUid { get; set; }

    public string VendorCode { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string Size { get; set; }

    public string PackingStandard { get; set; }

    public string OrderStandard { get; set; }

    public string UCGFEA { get; set; }

    public string Volume { get; set; }

    public string Top { get; set; }

    public double AvailableQtyUk { get; set; }

    public double AvailableQtyRoad { get; set; }

    public double AvailableQtyUkVAT { get; set; }

    public double AvailableQtyPl { get; set; }

    public double AvailableQtyPlVAT { get; set; }

    public double Weight { get; set; }

    public bool HasAnalogue { get; set; }

    public bool HasComponent { get; set; }

    public bool HasImage { get; set; }

    public bool IsForWeb { get; set; }

    public bool IsForSale { get; set; }

    public bool IsForZeroSale { get; set; }

    public string MainOriginalNumber { get; set; }

    public List<string> OriginalNumbers { get; set; } = new();

    public string Image { get; set; }

    public long MeasureUnitId { get; set; }

    public decimal CurrentPrice { get; set; }

    public decimal CurrentLocalPrice { get; set; }

    public decimal CurrentWithVatPrice { get; set; }

    public decimal CurrentLocalWithVatPrice { get; set; }

    public string CurrencyCode { get; set; }

    public ProductSlug ProductSlug { get; set; }

    public List<ProductPricing> ProductPricings { get; set; }
}