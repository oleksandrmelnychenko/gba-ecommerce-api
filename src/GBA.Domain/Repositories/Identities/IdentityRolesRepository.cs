using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GBA.Domain.EntityHelpers;
using GBA.Domain.IdentityEntities;
using GBA.Domain.Repositories.Identities.Contracts;
using Microsoft.AspNetCore.Identity;

namespace GBA.Domain.Repositories.Identities;

public sealed class IdentityRolesRepository : IIdentityRolesRepository {
    private readonly RoleManager<IdentityRole> _roleManager;

    private readonly UserManager<UserIdentity> _userManager;

    public IdentityRolesRepository(
        RoleManager<IdentityRole> roleManager,
        UserManager<UserIdentity> userManager) {
        _roleManager = roleManager;

        _userManager = userManager;
    }

    public List<IdentityRole> GetAllExistingRoles() {
        return _roleManager.Roles.ToList();
    }

    public async Task<IdentityResult> AddRole(string name) {
        return await _roleManager.CreateAsync(new IdentityRole {
            Name = name,
            NormalizedName = name.ToUpper()
        });
    }

    public async Task<bool> RemoveRoleByName(string name) {
        IdentityRole role = _roleManager.Roles.FirstOrDefault(r => r.Name.Equals(name));

        if (role is null) return true;

        IdentityResult identityResult = await _roleManager.DeleteAsync(role);

        return identityResult.Succeeded;
    }

    public async Task ChangeUserRole(Guid userProfileNetId, string roleName) {
        Claim claim = new("NetId", userProfileNetId.ToString());

        IList<UserIdentity> users = await _userManager.GetUsersForClaimAsync(claim);

        if (users.Any())
            foreach (UserIdentity user in users) {
                IList<string> userRoles = await _userManager.GetRolesAsync(user);

                if (userRoles.Any()) {
                    string userRole = userRoles.FirstOrDefault();

                    if (!userRole.ToLower().Equals(roleName.ToLower())) {
                        await _userManager.RemoveFromRoleAsync(user, userRole);

                        await _userManager.AddToRoleAsync(user, roleName);
                    }
                } else {
                    await _userManager.AddToRoleAsync(user, roleName);
                }
            }
    }

    public async Task<IdentityResponse> UnassignUserFromRole(string userName, string roleName) {
        if (await _roleManager.RoleExistsAsync(roleName)) {
            UserIdentity user = await _userManager.FindByNameAsync(userName);

            if (user != null) {
                IdentityResult result = await _userManager.RemoveFromRoleAsync(user, roleName);

                if (result.Succeeded)
                    return new IdentityResponse {
                        Succeeded = true
                    };

                IdentityError error = result.Errors.FirstOrDefault();

                return new IdentityResponse {
                    Succeeded = false,
                    Errors = new List<ErrorItem> {
                        new() {
                            Code = error.Code,
                            Description = error.Description
                        }
                    }
                };
            }

            return new IdentityResponse {
                Succeeded = false,
                Errors = new List<ErrorItem> {
                    new() {
                        Code = "UserNotExists",
                        Description = $"User with UserName \"{userName}\" does not exists"
                    }
                }
            };
        }

        return new IdentityResponse {
            Succeeded = false,
            Errors = new List<ErrorItem> {
                new() {
                    Code = "RoleNotExists",
                    Description = $"Role with name \"{roleName}\" does not exists."
                }
            }
        };
    }

    public async Task<IdentityResponse> AssignUserToRole(string userName, string roleName) {
        if (await _roleManager.RoleExistsAsync(roleName)) {
            UserIdentity user = await _userManager.FindByNameAsync(userName);

            if (user != null) {
                IdentityResult result = await _userManager.AddToRoleAsync(user, roleName);

                if (result.Succeeded)
                    return new IdentityResponse {
                        Succeeded = true
                    };

                IdentityError error = result.Errors.FirstOrDefault();

                return new IdentityResponse {
                    Succeeded = false,
                    Errors = new List<ErrorItem> {
                        new() {
                            Code = error.Code,
                            Description = error.Description
                        }
                    }
                };
            }

            return new IdentityResponse {
                Succeeded = false,
                Errors = new List<ErrorItem> {
                    new() {
                        Code = "UserNotExists",
                        Description = $"User with UserName \"{userName}\" does not exists"
                    }
                }
            };
        }

        return new IdentityResponse {
            Succeeded = false,
            Errors = new List<ErrorItem> {
                new() {
                    Code = "RoleNotExists",
                    Description = $"Role with name \"{roleName}\" does not exists."
                }
            }
        };
    }
}