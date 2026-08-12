using System.Data;
using System.Security.Claims;
using System.Security.Principal;
using GBA.Common.Configuration;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.EntityHelpers;
using GBA.Domain.IdentityEntities;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Identities.Contracts;
using GBA.Services.Services.UserManagement;
using Microsoft.Extensions.Localization;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class RequestTokenServiceTests {
    private const string _login = "0000001089";
    private const string _password = "console-password-1089";

    [Fact]
    public async Task Console_created_exact_login_is_not_rebound_to_another_client_with_same_phone() {
        TestContext context = CreateContext();
        UserIdentity consoleIdentity = CreateClientIdentity();
        Client duplicatePhoneClient = new() { NetUid = Guid.NewGuid() };
        context.IdentityRepository
            .Setup(repository => repository.GetUserName(_login))
            .ReturnsAsync(consoleIdentity);
        context.IdentityRepository
            .Setup(repository => repository.AuthAndGetClaimsIdentity(consoleIdentity, _password))
            .ReturnsAsync(SuccessfulIdentity(consoleIdentity));
        context.ClientRepository
            .Setup(repository => repository.GetClientNetIdByMobileNumber(_login))
            .Returns(duplicatePhoneClient);
        context.IdentityRepository
            .Setup(repository => repository.AuthAndGetClaimsIdentityByNetId(
                duplicatePhoneClient.NetUid.ToString(),
                _password,
                _login))
            .ReturnsAsync(FailedIdentity());
        context.ClientRepository
            .Setup(repository => repository.GetByNetIdWithRoleAndType(consoleIdentity.NetId))
            .Returns(new Client { NetUid = consoleIdentity.NetId });

        Tuple<bool, string, GBA.Common.IdentityConfiguration.Entities.CompleteAccessToken> result =
            await context.Service.RequestToken(_login, _password);

        Assert.True(result.Item1);
        Assert.NotNull(result.Item3);
        Assert.Equal(consoleIdentity.NetId, result.Item3.UserNetUid);
        context.ClientRepository.Verify(
            repository => repository.GetClientNetIdByMobileNumber(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Wrong_password_for_exact_identity_cannot_fall_through_to_duplicate_phone_alias() {
        TestContext context = CreateContext();
        UserIdentity consoleIdentity = CreateClientIdentity();
        Client duplicatePhoneClient = new() { NetUid = Guid.NewGuid() };
        UserIdentity duplicateIdentity = CreateClientIdentity(duplicatePhoneClient.NetUid);
        context.IdentityRepository
            .Setup(repository => repository.GetUserName(_login))
            .ReturnsAsync(consoleIdentity);
        context.IdentityRepository
            .Setup(repository => repository.AuthAndGetClaimsIdentity(consoleIdentity, "wrong-password"))
            .ReturnsAsync(FailedIdentity());
        context.ClientRepository
            .Setup(repository => repository.GetClientNetIdByMobileNumber(_login))
            .Returns(duplicatePhoneClient);
        context.IdentityRepository
            .Setup(repository => repository.AuthAndGetClaimsIdentityByNetId(
                duplicatePhoneClient.NetUid.ToString(),
                "wrong-password",
                _login))
            .ReturnsAsync(SuccessfulIdentity(duplicateIdentity));

        Tuple<bool, string, GBA.Common.IdentityConfiguration.Entities.CompleteAccessToken> result =
            await context.Service.RequestToken(_login, "wrong-password");

        Assert.False(result.Item1);
        Assert.Null(result.Item3);
        context.ClientRepository.Verify(
            repository => repository.GetClientNetIdByMobileNumber(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Client_phone_alias_still_resolves_by_client_net_id_when_no_identity_uses_that_login() {
        TestContext context = CreateContext();
        Client client = new() { NetUid = Guid.NewGuid() };
        UserIdentity identity = CreateClientIdentity(client.NetUid);
        context.IdentityRepository
            .Setup(repository => repository.GetUserName(_login))
            .ReturnsAsync((UserIdentity)null!);
        context.ClientRepository
            .Setup(repository => repository.GetClientNetIdByMobileNumber(_login))
            .Returns(client);
        context.IdentityRepository
            .Setup(repository => repository.AuthAndGetClaimsIdentityByNetId(
                client.NetUid.ToString(),
                _password,
                _login))
            .ReturnsAsync(SuccessfulIdentity(identity));
        context.ClientRepository
            .Setup(repository => repository.GetByNetIdWithRoleAndType(identity.NetId))
            .Returns(client);

        Tuple<bool, string, GBA.Common.IdentityConfiguration.Entities.CompleteAccessToken> result =
            await context.Service.RequestToken(_login, _password);

        Assert.True(result.Item1);
        Assert.NotNull(result.Item3);
        Assert.Equal(identity.NetId, result.Item3.UserNetUid);
        context.IdentityRepository.Verify(
            repository => repository.AuthAndGetClaimsIdentityByNetId(
                client.NetUid.ToString(),
                _password,
                _login),
            Times.Once);
    }

    private static TestContext CreateContext() {
        SecuritySettings.Initialize(new SecuritySettings {
            JwtKey = new string('k', 64),
            JwtIssuer = "tests",
            JwtAudience = "tests",
            PriceEncryptionKey = "GBA_Test_Key_16!",
            PriceEncryptionIV = "GBA_Test_IV__16!",
            CorsOrigins = ["http://localhost"]
        });

        Mock<IDbConnection> clientConnection = new();
        Mock<IDbConnection> identityConnection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory
            .Setup(factory => factory.NewSqlConnection())
            .Returns(clientConnection.Object);
        connectionFactory
            .Setup(factory => factory.NewIdentitySqlConnection())
            .Returns(identityConnection.Object);

        Mock<IClientRepository> clientRepository = new();
        Mock<IClientRepositoriesFactory> clientRepositoriesFactory = new();
        clientRepositoriesFactory
            .Setup(factory => factory.NewClientRepository(clientConnection.Object))
            .Returns(clientRepository.Object);

        Mock<IIdentityRepository> identityRepository = new();
        Mock<IUserTokenRepository> userTokenRepository = new();
        Mock<IIdentityRepositoriesFactory> identityRepositoriesFactory = new();
        identityRepositoriesFactory
            .Setup(factory => factory.NewIdentityRepository())
            .Returns(identityRepository.Object);
        identityRepositoriesFactory
            .Setup(factory => factory.NewUserTokenRepository(identityConnection.Object))
            .Returns(userTokenRepository.Object);

        RequestTokenService service = new(
            identityRepositoriesFactory.Object,
            clientRepositoriesFactory.Object,
            connectionFactory.Object,
            Mock.Of<IStringLocalizer<SharedResource>>());

        return new TestContext(service, identityRepository, clientRepository);
    }

    private static UserIdentity CreateClientIdentity(Guid? netId = null) {
        return new UserIdentity {
            Id = Guid.NewGuid().ToString(),
            UserName = _login,
            NetId = netId ?? Guid.NewGuid(),
            Region = "uk",
            UserType = IdentityUserType.Client
        };
    }

    private static Tuple<ClaimsIdentity, string, UserIdentity> SuccessfulIdentity(UserIdentity user) {
        ClaimsIdentity claims = new(
            new GenericIdentity(user.UserName!, "Token"),
            [new Claim("NetId", user.NetId.ToString())]);
        return new Tuple<ClaimsIdentity, string, UserIdentity>(claims, string.Empty, user);
    }

    private static Tuple<ClaimsIdentity, string, UserIdentity> FailedIdentity() {
        return new Tuple<ClaimsIdentity, string, UserIdentity>(null!, "Invalid credentials", null!);
    }

    private sealed record TestContext(
        RequestTokenService Service,
        Mock<IIdentityRepository> IdentityRepository,
        Mock<IClientRepository> ClientRepository);
}
