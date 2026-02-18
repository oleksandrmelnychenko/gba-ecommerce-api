using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.TranslationEntities;

public class ConsumableProductCategoryTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public string Description { get; set; }

    public long ConsumableProductCategoryId { get; set; }

    public virtual ConsumableProductCategory ConsumableProductCategory { get; set; }
}