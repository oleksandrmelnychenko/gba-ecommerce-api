namespace GBA.Common.IdentityConfiguration.Entities;

public sealed class UserToken {
    public long Id { get; set; }

    public string Token { get; set; }

    public string UserId { get; set; }
}