using System;

namespace GBA.Common.IdentityConfiguration.Entities;

public sealed class CompleteAccessToken {
    public string AccessToken { get; set; }

    public string RefreshToken { get; set; }

    public Guid UserNetUid { get; set; }
}