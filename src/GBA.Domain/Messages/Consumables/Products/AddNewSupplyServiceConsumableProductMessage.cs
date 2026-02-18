using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Messages.Consumables.Products;

public sealed class AddNewSupplyServiceConsumableProductMessage {
    public AddNewSupplyServiceConsumableProductMessage(
        ConsumableProduct consumableProduct) {
        ConsumableProduct = consumableProduct;
    }

    public ConsumableProduct ConsumableProduct { get; }
}