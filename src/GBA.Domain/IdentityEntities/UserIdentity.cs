using System;
using GBA.Domain.EntityHelpers;
using Microsoft.AspNetCore.Identity;

namespace GBA.Domain.IdentityEntities;

public class UserIdentity : IdentityUser {
    public Guid NetId { get; set; }

    public IdentityUserType UserType { get; set; }

    public string Region { get; set; }
}