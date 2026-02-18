using GBA.Domain.Entities.Clients.PerfectClients;

namespace GBA.Domain.Messages.Clients;

public sealed class UpdatePerfectClientMessage {
    public UpdatePerfectClientMessage(PerfectClient perfectClient) {
        PerfectClient = perfectClient;
    }

    public PerfectClient PerfectClient { get; set; }
}