namespace GBA.Domain.Entities.Clients;

public sealed class ClientInRole : EntityBase {
    public long ClientId { get; set; }

    public long ClientTypeId { get; set; }

    public long ClientTypeRoleId { get; set; }

    public Client Client { get; set; }

    public ClientType ClientType { get; set; }

    public ClientTypeRole ClientTypeRole { get; set; }
}