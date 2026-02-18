namespace GBA.Domain.Messages.AllegroServices.WebApi;

public sealed class GetSellFormFieldsMessage {
    public GetSellFormFieldsMessage(int categoryId) {
        CategoryId = categoryId;
    }

    public int CategoryId { get; set; }
}