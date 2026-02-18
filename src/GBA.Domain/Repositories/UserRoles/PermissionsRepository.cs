using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Repositories.UserRoles.Contracts;

namespace GBA.Domain.Repositories.UserRoles;

public sealed class PermissionsRepository : IPermissionsRepository {
    private readonly IDbConnection _connection;

    public PermissionsRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(Permission permission) {
        return _connection.Query<long>(
            "INSERT INTO [Permission] ([ControlId], [Name], [ImageUrl], [Description], [DashboardNodeID], Updated) " +
            "VALUES (@ControlId, @Name, @ImageUrl, @Description, @DashboardNodeID, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY() ",
            permission
        ).FirstOrDefault();
    }

    public long AddRolePermission(RolePermission rolePermission) {
        return _connection.Query<long>(
            "INSERT INTO [RolePermission] ([UserRoleID], [PermissionID], Updated) " +
            "VALUES (@UserRoleId, @PermissionId, GETUTCDATE());" +
            "SELECT SCOPE_IDENTITY() ",
            rolePermission).FirstOrDefault();
    }

    public void RemoveRolePermissionByUserRoleId(long id) {
        _connection.Execute("DELETE FROM [RolePermission] WHERE UserRoleID = @Id ", new { Id = id });
    }

    public Permission GetById(long id) {
        return _connection.Query<Permission>(
            "SELECT * FROM [Permission] " +
            "WHERE [Permission].Deleted = 0 " +
            "AND [Permission].ID = @Id ",
            new { Id = id }
        ).FirstOrDefault();
    }


    public void Update(Permission permission) {
        _connection.Execute(
            "UPDATE [Permission] SET [ControlId] = @ControlId, [Name] = @Name, [ImageUrl] = @ImageUrl, " +
            "[Description] = @Description, [DashboardNodeID] = @DashboardNodeId, [Updated] = GETUTCDATE(), [Deleted] = @Deleted " +
            "WHERE ID = @Id ",
            permission);
    }


    public IEnumerable<Permission> GetPermissionsByDashboardNodeId(long id) {
        return _connection.Query<Permission>(
            "SELECT * FROM [Permission] " +
            "WHERE [Permission].Deleted = 0 " +
            "AND [Permission].DashboardNodeID = @Id ",
            new { Id = id });
    }

    public IEnumerable<Permission> GetPermissionsByUserRoleId(long id) {
        throw new NotImplementedException();
    }
}