namespace GBA.Domain.Messages.Clients;

public sealed class GetAllClientsFromSearchBySalesMessage {
    public GetAllClientsFromSearchBySalesMessage(string searchValue) {
        SearchValue = string.IsNullOrEmpty(searchValue) ? string.Empty : searchValue.Trim();
    }

    public string SearchValue { get; }
}