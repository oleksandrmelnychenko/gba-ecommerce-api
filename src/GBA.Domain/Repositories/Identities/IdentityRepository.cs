using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.EntityHelpers;
using GBA.Domain.IdentityEntities;
using GBA.Domain.Repositories.Identities.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace GBA.Domain.Repositories.Identities;

public sealed class IdentityRepository : IIdentityRepository {
    private readonly IStringLocalizer<SharedResource> _localizer;

    private readonly UserManager<UserIdentity> _userManager;

    public IdentityRepository(
        IStringLocalizer<SharedResource> localizer,
        UserManager<UserIdentity> userManager) {
        _localizer = localizer;

        _userManager = userManager;
    }

    public async Task<Tuple<ClaimsIdentity, string, UserIdentity>> AuthAndGetClaimsIdentityByNetId(string netId, string password, string login) {
        Claim claim = new("NetId", netId);

        IList<UserIdentity> users = await _userManager.GetUsersForClaimAsync(claim);

        if (!users.Any()) return await AuthAndGetClaimsIdentity(login, password);

        UserIdentity user = users.First();

        if (!IsUserEnabled(user)) return new Tuple<ClaimsIdentity, string, UserIdentity>(null, _localizer[SharedResourceNames.USER_DISABLED], null);

        if (!await _userManager.CheckPasswordAsync(user, password)) throw new Exception("The password you entered is incorrect");

        IList<string> roles = await _userManager.GetRolesAsync(user);
        IList<Claim> claims = await _userManager.GetClaimsAsync(user);

        claims.Add(new Claim("role", roles.FirstOrDefault()));
        claims.Add(new Claim("region", user.Region));
        claims.Add(new Claim("type", user.UserType.ToString()));
        claims.Add(new Claim("UserName", user.UserName));

        return new Tuple<ClaimsIdentity, string, UserIdentity>(new ClaimsIdentity(new GenericIdentity(user.UserName, "Token"), claims), string.Empty, user);
    }

    public async Task<Tuple<ClaimsIdentity, string, UserIdentity>> AuthAndGetClaimsIdentity(UserIdentity user, string password) {
        if (user == null) return new Tuple<ClaimsIdentity, string, UserIdentity>(null, _localizer[SharedResourceNames.INVALID_CREDENTIALS], null);

        if (!IsUserEnabled(user)) return new Tuple<ClaimsIdentity, string, UserIdentity>(null, _localizer[SharedResourceNames.USER_DISABLED], null);

        if (!await _userManager.CheckPasswordAsync(user, password))
            return new Tuple<ClaimsIdentity, string, UserIdentity>(null, _localizer[SharedResourceNames.INVALID_PASSWORD], null);

        IList<string> roles = await _userManager.GetRolesAsync(user);
        IList<Claim> claims = await _userManager.GetClaimsAsync(user);

        claims.Add(new Claim("role", roles.FirstOrDefault()));
        claims.Add(new Claim("region", user.Region));
        claims.Add(new Claim("type", user.UserType.ToString()));
        claims.Add(new Claim("UserName", user.UserName));

        return new Tuple<ClaimsIdentity, string, UserIdentity>(new ClaimsIdentity(new GenericIdentity(user.UserName, "Token"), claims), string.Empty, user);
    }

    public async Task<Tuple<ClaimsIdentity, string, UserIdentity>> AuthAndGetClaimsIdentity(string userName, string password) {
        UserIdentity user = await _userManager.FindByNameAsync(userName) ?? await _userManager.FindByEmailAsync(userName);

        if (user == null) return new Tuple<ClaimsIdentity, string, UserIdentity>(null, _localizer[SharedResourceNames.INVALID_CREDENTIALS], null);

        if (!IsUserEnabled(user)) return new Tuple<ClaimsIdentity, string, UserIdentity>(null, _localizer[SharedResourceNames.USER_DISABLED], null);

        if (!await _userManager.CheckPasswordAsync(user, password))
            return new Tuple<ClaimsIdentity, string, UserIdentity>(null, _localizer[SharedResourceNames.INVALID_PASSWORD], null);

        IList<string> roles = await _userManager.GetRolesAsync(user);
        IList<Claim> claims = await _userManager.GetClaimsAsync(user);

        claims.Add(new Claim("role", roles.FirstOrDefault()));
        claims.Add(new Claim("region", user.Region));
        claims.Add(new Claim("type", user.UserType.ToString()));
        claims.Add(new Claim("UserName", user.UserName));

        return new Tuple<ClaimsIdentity, string, UserIdentity>(new ClaimsIdentity(new GenericIdentity(user.UserName, "Token"), claims), string.Empty, user);
    }

