using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Common.IdentityConfiguration;
using GBA.Common.IdentityConfiguration.Entities;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.IdentityEntities;
using GBA.Domain.Messages.UserManagement;
using GBA.Domain.Repositories.Identities.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace GBA.Services.Actors.UserManagement;

public sealed class RequestTokenActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IIdentityRepositoriesFactory _identityRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public RequestTokenActor(
        IDbConnectionFactory connectionFactory,
        IIdentityRepositoriesFactory identityRepositoriesFactory,
        IUserRepositoriesFactory userRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _identityRepositoriesFactory = identityRepositoriesFactory;

        ReceiveAsync<RefreshTokenMessage>(async message => {
            try {
                using IDbConnection connection = _connectionFactory.NewIdentitySqlConnection();
                string decryptedToken = AesManager.Decrypt(message.RefreshToken);

                RefreshToken deserializedRefreshToken = JsonConvert.DeserializeObject<RefreshToken>(decryptedToken);

                if (deserializedRefreshToken.ExpireAt < DateTime.Now) throw new Exception("Refresh token expired");

                Tuple<ClaimsIdentity, UserIdentity> result = await _identityRepositoriesFactory.NewIdentityRepository()
                    .AuthAndGetClaimsIdentityByUserId(deserializedRefreshToken.UserId);

                if (result == null) throw new Exception("Refresh token invalid");

                Sender.Tell(new Tuple<bool, string, CompleteAccessToken>(
                    true,
                    string.Empty,
                    GenerateAccessAndRefreshToken(_identityRepositoriesFactory.NewUserTokenRepository(connection), result.Item1.Claims, result.Item2)
                ));
            } catch (Exception) {
                Sender.Tell(new Tuple<bool, string, CompleteAccessToken>(false, "Refresh token invalid", null));
            }
        });

        ReceiveAsync<RequestTokenMessage>(async message => {
            try {
                using IDbConnection connection = _connectionFactory.NewIdentitySqlConnection();
                if (string.IsNullOrEmpty(message.UserName) || string.IsNullOrEmpty(message.Password))
                    Sender.Tell(new Tuple<bool, string, CompleteAccessToken>(false, "Invalid credentials", null));

                UserIdentity user = await _identityRepositoriesFactory.NewIdentityRepository().GetUserName(message.UserName);
                UserIdentity userIdentity = _userRepositoriesFactory.NewUserRepository(connection).GetUserIdentity(user.NetId);
                Tuple<ClaimsIdentity, string, UserIdentity> identityResult =
                    await _identityRepositoriesFactory.NewIdentityRepository().AuthAndGetClaimsIdentity(userIdentity, message.Password);

                //Tuple<ClaimsIdentity, string, UserIdentity> identityResult =
                //await _identityRepositoriesFactory.NewIdentityRepository().AuthAndGetClaimsIdentity(message.UserName, message.Password);

                if (identityResult.Item1 == null) {
                    Sender.Tell(new Tuple<bool, string, CompleteAccessToken>(false, identityResult.Item2, null));
                    return;
                }

                Sender.Tell(new Tuple<bool, string, CompleteAccessToken>(
                    true,
                    string.Empty,
                    GenerateAccessAndRefreshToken(_identityRepositoriesFactory.NewUserTokenRepository(connection), identityResult.Item1.Claims, identityResult.Item3)
                ));
            } catch (Exception exc) {
                Sender.Tell(new Tuple<bool, string, CompleteAccessToken>(false, exc.Message, null));
            }
        });
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