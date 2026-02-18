using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GBA.Common.Helpers;
using GBA.Common.IdentityConfiguration;
using GBA.Common.IdentityConfiguration.Entities;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.EntityHelpers;
using GBA.Domain.IdentityEntities;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Identities.Contracts;
using GBA.Services.Services.UserManagement.Contracts;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace GBA.Services.Services.UserManagement;

public sealed class RequestTokenService : IRequestTokenService {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IIdentityRepositoriesFactory _identityRepositoriesFactory;

    private readonly IStringLocalizer<SharedResource> _localizer;

    public RequestTokenService(
        IIdentityRepositoriesFactory identityRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IDbConnectionFactory connectionFactory,
        IStringLocalizer<SharedResource> localizer) {
        _identityRepositoriesFactory = identityRepositoriesFactory;

        _clientRepositoriesFactory = clientRepositoriesFactory;

        _connectionFactory = connectionFactory;
        _localizer = localizer;
    }

    public async Task<Tuple<bool, string, CompleteAccessToken>> RefreshToken(string refreshToken) {
        try {
            using IDbConnection connection = _connectionFactory.NewIdentitySqlConnection();
            string decryptedToken = AesManager.Decrypt(refreshToken);

            RefreshToken deserializedRefreshToken = JsonConvert.DeserializeObject<RefreshToken>(decryptedToken);

            if (deserializedRefreshToken.ExpireAt < DateTime.Now) throw new Exception("Refresh token expired");

            Tuple<ClaimsIdentity, UserIdentity> result = await _identityRepositoriesFactory.NewIdentityRepository()
                .AuthAndGetClaimsIdentityByUserId(deserializedRefreshToken.UserId);

            if (result == null) throw new Exception("Refresh token invalid");

            return new Tuple<bool, string, CompleteAccessToken>(
                true,
                string.Empty,
                GenerateAccessAndRefreshToken(_identityRepositoriesFactory.NewUserTokenRepository(connection), result.Item1.Claims, result.Item2)
            );
        } catch (Exception) {
            return new Tuple<bool, string, CompleteAccessToken>(false, "Refresh token invalid", null);
        }
    }

    public async Task<Tuple<bool, string, CompleteAccessToken>> RequestToken(string userName, string password) {
        try {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password)) return new Tuple<bool, string, CompleteAccessToken>(false, "Invalid credentials", null);

            Regex regionCodeRegex = new(@"^(\D{2,3}\d{5})$");

            Tuple<ClaimsIdentity, string, UserIdentity> identityResult;

            using (IDbConnection connection = _connectionFactory.NewSqlConnection()) {
                if (regionCodeRegex.IsMatch(userName)) {
                    Guid clientNetId = _clientRepositoriesFactory.NewClientRepository(connection).GetClientNetIdByRegionCode(userName);

                    if (clientNetId.Equals(Guid.Empty))
                        return new Tuple<bool, string, CompleteAccessToken>(false, "Invalid credentials", null);

                    identityResult =
                        await _identityRepositoriesFactory.NewIdentityRepository().AuthAndGetClaimsIdentityByNetId(clientNetId.ToString(), password, userName);
                } else {
                    Client clientEmail = _clientRepositoriesFactory.NewClientRepository(connection).GetEmail(userName);
                    if (clientEmail == null)
                        clientEmail = _clientRepositoriesFactory.NewClientRepository(connection).GetClientNetIdByMobileNumber(userName);

                    if (clientEmail != null)
                        identityResult =
                            await _identityRepositoriesFactory.NewIdentityRepository().AuthAndGetClaimsIdentityByNetId(clientEmail.NetUid.ToString(), password, userName);
                    else
                        identityResult = await _identityRepositoriesFactory.NewIdentityRepository().AuthAndGetClaimsIdentity(userName, password);
                }

                if (identityResult.Item1 == null) return new Tuple<bool, string, CompleteAccessToken>(false, identityResult.Item2, null);
                if (identityResult.Item3.UserType != IdentityUserType.Client)
                    if (identityResult.Item3.UserType != IdentityUserType.Workplace)
                        return new Tuple<bool, string, CompleteAccessToken>(false, _localizer[SharedResourceNames.INVALID_CREDENTIALS], null);


                Client client = _clientRepositoriesFactory.NewClientRepository(connection).GetByNetIdWithRoleAndType(identityResult.Item3.NetId);

                if (client?.ClientInRole != null) {
                    if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")) {
                        if (!client.ClientInRole.ClientTypeRoleId.Equals(1)) return new Tuple<bool, string, CompleteAccessToken>(false, "Invalid region", null);
                    } else {
                        if (client.ClientInRole.ClientTypeRoleId.Equals(1)) return new Tuple<bool, string, CompleteAccessToken>(false, "Invalid region", null);
                    }
                }
            }

            using (IDbConnection identityConnection = _connectionFactory.NewIdentitySqlConnection()) {
                return new Tuple<bool, string, CompleteAccessToken>(
                    true,
                    string.Empty,
                    GenerateAccessAndRefreshToken(
                        _identityRepositoriesFactory.NewUserTokenRepository(identityConnection),
                        identityResult.Item1.Claims,
                        identityResult.Item3
                    )
                );
            }
        } catch (Exception exc) {
            return new Tuple<bool, string, CompleteAccessToken>(false, exc.Message, null);
        }
    }


    private CompleteAccessToken GenerateAccessAndRefreshToken(
        IUserTokenRepository userTokensRepository,
        IEnumerable<Claim> claims,
        UserIdentity user) {
        DateTime now = DateTime.UtcNow;

        JwtSecurityToken jwt = new(
            AuthOptions.ISSUER,
            AuthOptions.AUDIENCE_LOCAL,
            notBefore: now,
            claims: claims,
            expires: now.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),
            signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256)
        );

        RefreshToken newRefreshToken = new() {
            UserId = user.Id,
            ExpireAt = DateTime.Now.AddMinutes(AuthOptions.REFRESH_LIFETIME)
        };

        string encryptedRefreshToken = AesManager.Encrypt(JsonConvert.SerializeObject(newRefreshToken));

        if (userTokensRepository.IsTokenExistForUser(user.Id)) {
            UserToken userToken = userTokensRepository.GetByUserId(user.Id);

            userToken.Token = encryptedRefreshToken;

            userTokensRepository.Update(userToken);
        } else {
            UserToken userToken = new() {
                Token = encryptedRefreshToken,
                UserId = user.Id
            };

            userTokensRepository.Add(userToken);
        }

        return new CompleteAccessToken {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(jwt),
            RefreshToken = encryptedRefreshToken,
            UserNetUid = user.NetId
        };
    }
}
