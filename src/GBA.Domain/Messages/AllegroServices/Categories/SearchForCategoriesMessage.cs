namespace GBA.Domain.Messages.AllegroServices.Categories;

public sealed class SearchForCategoriesMessage {
    public SearchForCategoriesMessage(string value, int limit, int offset) {
        Value = value;

        Limit = limit;

        Offset = offset;
    }

    public string Value { get; set; }

    public int Limit { get; set; }

    public int Offset { get; set; }
}