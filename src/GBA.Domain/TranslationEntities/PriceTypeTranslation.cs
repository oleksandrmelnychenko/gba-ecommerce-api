using GBA.Domain.Entities.Pricings;

namespace GBA.Domain.TranslationEntities;

public class PriceTypeTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public long PriceTypeId { get; set; }

    public virtual PriceType PriceType { get; set; }
}