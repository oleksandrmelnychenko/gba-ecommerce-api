using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Dashboards;
using GBA.Domain.Repositories.UserRoles.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.UserRoles;

public sealed class UserRoleRepository : IUserRoleRepository {
    private readonly IDbConnection _connection;

    public UserRoleRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(UserRole userRole) {
        return _connection.Query<long>(
                "INSERT INTO [UserRole] (Name, Dashboard, UserRoleType, Updated) " +
                "VALUES (@Name, @Dashboard, @UserRoleType, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                userRole
            )
            .Single();
    }

    public void Update(UserRole userRole) {
        _connection.Execute(
            "UPDATE [UserRole] SET " +
            "Name = @Name, Dashboard = @Dashboard, UserRoleType = @UserRoleType, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            userRole
        );
    }

    public UserRole GetById(long id) {
        return _connection.Query<UserRole, UserRoleTranslation, UserRole>(
                "SELECT * FROM [UserRole] " +
                "LEFT OUTER JOIN UserRoleTranslation " +
                "ON [UserRole].ID = UserRoleTranslation.UserRoleID " +
                "AND UserRoleTranslation.CultureCode = @Culture " +
                "AND UserRoleTranslation.Deleted = 0 " +
                "WHERE [UserRole].ID = @Id",
                (role, translation) => {
                    if (translation != null) role.Name = translation.Name;

                    return role;
                },
                new {
                    Id = id,
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            )
            .SingleOrDefault();
    }

    public UserRole GetByNetId(Guid netId) {
        return _connection.Query<UserRole, UserRoleTranslation, UserRole>(
                "SELECT * FROM [UserRole] " +
                "LEFT OUTER JOIN UserRoleTranslation " +
                "ON [UserRole].ID = UserRoleTranslation.UserRoleID " +
                "AND UserRoleTranslation.CultureCode = @Culture " +
                "AND UserRoleTranslation.Deleted = 0 " +
                "WHERE [UserRole].NetUID = @NetId",
                (role, translation) => {
                    if (translation != null) role.Name = translation.Name;

                    return role;
                },
                new { NetId = netId.ToString(), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }

    public UserRole GetByNetIdWithoutTranslation(Guid netId) {
        return _connection.Query<UserRole>(
                "SELECT * FROM [UserRole] " +
                "WHERE [UserRole].NetUID = @NetId",
                new { NetId = netId.ToString() }
            )
            .SingleOrDefault();
    }

    public List<UserRole> GetAll() {
        List<UserRole> toReturn = new();

        _connection.Query<UserRole, UserRoleTranslation, Permission, DashboardNode, UserRole>(
            "SELECT " +
            "[UserRole].* " +
            ", [UserRoleTranslation].* " +
            ", [Permission].* " +
            ", [DashboardNode].* " +
            "FROM [UserRole] " +
            "LEFT OUTER JOIN [UserRoleTranslation] " +
            "ON [UserRole].ID = [UserRoleTranslation].UserRoleID " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "LEFT JOIN [RolePermission] " +
            "ON [RolePermission].UserRoleID = [UserRole].ID " +
            "LEFT JOIN [Permission] " +
            "ON [Permission].ID = [RolePermission].PermissionID " +
            "AND [Permission].Deleted = 0 " +
            "LEFT JOIN [UserRoleDashboardNode] " +
            "ON [UserRoleDashboardNode].[UserRoleID] = [UserRole].ID " +
            "LEFT JOIN [DashboardNode] " +
            "ON [DashboardNode].ID = [UserRoleDashboardNode].DashboardNodeID " +
            "AND [DashboardNode].Deleted = 0 " +
            "WHERE [UserRole].Deleted = 0 ",
            (role, translation, permission, dashboardNode) => {
                if (!toReturn.Any(e => e.Id.Equals(role.Id))) {
                    if (translation != null) role.Name = translation.Name;

                    if (permission != null) role.Permissions.Add(permission);

                    if (dashboardNode != null) role.DashboardNodes.Add(dashboardNode);

                    toReturn.Add(role);
                } else {
                    UserRole userRole = toReturn.First(e => e.Id.Equals(role.Id));

                    if (permission != null && !userRole.Permissions.Any(e => e.Id.Equals(permission.Id))) userRole.Permissions.Add(permission);

                    if (dashboardNode != null && !userRole.DashboardNodes.Any(e => e.Id.Equals(dashboardNode.Id))) userRole.DashboardNodes.Add(dashboardNode);
                }

                return role;
            },
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [UserRole] SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }
}