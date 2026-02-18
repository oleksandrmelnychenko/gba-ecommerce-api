using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Messages.Clients;

public sealed class AddClientWorkplaceMessage {
    public AddClientWorkplaceMessage(ClientWorkplace clientWorkplace) {
        ClientWorkplace = clientWorkplace;
    }

    public ClientWorkplace ClientWorkplace { get; }
}