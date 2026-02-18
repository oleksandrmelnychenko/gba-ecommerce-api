namespace GBA.Domain.Entities.Clients;

public sealed class ClientSubClient : EntityBase {
    public long RootClientId { get; set; }

    public long SubClientId { get; set; }

    public Client RootClient { get; set; }

    public Client SubClient { get; set; }
}