    public async Task<Tuple<ClaimsIdentity, UserIdentity>> AuthAndGetClaimsIdentityByUserId(string userId) {
        UserIdentity user = await _userManager.FindByIdAsync(userId);

        if (user == null) return null;

        IList<string> roles = await _userManager.GetRolesAsync(user);
        IList<Claim> claims = await _userManager.GetClaimsAsync(user);

        claims.Add(new Claim("role", roles.FirstOrDefault()));
        claims.Add(new Claim("region", user.Region));
        claims.Add(new Claim("type", user.UserType.ToString()));
        claims.Add(new Claim("UserName", user.UserName));

        return new Tuple<ClaimsIdentity, UserIdentity>(new ClaimsIdentity(new GenericIdentity(user.UserName, "Token"), claims), user);
    }

    public async Task<IdentityResult> AddUserRoleAndClaims(UserIdentity user, string role) {
        List<Claim> claims = new() {
            new Claim("NetId", user.NetId.ToString())
        };

        IdentityResult result = await _userManager.AddClaimsAsync(user, claims);

        if (!string.IsNullOrEmpty(role)) return await _userManager.AddToRoleAsync(user, role);

        return result;
    }

    public async Task<IdentityResponse> CreateUser(UserIdentity user, string password, bool crmUser = true) {
        IdentityResponse checkUserNameResponse = await IsUserNameAvailable(user.UserName, crmUser);

        if (!checkUserNameResponse.Succeeded) return checkUserNameResponse;

        if (!string.IsNullOrEmpty(user.Email)) {
            UserIdentity userExists = await _userManager.FindByEmailAsync(user.Email);

            if (userExists != null) {
                checkUserNameResponse.Succeeded = false;
                checkUserNameResponse.Errors.Add(new ErrorItem { Code = "EmailExist", Description = _localizer[SharedResourceNames.INVALID_EMAIL] });

                return checkUserNameResponse;
            }
        }

        IdentityResult result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded) return new IdentityResponse { Succeeded = true };

        IdentityResponse identityResponse = new() { Succeeded = false };

        foreach (IdentityError error in result.Errors)
            if (error.Code.ToLower().Contains("password")) {
                if (!identityResponse.Errors.Any())
                    identityResponse.Errors.Add(new ErrorItem { Code = error.Code, Description = _localizer[SharedResourceNames.INVALID_PASSWORD] });
            } else {
                identityResponse.Errors.Add(new ErrorItem { Code = error.Code, Description = _localizer[SharedResourceNames.INVALID_EMAIL] });
            }

