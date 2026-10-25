using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using GBA.Common.Configuration;
using GBA.Common.Helpers;
using GBA.Common.IdentityConfiguration.Entities;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.EntityHelpers;
using GBA.Domain.IdentityEntities;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Identities.Contracts;
using GBA.Ecommerce.Auth;
using GBA.Ecommerce.Controllers;
using GBA.Ecommerce.Controllers.UserManagement;
using GBA.Services.Services.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class ShopTokenSeparationTests {
    private const string ShopKey = "test-only-shop-signing-key-0123456789abcdef-shop";
    private const string ShopIssuer = "GbaShop";
    private const string ShopAudience = "GbaShop";
    private const string GbaKey = "test-only-gba-server-signing-key-0123456789abcdef";
    private static readonly RSA GbaRsa = RSA.Create(2048);
    private static readonly string GbaPublicKeyPem = GbaRsa.ExportSubjectPublicKeyInfoPem();
    private const string GbaIssuer = "ConcordCRM";
    private const string GbaAudience = "https://gba-api-test.example/";

    [Fact]
    public async Task Shop_customer_reaches_customer_endpoints_but_not_administration() {
        await using ShopHost host = await ShopHost.StartAsync(legacy: false, withGbaServer: true);
        string token = ShopToken("Client", "ClientUa");

        Assert.Equal(HttpStatusCode.OK, await host.GetAsync("/customer", token));
        Assert.Equal(HttpStatusCode.OK, await host.GetAsync("/customer-plain", token));
        Assert.Equal(HttpStatusCode.Forbidden, await host.PostAsync("/es-full", token));
        Assert.Equal(HttpStatusCode.Forbidden, await host.PostAsync("/maintenance", token));
        Assert.Equal(HttpStatusCode.Forbidden, await host.PostAsync("/region-update", token));
    }

    [Fact]
    public async Task Crm_administrator_reaches_administration_but_never_acts_as_a_customer() {
        await using ShopHost host = await ShopHost.StartAsync(legacy: false, withGbaServer: true);
        string administrative = GbaToken("User", "GBA");
        string administrator = GbaToken("User", "Administrator");

        Assert.Equal(HttpStatusCode.OK, await host.PostAsync("/es-full", administrative));
        Assert.Equal(HttpStatusCode.Forbidden, await host.PostAsync("/maintenance", administrative));
        Assert.Equal(HttpStatusCode.OK, await host.PostAsync("/maintenance", administrator));
        Assert.Equal(HttpStatusCode.OK, await host.PostAsync("/region-update", administrator));
        Assert.Equal(HttpStatusCode.Unauthorized, await host.GetAsync("/customer", administrator));
        Assert.Equal(HttpStatusCode.Unauthorized, await host.GetAsync("/customer-plain", administrator));
    }

    [Fact]
    public async Task Crm_user_without_an_administrative_role_is_forbidden() {
        await using ShopHost host = await ShopHost.StartAsync(legacy: false, withGbaServer: true);

        Assert.Equal(HttpStatusCode.Forbidden, await host.PostAsync("/es-full", GbaToken("User", "SalesAnalytic")));
    }

    [Fact]
    public async Task Gba_signed_customer_token_is_rejected_by_both_schemes_when_strict() {
        await using ShopHost host = await ShopHost.StartAsync(legacy: false, withGbaServer: true);
        string token = GbaToken("Client", "ClientUa");

        Assert.Equal(HttpStatusCode.Unauthorized, await host.GetAsync("/customer", token));
        Assert.Equal(HttpStatusCode.Unauthorized, await host.PostAsync("/es-full", token));
    }

    [Fact]
    public async Task Hs256_tokens_signed_with_the_former_shared_key_are_accepted_nowhere() {
        await using ShopHost host = await ShopHost.StartAsync(legacy: true, withGbaServer: true);

        Assert.Equal(HttpStatusCode.Unauthorized, await host.GetAsync("/customer", FormerSharedToken("Client", "ClientUa")));
        Assert.Equal(HttpStatusCode.Unauthorized, await host.GetAsync("/customer", FormerSharedToken("Workplace", "Workplace")));
        Assert.Equal(HttpStatusCode.Unauthorized, await host.PostAsync("/es-full", FormerSharedToken("User", "GBA")));
        Assert.Equal(HttpStatusCode.Unauthorized, await host.PostAsync("/maintenance", FormerSharedToken("User", "Administrator")));
        Assert.Equal(HttpStatusCode.OK, await host.PostAsync("/es-full", GbaToken("User", "GBA")));
    }

    [Fact]
    public async Task Crm_tokens_signed_by_an_unpublished_rsa_key_are_rejected() {
        await using ShopHost host = await ShopHost.StartAsync(legacy: false, withGbaServer: true);
        using RSA other = RSA.Create(2048);

        Assert.Equal(HttpStatusCode.Unauthorized, await host.PostAsync("/es-full", Mint(
            new SigningCredentials(new RsaSecurityKey(other), SecurityAlgorithms.RsaSha256),
            GbaIssuer, GbaAudience, "User", "GBA")));
    }

    [Fact]
    public void The_shop_holds_only_the_public_half_of_the_gba_server_key() {
        GbaServerAuthSettings settings = GbaServerAuthSettings.Create(GbaServer(), Security(legacy: false));
        RsaSecurityKey key = Assert.Single(settings.ValidationKeys);

        Assert.Null(key.Rsa);
        Assert.Null(key.Parameters.D);
        Assert.Equal(PrivateKeyStatus.DoesNotExist, key.PrivateKeyStatus);
        Assert.Equal(
            Base64UrlEncoder.Encode(JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(GbaRsa.ExportParameters(false))).ComputeJwkThumbprint()),
            key.KeyId);
    }

    [Fact]
    public async Task Without_gba_server_settings_crm_tokens_reach_nothing() {
        await using ShopHost host = await ShopHost.StartAsync(legacy: false, withGbaServer: false);
        string token = GbaToken("User", "Administrator");

        Assert.Equal(HttpStatusCode.Unauthorized, await host.PostAsync("/es-full", token));
        Assert.Equal(HttpStatusCode.Unauthorized, await host.PostAsync("/maintenance", token));
        Assert.Equal(HttpStatusCode.Unauthorized, await host.GetAsync("/customer", token));
    }

    [Fact]
    public async Task Anonymous_callers_reach_only_anonymous_endpoints() {
        await using ShopHost host = await ShopHost.StartAsync(legacy: false, withGbaServer: true);

        Assert.Equal(HttpStatusCode.OK, await host.GetAsync("/es-health", token: null));
        Assert.Equal(HttpStatusCode.Unauthorized, await host.GetAsync("/customer", token: null));
        Assert.Equal(HttpStatusCode.Unauthorized, await host.PostAsync("/es-full", token: null));
    }

    [Fact]
    public void Gba_server_settings_accept_only_a_strong_public_key_with_its_own_audience() {
        SecuritySettings security = Security(legacy: false);
        using RSA weak = RSA.Create(1024);

        Assert.Contains("no longer accepted", Assert.Throws<InvalidOperationException>(() => GbaServerAuthSettings.Initialize(
            new GbaServerAuthSettings { JwtKey = GbaKey, JwtPublicKey = GbaPublicKeyPem, JwtIssuer = GbaIssuer, JwtAudience = GbaAudience },
            security)).Message, StringComparison.Ordinal);
        Assert.Contains("private key", Assert.Throws<InvalidOperationException>(() => GbaServerAuthSettings.Initialize(
            new GbaServerAuthSettings { JwtPublicKey = GbaRsa.ExportPkcs8PrivateKeyPem(), JwtIssuer = GbaIssuer, JwtAudience = GbaAudience },
            security)).Message, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => GbaServerAuthSettings.Initialize(
            new GbaServerAuthSettings { JwtPublicKey = GbaPublicKeyPem, JwtIssuer = GbaIssuer, JwtAudience = ShopAudience },
            security));
        Assert.Throws<InvalidOperationException>(() => GbaServerAuthSettings.Initialize(
            new GbaServerAuthSettings { JwtPublicKey = weak.ExportSubjectPublicKeyInfoPem(), JwtIssuer = GbaIssuer, JwtAudience = GbaAudience },
            security));
        Assert.False(GbaServerAuthSettings.Initialize(new GbaServerAuthSettings(), security).IsConfigured);
    }

    [Fact]
    public void Shop_startup_fails_without_its_own_signing_key_or_with_a_raw_legacy_key() {
        SecuritySettings missing = Security(legacy: false);
        missing.JwtKey = string.Empty;
        SecuritySettings rawLegacy = Security(legacy: true);
        rawLegacy.LegacyRefreshTokenKey = GbaKey;

        Assert.Contains("Security:JwtKey", Assert.Throws<InvalidOperationException>(() => SecuritySettings.Initialize(missing)).Message,
            StringComparison.Ordinal);
        Assert.Contains("LegacyRefreshTokenKey", Assert.Throws<InvalidOperationException>(() => SecuritySettings.Initialize(rawLegacy)).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Administration_endpoints_use_the_explicit_administration_policies() {
        Assert.Equal(
            GBA.Common.IdentityConfiguration.Policies.DefaultPolicies.ShopAdministrativePolicy,
            typeof(ElasticsearchController).GetCustomAttribute<AuthorizeAttribute>()!.Policy);
        Assert.Equal(
            GBA.Common.IdentityConfiguration.Policies.DefaultPolicies.ShopAdministratorPolicy,
            typeof(MaintenanceController).GetCustomAttribute<AuthorizeAttribute>()!.Policy);
        Assert.Equal(
            GBA.Common.IdentityConfiguration.Policies.DefaultPolicies.ShopAdministratorPolicy,
            typeof(EcommerceRegionsController).GetMethod(nameof(EcommerceRegionsController.Update))!
                .GetCustomAttribute<AuthorizeAttribute>()!.Policy);
        Assert.DoesNotContain(
            typeof(ElasticsearchController).Assembly.GetTypes()
                .SelectMany(type => type.GetCustomAttributes<AuthorizeAttribute>(inherit: false)
                    .Concat(type.GetMethods().SelectMany(method => method.GetCustomAttributes<AuthorizeAttribute>(inherit: false)))),
            attribute => attribute.Roles?.Contains("Administrator", StringComparison.Ordinal) == true ||
                         attribute.Roles?.Contains("GBA", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Refresh_tokens_from_the_shared_key_decrypt_only_during_the_transition() {
        string payload = JsonSerializer.Serialize(new RefreshToken {
            UserId = Guid.NewGuid().ToString(),
            ExpireAt = DateTime.UtcNow.AddDays(1)
        });

        InitializeSecurity(Security(legacy: false, jwtKey: GbaKey), gbaServer: false);
        string legacyRefreshToken = AesManager.Encrypt(payload);
        Assert.Equal(48, AesManager.DeriveKeyMaterial(GbaKey).Length);

        InitializeSecurity(Security(legacy: false), gbaServer: true);
        Assert.ThrowsAny<Exception>(() => RequestTokenService.DecryptRefreshToken(legacyRefreshToken));

        InitializeSecurity(Security(legacy: true), gbaServer: true);
        Assert.Equal(payload, RequestTokenService.DecryptRefreshToken(legacyRefreshToken));

        string currentRefreshToken = AesManager.Encrypt(payload);
        Assert.NotEqual(legacyRefreshToken, currentRefreshToken);
        Assert.Equal(payload, RequestTokenService.DecryptRefreshToken(currentRefreshToken));
    }

    [Fact]
    public async Task Logout_revokes_only_the_presented_refresh_token() {
        InitializeSecurity(Security(legacy: false), gbaServer: true);
        string userId = Guid.NewGuid().ToString();
        string sessionId = Guid.NewGuid().ToString("N");
        string refreshToken = AesManager.Encrypt(JsonSerializer.Serialize(new RefreshToken {
            UserId = userId,
            SessionId = sessionId,
            ExpireAt = DateTime.UtcNow.AddDays(1)
        }));
        string stale = AesManager.Encrypt(JsonSerializer.Serialize(new RefreshToken {
            UserId = userId,
            ExpireAt = DateTime.UtcNow.AddDays(2)
        }));
        UserToken row = new() {
            Id = 17,
            UserId = userId,
            Token = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)))
        };

        Mock<IDbConnection> identityConnection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory.Setup(factory => factory.NewIdentitySqlConnection()).Returns(identityConnection.Object);
        Mock<IUserTokenRepository> userTokens = new();
        userTokens.Setup(repository => repository.GetByUserId(userId)).Returns(row);
        userTokens.Setup(repository => repository.TryReplace(
                row.Id,
                row.UserId,
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns((long _, string _, string expected, string replacement) => {
                if (!string.Equals(row.Token, expected, StringComparison.Ordinal)) return false;
                row.Token = replacement;
                return true;
            });
        Mock<IIdentityRepositoriesFactory> identityFactory = new();
        identityFactory.Setup(factory => factory.NewUserTokenRepository(identityConnection.Object)).Returns(userTokens.Object);
        RequestTokenService service = new(
            identityFactory.Object,
            Mock.Of<IClientRepositoriesFactory>(),
            connectionFactory.Object,
            Mock.Of<IStringLocalizer<SharedResource>>());

        Assert.False(await service.RevokeRefreshToken(stale));
        userTokens.Verify(repository => repository.TryReplace(
            It.IsAny<long>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);

        Assert.True(await service.RevokeRefreshToken(refreshToken));
        Assert.Equal(string.Empty, row.Token);
        userTokens.Verify(repository => repository.TryReplace(
            row.Id,
            row.UserId,
            It.IsAny<string>(),
            string.Empty), Times.Once);
        Assert.True(await service.RevokeRefreshToken("not-a-token"));
    }

    [Fact]
    public async Task Logout_does_not_mask_identity_store_failures_as_an_invalid_token() {
        InitializeSecurity(Security(legacy: false), gbaServer: true);
        string userId = Guid.NewGuid().ToString();
        string refreshToken = AesManager.Encrypt(JsonSerializer.Serialize(new RefreshToken {
            UserId = userId,
            ExpireAt = DateTime.UtcNow.AddDays(1)
        }));

        Mock<IDbConnection> identityConnection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory.Setup(factory => factory.NewIdentitySqlConnection()).Returns(identityConnection.Object);
        Mock<IUserTokenRepository> userTokens = new();
        userTokens.Setup(repository => repository.GetByUserId(userId))
            .Throws(new DataException("identity store unavailable"));
        Mock<IIdentityRepositoriesFactory> identityFactory = new();
        identityFactory.Setup(factory => factory.NewUserTokenRepository(identityConnection.Object)).Returns(userTokens.Object);
        RequestTokenService service = new(
            identityFactory.Object,
            Mock.Of<IClientRepositoriesFactory>(),
            connectionFactory.Object,
            Mock.Of<IStringLocalizer<SharedResource>>());

        DataException error = await Assert.ThrowsAsync<DataException>(
            () => service.RevokeRefreshToken(refreshToken));

        Assert.Equal("identity store unavailable", error.Message);
    }

    [Fact]
    public async Task Logout_is_idempotent_when_the_refresh_token_row_is_already_missing() {
        InitializeSecurity(Security(legacy: false), gbaServer: true);
        string userId = Guid.NewGuid().ToString();
        string refreshToken = AesManager.Encrypt(JsonSerializer.Serialize(new RefreshToken {
            UserId = userId,
            ExpireAt = DateTime.UtcNow.AddDays(1)
        }));

        Mock<IDbConnection> identityConnection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory.Setup(factory => factory.NewIdentitySqlConnection()).Returns(identityConnection.Object);
        Mock<IUserTokenRepository> userTokens = new();
        userTokens.Setup(repository => repository.GetByUserId(userId)).Returns((UserToken)null!);
        Mock<IIdentityRepositoriesFactory> identityFactory = new();
        identityFactory.Setup(factory => factory.NewUserTokenRepository(identityConnection.Object)).Returns(userTokens.Object);
        RequestTokenService service = new(
            identityFactory.Object,
            Mock.Of<IClientRepositoriesFactory>(),
            connectionFactory.Object,
            Mock.Of<IStringLocalizer<SharedResource>>());

        Assert.True(await service.RevokeRefreshToken(refreshToken));
        userTokens.Verify(repository => repository.TryReplace(
            It.IsAny<long>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Logout_does_not_mask_compare_and_swap_failures() {
        InitializeSecurity(Security(legacy: false), gbaServer: true);
        string userId = Guid.NewGuid().ToString();
        string refreshToken = AesManager.Encrypt(JsonSerializer.Serialize(new RefreshToken {
            UserId = userId,
            ExpireAt = DateTime.UtcNow.AddDays(1)
        }));
        UserToken row = new() {
            Id = 23,
            UserId = userId,
            Token = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)))
        };

        Mock<IDbConnection> identityConnection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory.Setup(factory => factory.NewIdentitySqlConnection()).Returns(identityConnection.Object);
        Mock<IUserTokenRepository> userTokens = new();
        userTokens.Setup(repository => repository.GetByUserId(userId)).Returns(row);
        userTokens.Setup(repository => repository.TryReplace(
                row.Id,
                row.UserId,
                row.Token,
                string.Empty))
            .Throws(new DataException("compare-and-swap unavailable"));
        Mock<IIdentityRepositoriesFactory> identityFactory = new();
        identityFactory.Setup(factory => factory.NewUserTokenRepository(identityConnection.Object)).Returns(userTokens.Object);
        RequestTokenService service = new(
            identityFactory.Object,
            Mock.Of<IClientRepositoriesFactory>(),
            connectionFactory.Object,
            Mock.Of<IStringLocalizer<SharedResource>>());

        DataException error = await Assert.ThrowsAsync<DataException>(
            () => service.RevokeRefreshToken(refreshToken));

        Assert.Equal("compare-and-swap unavailable", error.Message);
    }

    [Fact]
    public async Task Concurrent_logout_prevents_an_inflight_refresh_from_restoring_the_session() {
        InitializeSecurity(Security(legacy: false), gbaServer: true);
        string userId = Guid.NewGuid().ToString();
        string refreshToken = AesManager.Encrypt(JsonSerializer.Serialize(new RefreshToken {
            UserId = userId,
            ExpireAt = DateTime.UtcNow.AddDays(1)
        }));
        string storedToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        const long tokenId = 29;
        object tokenLock = new();
        TaskCompletionSource lookupStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<Tuple<ClaimsIdentity, UserIdentity>> releaseLookup =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        Mock<IDbConnection> identityConnection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory.Setup(factory => factory.NewIdentitySqlConnection()).Returns(identityConnection.Object);
        Mock<IUserTokenRepository> userTokens = new();
        userTokens.Setup(repository => repository.GetByUserId(userId))
            .Returns(() => {
                lock (tokenLock) {
                    return new UserToken {Id = tokenId, UserId = userId, Token = storedToken};
                }
            });
        userTokens.Setup(repository => repository.TryReplace(
                tokenId,
                userId,
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns((long _, string _, string expected, string replacement) => {
                lock (tokenLock) {
                    if (!string.Equals(storedToken, expected, StringComparison.Ordinal)) return false;
                    storedToken = replacement;
                    return true;
                }
            });
        UserIdentity user = new() {
            Id = userId,
            UserName = "shop-client",
            NetId = Guid.NewGuid(),
            UserType = IdentityUserType.Client
        };
        Mock<IIdentityRepository> identities = new();
        identities.Setup(repository => repository.AuthAndGetClaimsIdentityByUserId(userId))
            .Returns(() => {
                lookupStarted.TrySetResult();
                return releaseLookup.Task;
            });
        Mock<IIdentityRepositoriesFactory> identityFactory = new();
        identityFactory.Setup(factory => factory.NewUserTokenRepository(identityConnection.Object)).Returns(userTokens.Object);
        identityFactory.Setup(factory => factory.NewIdentityRepository()).Returns(identities.Object);
        RequestTokenService service = new(
            identityFactory.Object,
            Mock.Of<IClientRepositoriesFactory>(),
            connectionFactory.Object,
            Mock.Of<IStringLocalizer<SharedResource>>());

        Task<Tuple<bool, string, CompleteAccessToken>> refresh = service.RefreshToken(refreshToken);
        await lookupStarted.Task;

        Assert.True(await service.RevokeRefreshToken(refreshToken));
        releaseLookup.SetResult(new Tuple<ClaimsIdentity, UserIdentity>(
            new ClaimsIdentity(
                new GenericIdentity(user.UserName, "Token"),
                [new Claim("type", "Client")]),
            user));

        Tuple<bool, string, CompleteAccessToken> result = await refresh;
        Assert.False(result.Item1);
        Assert.Null(result.Item3);
        Assert.Equal(string.Empty, storedToken);
    }

    [Fact]
    public async Task Refresh_rotates_the_current_token_with_compare_and_swap() {
        InitializeSecurity(Security(legacy: false), gbaServer: true);
        string userId = Guid.NewGuid().ToString();
        string sessionId = Guid.NewGuid().ToString("N");
        string refreshToken = AesManager.Encrypt(JsonSerializer.Serialize(new RefreshToken {
            UserId = userId,
            SessionId = sessionId,
            ExpireAt = DateTime.UtcNow.AddDays(1)
        }));
        string storedToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        const long tokenId = 31;
        UserIdentity user = new() {
            Id = userId,
            UserName = "shop-client",
            NetId = Guid.NewGuid(),
            UserType = IdentityUserType.Client
        };

        Mock<IDbConnection> identityConnection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory.Setup(factory => factory.NewIdentitySqlConnection()).Returns(identityConnection.Object);
        Mock<IUserTokenRepository> userTokens = new();
        userTokens.Setup(repository => repository.GetByUserId(userId))
            .Returns(() => new UserToken {Id = tokenId, UserId = userId, Token = storedToken});
        userTokens.Setup(repository => repository.TryReplace(
                tokenId,
                userId,
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns((long _, string _, string expected, string replacement) => {
                if (!string.Equals(storedToken, expected, StringComparison.Ordinal)) return false;
                storedToken = replacement;
                return true;
            });
        Mock<IIdentityRepository> identities = new();
        identities.Setup(repository => repository.AuthAndGetClaimsIdentityByUserId(userId))
            .ReturnsAsync(new Tuple<ClaimsIdentity, UserIdentity>(
                new ClaimsIdentity(
                    new GenericIdentity(user.UserName, "Token"),
                    [new Claim("type", "Client")]),
                user));
        Mock<IIdentityRepositoriesFactory> identityFactory = new();
        identityFactory.Setup(factory => factory.NewUserTokenRepository(identityConnection.Object)).Returns(userTokens.Object);
        identityFactory.Setup(factory => factory.NewIdentityRepository()).Returns(identities.Object);
        RequestTokenService service = new(
            identityFactory.Object,
            Mock.Of<IClientRepositoriesFactory>(),
            connectionFactory.Object,
            Mock.Of<IStringLocalizer<SharedResource>>());

        Tuple<bool, string, CompleteAccessToken> result = await service.RefreshToken(refreshToken);

        Assert.True(result.Item1);
        Assert.NotNull(result.Item3);
        Assert.NotEqual(refreshToken, result.Item3.RefreshToken);
        Assert.Equal(
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(result.Item3.RefreshToken))),
            storedToken);
        JwtSecurityToken rotatedAccessToken = new JwtSecurityTokenHandler()
            .ReadJwtToken(result.Item3.AccessToken);
        Assert.Equal(
            sessionId,
            Assert.Single(
                rotatedAccessToken.Claims,
                claim => claim.Type == RequestTokenService.SessionIdClaimType).Value);
        RefreshToken rotatedRefreshToken = JsonSerializer.Deserialize<RefreshToken>(
            RequestTokenService.DecryptRefreshToken(result.Item3.RefreshToken))!;
        Assert.Equal(sessionId, rotatedRefreshToken.SessionId);
        Assert.False(await service.RevokeRefreshToken(refreshToken));
    }

    [Fact]
    public async Task Refresh_never_reissues_a_shop_token_for_an_internal_user_outside_development() {
        InitializeSecurity(Security(legacy: false), gbaServer: true);
        UserIdentity user = new() {
            Id = Guid.NewGuid().ToString(),
            UserName = "crm-user",
            NetId = Guid.NewGuid(),
            Region = "uk",
            UserType = IdentityUserType.User
        };
        string refreshToken = AesManager.Encrypt(JsonSerializer.Serialize(new RefreshToken {
            UserId = user.Id,
            ExpireAt = DateTime.UtcNow.AddDays(1)
        }));

        Mock<IDbConnection> identityConnection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory.Setup(factory => factory.NewIdentitySqlConnection()).Returns(identityConnection.Object);
        Mock<IUserTokenRepository> userTokens = new();
        userTokens.Setup(repository => repository.GetByUserId(user.Id)).Returns(new UserToken {
            UserId = user.Id,
            Token = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)))
        });
        Mock<IIdentityRepository> identities = new();
        identities.Setup(repository => repository.AuthAndGetClaimsIdentityByUserId(user.Id))
            .ReturnsAsync(new Tuple<ClaimsIdentity, UserIdentity>(
                new ClaimsIdentity(new GenericIdentity(user.UserName, "Token"), [new Claim("type", "User")]),
                user));
        Mock<IIdentityRepositoriesFactory> identityFactory = new();
        identityFactory.Setup(factory => factory.NewIdentityRepository()).Returns(identities.Object);
        identityFactory.Setup(factory => factory.NewUserTokenRepository(identityConnection.Object)).Returns(userTokens.Object);
        RequestTokenService service = new(
            identityFactory.Object,
            Mock.Of<IClientRepositoriesFactory>(),
            connectionFactory.Object,
            Mock.Of<IStringLocalizer<SharedResource>>());

        string? previousEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        try {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
            Tuple<bool, string, CompleteAccessToken> result = await service.RefreshToken(refreshToken);

            Assert.False(result.Item1);
            Assert.Null(result.Item3);
            userTokens.Verify(repository => repository.Update(It.IsAny<UserToken>()), Times.Never);
        } finally {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", previousEnvironment);
        }
    }

    private static SecuritySettings Security(bool legacy, string jwtKey = ShopKey) => new() {
        JwtKey = jwtKey,
        JwtIssuer = ShopIssuer,
        JwtAudience = ShopAudience,
        PriceEncryptionKey = "GBA_Test_Key_16!",
        PriceEncryptionIV = "GBA_Test_IV__16!",
        CorsOrigins = ["http://localhost"],
        AcceptLegacySharedTokens = legacy,
        LegacyRefreshTokenKey = legacy ? Convert.ToBase64String(AesManager.DeriveKeyMaterial(GbaKey)) : string.Empty
    };

    private static GbaServerAuthSettings GbaServer() => new() {
        JwtPublicKey = GbaPublicKeyPem,
        JwtIssuer = GbaIssuer,
        JwtAudience = GbaAudience
    };

    private static void InitializeSecurity(SecuritySettings security, bool gbaServer) {
        SecuritySettings.Initialize(security);
        GbaServerAuthSettings.Initialize(gbaServer ? GbaServer() : new GbaServerAuthSettings(), security);
    }

    private static string ShopToken(string type, string role) =>
        Mint(Hs256(ShopKey), ShopIssuer, ShopAudience, type, role);

    private static string GbaToken(string type, string role) =>
        Mint(new SigningCredentials(new RsaSecurityKey(GbaRsa) {
            KeyId = GbaServerAuthSettings.ComputeKeyId(GbaRsa.ExportParameters(false))
        }, SecurityAlgorithms.RsaSha256), GbaIssuer, GbaAudience, type, role);

    private static string FormerSharedToken(string type, string role) =>
        Mint(Hs256(GbaKey), GbaIssuer, GbaAudience, type, role);

    private static SigningCredentials Hs256(string key) =>
        new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

    private static string Mint(SigningCredentials credentials, string issuer, string audience, string type, string role) {
        DateTime now = DateTime.UtcNow;
        JwtSecurityToken token = new(
            issuer,
            audience,
            [
                new Claim("NetId", Guid.NewGuid().ToString()),
                new Claim("role", role),
                new Claim("type", type),
                new Claim("UserName", "tests")
            ],
            now.AddMinutes(-1),
            now.AddMinutes(15),
            credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class ShopHost : IAsyncDisposable {
        private readonly WebApplication _application;
        private readonly HttpClient _client;

        private ShopHost(WebApplication application, HttpClient client) {
            _application = application;
            _client = client;
        }

        public static async Task<ShopHost> StartAsync(bool legacy, bool withGbaServer) {
            SecuritySettings security = Security(legacy);
            GbaServerAuthSettings gbaServer = GbaServerAuthSettings.Create(
                withGbaServer ? GbaServer() : new GbaServerAuthSettings(),
                security);

            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions {
                EnvironmentName = "Testing"
            });
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
            builder.Services.AddShopAuthentication(security, gbaServer);

            WebApplication application = builder.Build();
            application.UseRouting();
            application.UseAuthentication();
            application.UseAuthorization();

            application.MapGet("/customer", () => Results.Ok())
                .WithMetadata(Metadata(typeof(OrdersController), nameof(OrdersController.GenerateOrderFromClientShoppingCartAsync)));
            application.MapGet("/customer-plain", () => Results.Ok())
                .WithMetadata(Metadata(typeof(UserProfilesController), nameof(UserProfilesController.GetClientByNetId)));
            application.MapPost("/es-full", () => Results.Ok())
                .WithMetadata(Metadata(typeof(ElasticsearchController), nameof(ElasticsearchController.FullSyncAsync)));
            application.MapGet("/es-health", () => Results.Ok())
                .WithMetadata(Metadata(typeof(ElasticsearchController), nameof(ElasticsearchController.HealthAsync)));
            application.MapPost("/maintenance", () => Results.Ok())
                .WithMetadata(Metadata(typeof(MaintenanceController), nameof(MaintenanceController.ReleaseExpiredCartsAsync)));
            application.MapPost("/region-update", () => Results.Ok())
                .WithMetadata(Metadata(typeof(EcommerceRegionsController), nameof(EcommerceRegionsController.Update)));

            await application.StartAsync();
            string address = application.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single();

            return new ShopHost(application, new HttpClient { BaseAddress = new Uri(address) });
        }

        public Task<HttpStatusCode> GetAsync(string path, string? token) => SendAsync(HttpMethod.Get, path, token);

        public Task<HttpStatusCode> PostAsync(string path, string? token) => SendAsync(HttpMethod.Post, path, token);

        public async ValueTask DisposeAsync() {
            _client.Dispose();
            await _application.DisposeAsync();
        }

        private async Task<HttpStatusCode> SendAsync(HttpMethod method, string path, string? token) {
            using HttpRequestMessage request = new(method, path);
            if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using HttpResponseMessage response = await _client.SendAsync(request);
            return response.StatusCode;
        }

        private static object[] Metadata(Type controllerType, string actionName) {
            MethodInfo action = controllerType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Single(method => method.Name == actionName);

            return controllerType.GetCustomAttributes(inherit: true)
                .Concat(action.GetCustomAttributes(inherit: true))
                .Where(attribute => attribute is IAuthorizeData or IAllowAnonymous)
                .ToArray();
        }
    }
}
