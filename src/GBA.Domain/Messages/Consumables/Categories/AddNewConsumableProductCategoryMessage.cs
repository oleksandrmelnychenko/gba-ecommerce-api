using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Messages.Consumables.Categories;

public sealed class AddNewConsumableProductCategoryMessage {
    public AddNewConsumableProductCategoryMessage(ConsumableProductCategory consumableProductCategory) {
        ConsumableProductCategory = consumableProductCategory;
    }

    public ConsumableProductCategory ConsumableProductCategory { get; set; }
}