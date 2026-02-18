namespace GBA.Domain.Messages.Consumables.Categories;

public sealed class GetAllConsumableProductCategoriesFromSearchMessage {
    public GetAllConsumableProductCategoriesFromSearchMessage(string value) {
        Value = value;
    }

    public string Value { get; set; }
}