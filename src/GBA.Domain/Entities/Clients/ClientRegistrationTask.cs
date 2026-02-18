namespace GBA.Domain.Entities.Clients;

public sealed class ClientRegistrationTask : EntityBase {
    public bool IsDone { get; set; }

    public long ClientId { get; set; }

    public Client Client { get; set; }
}