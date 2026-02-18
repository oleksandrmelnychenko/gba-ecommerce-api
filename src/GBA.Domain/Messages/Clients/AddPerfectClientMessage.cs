using GBA.Domain.Entities.Clients.PerfectClients;

namespace GBA.Domain.Messages.Clients;

public sealed class AddPerfectClientMessage {
    public AddPerfectClientMessage(PerfectClient perfectClient) {
        PerfectClient = perfectClient;
    }

    public PerfectClient PerfectClient { get; set; }
}