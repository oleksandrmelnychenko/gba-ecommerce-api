using GBA.Domain.AllegroSellFormEntities;

namespace GBA.Domain.Messages.AllegroServices.Selling;

public sealed class AddNewSellingItemMessage {
    public AddNewSellingItemMessage(NewAllegroSellingItemRequest newSellingRequest) {
        NewSellingRequest = newSellingRequest;
    }

    public NewAllegroSellingItemRequest NewSellingRequest { get; set; }
}