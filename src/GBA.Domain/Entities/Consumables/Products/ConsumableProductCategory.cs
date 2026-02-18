using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities.Consumables;

public sealed class ConsumableProductCategory : EntityBase {
    public ConsumableProductCategory() {
        ConsumableProductCategoryTranslations = new HashSet<ConsumableProductCategoryTranslation>();

        ConsumableProducts = new HashSet<ConsumableProduct>();

        ConsumablesOrderItems = new HashSet<ConsumablesOrderItem>();
    }

    public string Name { get; set; }

    public string Description { get; set; }

    public bool IsSupplyServiceCategory { get; set; }

    public ICollection<ConsumableProductCategoryTranslation> ConsumableProductCategoryTranslations { get; set; }

    public ICollection<ConsumableProduct> ConsumableProducts { get; set; }

    public ICollection<ConsumablesOrderItem> ConsumablesOrderItems { get; set; }
}