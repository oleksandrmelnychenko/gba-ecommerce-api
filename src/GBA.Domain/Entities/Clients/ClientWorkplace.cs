namespace GBA.Domain.Entities.Clients;

public sealed class ClientWorkplace : EntityBase {
    public long MainClientId { get; set; }

    public long WorkplaceId { get; set; }

    public Client MainClient { get; set; }

    public Client WorkplaceClient { get; set; }

    public ClientGroup ClientGroup { get; set; }
}