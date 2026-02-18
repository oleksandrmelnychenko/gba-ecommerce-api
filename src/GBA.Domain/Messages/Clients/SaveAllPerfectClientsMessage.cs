using System.Collections.Generic;
using GBA.Domain.Entities.Clients.PerfectClients;

namespace GBA.Domain.Messages.Clients;

public sealed class SaveAllPerfectClientsMessage {
    public SaveAllPerfectClientsMessage(List<PerfectClient> perfectClients) {
        PerfectClients = perfectClients;
    }

    public List<PerfectClient> PerfectClients { get; set; }
}