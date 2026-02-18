using GBA.Domain.Entities.Pricings;

namespace GBA.Domain.TranslationEntities;

public class PricingTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public long PricingId { get; set; }

    public virtual Pricing Pricing { get; set; }
}