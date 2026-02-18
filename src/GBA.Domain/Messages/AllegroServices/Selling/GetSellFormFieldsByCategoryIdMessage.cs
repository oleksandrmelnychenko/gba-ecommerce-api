namespace GBA.Domain.Messages.AllegroServices.Selling;

public sealed class GetSellFormFieldsByCategoryIdMessage {
    public GetSellFormFieldsByCategoryIdMessage(int categoryId) {
        CategoryId = categoryId;
    }

    public int CategoryId { get; set; }
}