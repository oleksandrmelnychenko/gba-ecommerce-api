namespace GBA.Domain.Messages.Consumables.Categories;

public sealed class GetConsumableProductCategoriesSupplyServicesMessage {
    public GetConsumableProductCategoriesSupplyServicesMessage(
        string value) {
        Value = value ?? string.Empty;
    }

    public string Value { get; }
}