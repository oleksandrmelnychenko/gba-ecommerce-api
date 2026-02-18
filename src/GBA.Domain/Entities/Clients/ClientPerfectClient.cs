using GBA.Domain.Entities.Clients.PerfectClients;

namespace GBA.Domain.Entities.Clients;

public sealed class ClientPerfectClient : EntityBase {
    public long PerfectClientId { get; set; }

    public long ClientId { get; set; }

    public long? PerfectClientValueId { get; set; }

    public string Value { get; set; }

    public bool IsChecked { get; set; }

    public Client Client { get; set; }

    public PerfectClient PerfectClient { get; set; }

    public PerfectClientValue PerfectClientValue { get; set; }
}