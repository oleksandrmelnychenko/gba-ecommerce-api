using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Messages.UserManagement;
using GBA.Domain.Repositories.UserRoles.Contracts;

namespace GBA.Services.Actors.UserManagement;

public sealed class UserPermissionsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IUserRoleRepositoriesFactory _userRoleRepositoriesFactory;

    public UserPermissionsActor(
        IDbConnectionFactory connectionFactory,
        IUserRoleRepositoriesFactory roleRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRoleRepositoriesFactory = roleRepositoriesFactory;

        Receive<AddPermissionMessage>(ProcessAddPermissionMessage);

        Receive<UpdatePermissionMessage>(ProcessUpdatePermissionMessage);

        Receive<GetPermissionsByDashboardNodeIdMessage>(ProcessGetPermissionsByDashboardNodeIdMessage);
    }

    private void ProcessUpdatePermissionMessage(UpdatePermissionMessage message) {
        if (message.Permission == null || message.Permission.IsNew()) {
            Sender.Tell(new Tuple<bool, string>(false, "Object do not exists in database"));
        } else {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            IPermissionsRepository permissionsRepository = _userRoleRepositoriesFactory.NewPermissionsRepository(connection);

            permissionsRepository.Update(message.Permission);

            Sender.Tell(new Tuple<bool, string>(true, string.Empty));
        }
    }

    private void ProcessGetPermissionsByDashboardNodeIdMessage(GetPermissionsByDashboardNodeIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();

        IPermissionsRepository permissionsRepository = _userRoleRepositoriesFactory.NewPermissionsRepository(connection);

        Sender.Tell(permissionsRepository.GetPermissionsByDashboardNodeId(message.DashboardNodeId));
    }

    private void ProcessAddPermissionMessage(AddPermissionMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();

        IPermissionsRepository permissionsRepository = _userRoleRepositoriesFactory.NewPermissionsRepository(connection);

        long id = permissionsRepository.Add(message.Permission);

        Sender.Tell(new Tuple<Permission, string>(permissionsRepository.GetById(id), string.Empty));
    }
}