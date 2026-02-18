using System;

namespace GBA.Common.IdentityConfiguration.Entities;

public sealed class RefreshToken {
    public string UserId { get; set; }

    public DateTime ExpireAt { get; set; }
}