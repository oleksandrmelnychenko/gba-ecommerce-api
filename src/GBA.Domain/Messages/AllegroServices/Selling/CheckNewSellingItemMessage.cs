using GBA.Domain.AllegroSellFormEntities;

namespace GBA.Domain.Messages.AllegroServices.Selling;

public sealed class CheckNewSellingItemMessage {
    public CheckNewSellingItemMessage(NewAllegroSellingItemRequest newSellingRequest) {
        NewSellingRequest = newSellingRequest;
    }

    public NewAllegroSellingItemRequest NewSellingRequest { get; set; }
}