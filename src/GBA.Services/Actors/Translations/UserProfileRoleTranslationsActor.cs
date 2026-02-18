using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Translations.UserProfileRoles;
using GBA.Domain.Repositories.UserRoles.Contracts;

namespace GBA.Services.Actors.Translations;

public sealed class UserProfileRoleTranslationsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IUserRoleRepositoriesFactory _userRoleRepositoriesFactory;

    public UserProfileRoleTranslationsActor(
        IDbConnectionFactory connectionFactory,
        IUserRoleRepositoriesFactory userRoleRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRoleRepositoriesFactory = userRoleRepositoriesFactory;

        Receive<AddUserProfileRoleTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IUserRoleTranslationRepository userRoleTranslationRepository = _userRoleRepositoriesFactory.NewUserRoleTranslationRepository(connection);

            message.UserProfileRoleTranslation.UserRoleId = message.UserProfileRoleTranslation.UserRole.Id;

            Sender.Tell(userRoleTranslationRepository.GetById(userRoleTranslationRepository.Add(message.UserProfileRoleTranslation)));
        });

        Receive<UpdateUserProfileRoleTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IUserRoleTranslationRepository userProfileRoleTranslationRepository = _userRoleRepositoriesFactory.NewUserRoleTranslationRepository(connection);

            userProfileRoleTranslationRepository.Update(message.UserProfileRoleTranslation);

            Sender.Tell(userProfileRoleTranslationRepository.GetByNetId(message.UserProfileRoleTranslation.NetUid));
        });

        Receive<GetAllUserProfileRoleTranslationsMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_userRoleRepositoriesFactory.NewUserRoleTranslationRepository(connection).GetAll());
        });

        Receive<GetUserProfileRoleTranslationByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_userRoleRepositoriesFactory.NewUserRoleTranslationRepository(connection).GetByNetId(message.NetId));
        });

        Receive<DeleteUserProfileRoleTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _userRoleRepositoriesFactory.NewUserRoleTranslationRepository(connection).Remove(message.NetId);
        });
    }
}