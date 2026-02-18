using System;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Dashboards;
using GBA.Domain.Messages.UserManagement.RoleManagement;
using GBA.Domain.Messages.UserManagement.UserProfileRoles;
using GBA.Domain.Repositories.Identities.Contracts;
using GBA.Domain.Repositories.UserRoles.Contracts;
using Microsoft.AspNetCore.Identity;

namespace GBA.Services.Actors.UserManagement;

public sealed class UserProfileRolesActor : ReceiveActor {
    private const string DUPLICATED_NAME_ERROR = "DuplicateRoleName";
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IIdentityRolesRepository _identityRolesRepository;
    private readonly IUserRoleRepositoriesFactory _userRoleRepositoriesFactory;

    public UserProfileRolesActor(
        IDbConnectionFactory connectionFactory,
        IUserRoleRepositoriesFactory userRoleRepositoriesFactory,
        IIdentityRolesRepository identityRolesRepository) {
        _connectionFactory = connectionFactory;
        _userRoleRepositoriesFactory = userRoleRepositoriesFactory;
        _identityRolesRepository = identityRolesRepository;

        Receive<AssignNodesToRoleMessage>(ProcessAssignNodesToRoleMessage);

        Receive<GetAllUserProfileRolesMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_userRoleRepositoriesFactory.NewUserRoleRepository(connection).GetAll());
        });

        ReceiveAsync<AddUserProfileRoleMessage>(async message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IUserRoleRepository userRoleRepository = _userRoleRepositoriesFactory.NewUserRoleRepository(connection);

            IdentityResult identityResult = await _identityRolesRepository.AddRole(message.UserProfileRole.Name);

            Sender.Tell(!identityResult.Succeeded
                ? new Tuple<UserRole, string>(null,
                    identityResult.Errors.Any(e => e.Code.Equals(DUPLICATED_NAME_ERROR))
                        ? DUPLICATED_NAME_ERROR
                        : identityResult.Errors.ToString())
                : new Tuple<UserRole, string>(userRoleRepository.GetById(userRoleRepository.Add(message.UserProfileRole)), string.Empty));
        });

        Receive<UpdateUserProfileRoleMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IUserRoleRepository userRoleRepository = _userRoleRepositoriesFactory.NewUserRoleRepository(connection);

            userRoleRepository.Update(message.UserProfileRole);

            Sender.Tell(userRoleRepository.GetByNetId(message.UserProfileRole.NetUid));
        });

        Receive<GetUserProfileRoleByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_userRoleRepositoriesFactory.NewUserRoleRepository(connection).GetByNetId(message.NetId));
        });

        ReceiveAsync<DeleteUserProfileRoleMessage>(async message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IUserRoleRepository roleRepository = _userRoleRepositoriesFactory.NewUserRoleRepository(connection);
            UserRole userRole = roleRepository.GetByNetId(message.NetId);

            roleRepository.Remove(message.NetId);

            await _identityRolesRepository.RemoveRoleByName(userRole.Name);
        });
    }

    private void ProcessAssignNodesToRoleMessage(AssignNodesToRoleMessage message) {
        if (message.UserRole == null || message.UserRole.IsNew()) {
            Sender.Tell(null);
            return;
        }

        if (!message.UserRole.DashboardNodes.Any()) {
            Sender.Tell(null);
            return;
        }

        using IDbConnection connection = _connectionFactory.NewSqlConnection();

        IUserRoleDashboardNodeRepository userRoleDashboardNodeRepository = _userRoleRepositoriesFactory.NewUserRoleDashboardNodeRepository(connection);
        IPermissionsRepository permissionsRepository = _userRoleRepositoriesFactory.NewPermissionsRepository(connection);

        permissionsRepository.RemoveRolePermissionByUserRoleId(message.UserRole.Id);
        userRoleDashboardNodeRepository.RemoveByUserRoleId(message.UserRole.Id);

        foreach (Permission permission in message.UserRole.Permissions)
            permissionsRepository.AddRolePermission(new RolePermission {
                UserRoleId = message.UserRole.Id,
                PermissionId = permission.Id
            });

        foreach (DashboardNode dashboardNode in message.UserRole.DashboardNodes)
            userRoleDashboardNodeRepository.Add(new UserRoleDashboardNode {
                UserRoleId = message.UserRole.Id,
                DashboardNodeId = dashboardNode.Id
            });

        Sender.Tell(message.UserRole);
    }
}