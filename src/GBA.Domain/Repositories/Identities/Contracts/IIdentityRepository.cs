using System;
using System.Security.Claims;
using System.Threading.Tasks;
using GBA.Domain.EntityHelpers;
using GBA.Domain.IdentityEntities;
using Microsoft.AspNetCore.Identity;

namespace GBA.Domain.Repositories.Identities.Contracts;

public interface IIdentityRepository {
    Task<Tuple<ClaimsIdentity, string, UserIdentity>> AuthAndGetClaimsIdentity(string userName, string password);

    Task<Tuple<ClaimsIdentity, string, UserIdentity>> AuthAndGetClaimsIdentity(UserIdentity userName, string password);

    Task<Tuple<ClaimsIdentity, string, UserIdentity>> AuthAndGetClaimsIdentityByNetId(string netId, string password, string login);

    Task<Tuple<ClaimsIdentity, UserIdentity>> AuthAndGetClaimsIdentityByUserId(string userId);

    Task<IdentityResponse> CreateUser(UserIdentity user, string password, bool crmUser = true);

    Task<IdentityResponse> IsUserNameAvailable(string userName, bool crmUser = true);

    Task<IdentityResponse> IsEmailAvailableAsync(string email);

    Task<IdentityResponse> ResetPassword(string netId, string password);

    Task<IdentityResult> AddUserRoleAndClaims(UserIdentity user, string role);

    Task<IdentityResult> UpdateUserName(UserIdentity user);
    Task UpdateUsersEmail(UserIdentity user, string email);

    Task<UserIdentity> GetUserByNetId(string netId);

    Task<UserIdentity> GetUserName(string userName);

    Task UpdateUserRegion(Guid userProfileNetId, string region);

    Task DisableUser(Guid netId);

    Task DeleteUserByNetId(string netId);
}