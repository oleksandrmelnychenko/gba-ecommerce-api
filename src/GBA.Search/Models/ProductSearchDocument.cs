using System;
using System.Collections.Generic;
using GBA.Domain.EntityHelpers;
using GBA.Services.Services.Products;

namespace GBA.Search.Models;

public sealed record ProductSearchCatalogContext(
    long OrganizationId,
    string Source,
    bool WithVat,
    Guid ClientAgreementNetId,
    long PricingId,
    long CurrencyId,
    bool UseIndexedRetailPrice,
    PricingDependencyRevisions? PricingRevisions = null) {
    public bool IsValid {
        get {
            if (OrganizationId <= 0
                || ClientAgreementNetId == Guid.Empty
                || PricingId <= 0
                || CurrencyId <= 0
                || string.IsNullOrWhiteSpace(Source))
                return false;

            if (UseIndexedRetailPrice && PricingRevisions?.IsValid != true)
                return false;

            return ProductSourceIdentitySql.TryNormalizeSourceWorld(Source, out string normalized)
                   && string.Equals(Source, normalized, StringComparison.Ordinal);
        }
    }
}

public sealed class ProductSearchDocument {
    public long Id { get; set; }
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
    public string RetailCurrencyCodeVat { get; set; } = "UAH";
    public string IndexedProductPricingRevision { get; set; } = string.Empty;
    public string IndexedPricingHierarchyRevision { get; set; } = string.Empty;
    public string IndexedDiscountRevision { get; set; } = string.Empty;
    public string IndexedExchangeRateRevision { get; set; } = string.Empty;

    public PricingDependencyRevisions IndexedPricingRevisions => new(
        IndexedProductPricingRevision,
        IndexedPricingHierarchyRevision,
        IndexedDiscountRevision,
        IndexedExchangeRateRevision);

    // The shared index represents one validated Fenix retail organization. Separate
    // variant metadata prevents source/VAT fields from being combined across arrays.
    public long CatalogOrganizationIdNonVat { get; set; }
    public long CatalogOrganizationIdVat { get; set; }
    public string CatalogAgreementSourceNonVat { get; set; } = string.Empty;
    public string CatalogAgreementSourceVat { get; set; } = string.Empty;
    public string ProductSourceFenix { get; set; } = string.Empty;
    public string ProductSourceAmg { get; set; } = string.Empty;
    public bool IsCanonicalFenix { get; set; }
    public bool IsCanonicalAmg { get; set; }
    public List<ProductSearchCatalogScope> CatalogScopes { get; set; } = [];
    public string CatalogSourceSystemNonVat { get; set; } = string.Empty;
    public string CatalogSourceSystemVat { get; set; } = string.Empty;
    public string CatalogAgreementNetUidNonVat { get; set; } = string.Empty;
    public string CatalogAgreementNetUidVat { get; set; } = string.Empty;
    public long CatalogPricingIdNonVat { get; set; }
    public long CatalogPricingIdVat { get; set; }
    public long CatalogCurrencyIdNonVat { get; set; }
    public long CatalogCurrencyIdVat { get; set; }
    public bool HasNonVatCatalogAvailability { get; set; }
    public bool HasVatCatalogAvailability { get; set; }
    public bool HasNonVatCatalogSource { get; set; }
    public bool HasVatCatalogSource { get; set; }

    public long UpdatedAt { get; set; }
}

public sealed class ProductSearchCatalogScope {
    public long OrganizationId { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public bool WithVat { get; set; }
    public double AvailableQtyUk { get; set; }
    public double AvailableQtyPl { get; set; }
    public double AvailableQty { get; set; }
}
