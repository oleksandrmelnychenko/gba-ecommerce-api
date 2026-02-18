namespace GBA.Domain.Entities;

public sealed class UserScreenResolution : EntityBase {
    public int Width { get; set; }

    public int Height { get; set; }

    public long UserId { get; set; }

    public User User { get; set; }
}