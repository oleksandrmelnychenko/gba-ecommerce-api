using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.EntityHelpers;
using Microsoft.AspNetCore.Identity;

namespace GBA.Domain.Repositories.Identities.Contracts;

public interface IIdentityRolesRepository {
    List<IdentityRole> GetAllExistingRoles();

    Task<IdentityResponse> UnassignUserFromRole(string userName, string roleName);

    Task<IdentityResponse> AssignUserToRole(string userName, string roleName);

    Task ChangeUserRole(Guid userProfileNetId, string roleName);

    Task<IdentityResult> AddRole(string name);

    Task<bool> RemoveRoleByName(string name);
}