        return identityResponse;
    }

    public async Task<IdentityResponse> ResetPassword(string netId, string password) {
        Claim claim = new("NetId", netId);

        IList<UserIdentity> users = await _userManager.GetUsersForClaimAsync(claim);

        if (users.Any()) {
            UserIdentity user = users.First();

            string resetPasswordToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            IdentityResult result = await _userManager.ResetPasswordAsync(user, resetPasswordToken, password);

            if (result.Succeeded)
                //IdentityResult updateResult = await _userManager.UpdateAsync(user);
                //if (updateResult.Succeeded)
                return new IdentityResponse { Succeeded = true };

            IdentityResponse identityResponse = new() { Succeeded = false };

            foreach (IdentityError error in result.Errors)
                if (error.Code.ToLower().Contains("password")) {
                    if (!identityResponse.Errors.Any())
                        identityResponse.Errors.Add(new ErrorItem { Code = error.Code, Description = _localizer[SharedResourceNames.INVALID_PASSWORD] });
                } else {
                    identityResponse.Errors.Add(new ErrorItem { Code = error.Code, Description = error.Description });
                }

            return identityResponse;
        } else {
            IdentityResponse identityResponse = new() { Succeeded = false };

            identityResponse.Errors.Add(new ErrorItem { Code = "UserWithNetIdNotExist", Description = "User with provided netId does not exist" });

            return identityResponse;
        }
    }

    public async Task UpdateUserRegion(Guid userProfileNetId, string region) {
        Claim claim = new("NetId", userProfileNetId.ToString());

        IList<UserIdentity> users = await _userManager.GetUsersForClaimAsync(claim);

        if (users.Any())
            foreach (UserIdentity user in users) {
                user.Region = region;

                await _userManager.UpdateAsync(user);
            }
    }

    public async Task<IdentityResponse> IsUserNameAvailable(string userName, bool crmUser = true) {
        IdentityResponse response = new() { Succeeded = false };

        if (string.IsNullOrEmpty(userName)) {
            response.Succeeded = true;

            return response;
        }

        UserIdentity user = await _userManager.FindByNameAsync(userName);

        if (user == null) {
            response.Succeeded = true;

            return response;
        }

        response.Errors.Add(new ErrorItem
            { Code = "UserNameExist", Description = crmUser ? _localizer[SharedResourceNames.INVALID_USERNAME] : _localizer[SharedResourceNames.INVALID_EMAIL] });

        return response;
    }

    public async Task<IdentityResponse> IsEmailAvailableAsync(string email) {
        IdentityResponse response = new() { Succeeded = false };

        if (string.IsNullOrEmpty(email)) {
            response.Succeeded = true;

            return response;
        }

        UserIdentity user = await _userManager.FindByEmailAsync(email);

        if (user == null) {
            response.Succeeded = true;

            return response;
        }

        response.Errors.Add(new ErrorItem { Code = "EmailExist", Description = _localizer[SharedResourceNames.INVALID_EMAIL] });

        return response;
    }

    public async Task<UserIdentity> GetUserByNetId(string netId) {
        Claim claim = new("NetId", netId);

        IList<UserIdentity> users = await _userManager.GetUsersForClaimAsync(claim);

        return users.Any() ? users.First() : null;
    }

    public async Task<IdentityResult> UpdateUserName(UserIdentity user) {
        return await _userManager.SetUserNameAsync(user, user.UserName);
    }

    public async Task UpdateUsersEmail(UserIdentity user, string email) {
        IdentityResult updateResult = await _userManager.SetEmailAsync(user, email);

        if (updateResult.Succeeded) return;

        throw new Exception(updateResult.Errors.FirstOrDefault()?.Description ?? string.Empty);
    }

    public async Task DisableUser(Guid netId) {
        Claim claim = new("NetId", netId.ToString());

        IList<UserIdentity> users = await _userManager.GetUsersForClaimAsync(claim);

        if (users.Any())
            foreach (UserIdentity user in users)
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
    }

    public async Task DeleteUserByNetId(string netId) {
        Claim claim = new("NetId", netId);

        IList<UserIdentity> users = await _userManager.GetUsersForClaimAsync(claim);

        if (users.Any())
            foreach (UserIdentity user in users)
                await _userManager.DeleteAsync(user);
    }

    public async Task<UserIdentity> GetUserName(string userName) {
        return await _userManager.FindByNameAsync(userName) ?? await _userManager.FindByEmailAsync(userName);
    }

    private bool IsUserEnabled(UserIdentity user) {
        if (user.LockoutEnd == null) return true;

        int compareResult = DateTimeOffset.Compare((DateTimeOffset)user.LockoutEnd, DateTimeOffset.Now);

        return compareResult <= 0;
    }
}