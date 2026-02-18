using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities.Pricings;

public sealed class PriceType : EntityBase {
    public PriceType() {
        Pricings = new HashSet<Pricing>();

        PriceTypeTranslations = new HashSet<PriceTypeTranslation>();
    }

    public string Name { get; set; }

    public ICollection<Pricing> Pricings { get; set; }

    public ICollection<PriceTypeTranslation> PriceTypeTranslations { get; set; }
}