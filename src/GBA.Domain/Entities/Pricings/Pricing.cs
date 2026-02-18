using System.Collections.Generic;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Ecommerce;
using GBA.Domain.Entities.Products;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities.Pricings;

public sealed class Pricing : EntityBase {
    public Pricing() {
        Pricings = new HashSet<Pricing>();

        ProviderPricings = new HashSet<ProviderPricing>();

        PricingTranslations = new HashSet<PricingTranslation>();

        Agreements = new HashSet<Agreement>();

        ProductPricings = new HashSet<ProductPricing>();

        PricingProductGroupDiscounts = new HashSet<PricingProductGroupDiscount>();

        SubPricingProductGroupDiscounts = new HashSet<PricingProductGroupDiscount>();

        PromotionalAgreements = new HashSet<Agreement>();

        DefaultPricings = new HashSet<EcommerceDefaultPricing>();

        PromotionalPricings = new HashSet<EcommerceDefaultPricing>();
    }

    public string Name { get; set; }

    public string Comment { get; set; }

    public double? ExtraCharge { get; set; }

    public decimal CalculatedExtraCharge { get; set; }

    public long? BasePricingId { get; set; }

    public long? CurrencyId { get; set; }

    public long? PriceTypeId { get; set; }

    public string Culture { get; set; }

    public bool ForShares { get; set; }

    public bool ForVat { get; set; }

    public int SortingPriority { get; set; }

    public PriceType PriceType { get; set; }

    public Currency Currency { get; set; }

    public Pricing BasePricing { get; set; }

    public ICollection<Pricing> Pricings { get; set; }

    public ICollection<ProviderPricing> ProviderPricings { get; set; }

    public ICollection<PricingTranslation> PricingTranslations { get; set; }

    public ICollection<Agreement> Agreements { get; set; }

    public ICollection<Agreement> PromotionalAgreements { get; set; }

    public ICollection<ProductPricing> ProductPricings { get; set; }

    public ICollection<PricingProductGroupDiscount> PricingProductGroupDiscounts { get; set; }

    public ICollection<PricingProductGroupDiscount> SubPricingProductGroupDiscounts { get; set; }

    public ICollection<EcommerceDefaultPricing> DefaultPricings { get; set; }

    public ICollection<EcommerceDefaultPricing> PromotionalPricings { get; set; }
}