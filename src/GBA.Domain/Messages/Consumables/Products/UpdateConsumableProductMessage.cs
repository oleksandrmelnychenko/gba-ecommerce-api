using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Messages.Consumables.Products;

public sealed class UpdateConsumableProductMessage {
    public UpdateConsumableProductMessage(ConsumableProduct consumableProduct) {
        ConsumableProduct = consumableProduct;
    }

    public ConsumableProduct ConsumableProduct { get; set; }
}