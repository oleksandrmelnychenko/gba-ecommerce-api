using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.IdentityEntities;
using GBA.Domain.Messages.UserManagement.UserProfiles;
using GBA.Domain.Repositories.Identities.Contracts;
using GBA.Domain.Repositories.UserRoles.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Domain.TransactionUnit;
using GBA.Domain.TransactionUnit.Contracts;

namespace GBA.Services.Actors.UserManagement;

public sealed class UserProfilesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IIdentityRepositoriesFactory _identityRepositoriesFactory;
    private readonly ITransactionUnitFactory _transactionUnitFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IUserRoleRepositoriesFactory _userRoleRepositoriesFactory;

    public UserProfilesActor(
        IDbConnectionFactory connectionFactory,
        ITransactionUnitFactory transactionUnitFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IIdentityRepositoriesFactory identityRepositoriesFactory,
        IUserRoleRepositoriesFactory userRoleRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _transactionUnitFactory = transactionUnitFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _identityRepositoriesFactory = identityRepositoriesFactory;
        _userRoleRepositoriesFactory = userRoleRepositoriesFactory;

        Receive<GetAllUserProfilesMessage>(Process);

        Receive<GetAllSalesManagersMessage>(Process);

        Receive<GetAllFromSearchMessage>(Process);

        Receive<GetAllPurchaseManagersMessage>(Process);

        Receive<GetUserProfileByNetIdMessage>(Process);

        ReceiveAsync<UpdateUserProfileMessage>(async message => {
            await Process(message);
        });

        ReceiveAsync<DeleteUserProfileMessage>(async message => {
            await Process(message);
        });

        Receive<AddScreenResolutionMessage>(Process);

        Receive<GetAllUserProfileByRoleTypesMessage>(Process);

        Receive<ResetPasswordByUserNetIdMessage>(Process);

        Receive<GetManagersFromSearchMessage>(Process);
    }

    private void Process(GetAllUserProfilesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_userRepositoriesFactory.NewUserRepository(connection).GetAll());
    }

    private void Process(GetAllSalesManagersMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_userRepositoriesFactory.NewUserRepository(connection).GetAllSalesManagers());
    }

    private void Process(GetAllFromSearchMessage message) {
        if (string.IsNullOrEmpty(message.Value)) message.Value = string.Empty;

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_userRepositoriesFactory.NewUserRepository(connection).GetAllFromSearch(message.Value));
    }

    private void Process(GetAllPurchaseManagersMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_userRepositoriesFactory.NewUserRepository(connection).GetAllPurchaseManagers());
    }

    private void Process(GetUserProfileByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.NetUid));
    }

    private async Task Process(UpdateUserProfileMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);

            UserRole userProfileRole = _userRoleRepositoriesFactory.NewUserRoleRepository(connection).GetByNetIdWithoutTranslation(message.UserProfile.UserRole.NetUid);

            message.UserProfile.UserRoleId = userProfileRole.Id;

            if (!string.IsNullOrEmpty(message.UserProfile.LastName)) {
                char[] chars = message.UserProfile.LastName.ToCharArray();

                if (chars.Any()) message.UserProfile.Abbreviation = chars.First().ToString();
            }

            if (!string.IsNullOrEmpty(message.UserProfile.FirstName)) {
                char[] chars = message.UserProfile.FirstName.ToCharArray();

                if (chars.Any()) message.UserProfile.Abbreviation += chars.First();
            }

            userRepository.Update(message.UserProfile);

            User user = userRepository.GetByNetId(message.UserProfile.NetUid);

            user.UserRole = message.UserProfile.UserRole;

            await _identityRepositoriesFactory.NewIdentityRolesRepository().ChangeUserRole(user.NetUid, userProfileRole.Name);

            IIdentityRepository identityRepository = _identityRepositoriesFactory.NewIdentityRepository();

            UserIdentity userIdentity = await identityRepository.GetUserByNetId(user.NetUid.ToString());

            await identityRepository.UpdateUserRegion(user.NetUid, user.Region);

            if (!userIdentity.UserName.ToLower().Equals(user.PhoneNumber.ToLower())) {
                userIdentity.UserName = user.PhoneNumber;
                await identityRepository.UpdateUserName(userIdentity);
            }

            if (!userIdentity.Email.ToLower().Equals(user.Email.ToLower())) await identityRepository.UpdateUsersEmail(userIdentity, message.UserProfile.Email);

            Sender.Tell(user);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private async Task Process(DeleteUserProfileMessage message) {
        using TransactionUnit transactionUnit = _transactionUnitFactory.New();
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _userRepositoriesFactory.NewUserRepository(connection).Remove(message.NetUid);

        transactionUnit.Complete();

        await _identityRepositoriesFactory.NewIdentityRepository().DeleteUserByNetId(message.NetUid.ToString());
    }

    private void Process(AddScreenResolutionMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IUserScreenResolutionRepository userScreenResolutionRepository = _userRepositoriesFactory.NewUserScreenResolutionRepository(connection);

        User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

        if (user == null) return;

        UserScreenResolution userScreenResolution = userScreenResolutionRepository.GetByUserNetId(user.NetUid);
        if (userScreenResolution != null) {
            userScreenResolution.Height = message.Height;
            userScreenResolution.Width = message.Width;

            userScreenResolutionRepository.Update(userScreenResolution);
        } else {
            userScreenResolution = new UserScreenResolution {
                Height = message.Height,
                Width = message.Width,
                UserId = user.Id
            };
            userScreenResolutionRepository.Add(userScreenResolution);
        }

        Sender.Tell(userScreenResolutionRepository.GetByUserNetId(user.NetUid));
    }

    private void Process(GetAllUserProfileByRoleTypesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_userRepositoriesFactory
            .NewUserRepository(connection)
            .GetAllByUserRoleTypes(message.UserRoleTypes)
        );
    }

    private void Process(ResetPasswordByUserNetIdMessage message) {
        Sender.Tell(_identityRepositoriesFactory.NewIdentityRepository().ResetPassword(message.NetId.ToString(), message.Password).Result);
    }

    private void Process(GetManagersFromSearchMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_userRepositoriesFactory.NewUserRepository(connection).GetManagersFromSearch(message.Value));
    }
}