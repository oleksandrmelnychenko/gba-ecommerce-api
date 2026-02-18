using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.TranslationEntities;

public class ConsumableProductTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public long ConsumableProductId { get; set; }

    public virtual ConsumableProduct ConsumableProduct { get; set; }
}