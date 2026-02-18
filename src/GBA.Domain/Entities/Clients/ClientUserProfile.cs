namespace GBA.Domain.Entities.Clients;

public sealed class ClientUserProfile : EntityBase {
    public long ClientId { get; set; }

    public long UserProfileId { get; set; }

    public Client Client { get; set; }

    public User UserProfile { get; set; }
}