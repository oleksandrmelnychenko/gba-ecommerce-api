using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Messages.Consumables.Categories;

public sealed class UpdateConsumableProductCategoryMessage {
    public UpdateConsumableProductCategoryMessage(ConsumableProductCategory consumableProductCategory) {
        ConsumableProductCategory = consumableProductCategory;
    }

    public ConsumableProductCategory ConsumableProductCategory { get; set; }
}