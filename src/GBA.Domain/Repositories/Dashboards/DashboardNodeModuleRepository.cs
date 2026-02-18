using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Dashboards;
using GBA.Domain.Repositories.Dashboards.Contracts;

namespace GBA.Domain.Repositories.Dashboards;

public sealed class DashboardNodeModuleRepository : IDashboardNodeModuleRepository {
    private readonly IDbConnection _connection;

    public DashboardNodeModuleRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(DashboardNodeModule module) {
        return _connection.Query<long>(
                "INSERT INTO [DashboardNodeModule] (Language, Module, Description, CssClass, Updated) " +
                "VALUES (@Language, @Module, @Description, @CssClass, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                module
            )
            .Single();
    }

    public void Update(DashboardNodeModule module) {
        _connection.Execute(
            "UPDATE [DashboardNodeModule] " +
            "SET Language = @Language, Module = @Module, Description = @Description, CssClass = @CssClass, Updated = getutcdate() " +
            "WHERE [DashboardNodeModule].NetUID = @NetUid",
            module
        );
    }

    public DashboardNodeModule GetById(long id) {
        return _connection.Query<DashboardNodeModule>(
                "SELECT * " +
                "FROM [DashboardNodeModule] " +
                "WHERE [DashboardNodeModule].ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public DashboardNodeModule GetByNetId(Guid netId) {
        return _connection.Query<DashboardNodeModule>(
                "SELECT * " +
                "FROM [DashboardNodeModule] " +
                "WHERE [DashboardNodeModule].NetUID = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public List<DashboardNodeModule> GetAll() {
        List<DashboardNodeModule> toReturn = new();

        _connection.Query<DashboardNodeModule, DashboardNode, Permission, DashboardNodeModule>(
            "SELECT * " +
            "FROM [DashboardNodeModule] " +
            "LEFT JOIN [DashboardNode] " +
            "ON [DashboardNode].DashboardNodeModuleID = [DashboardNodeModule].ID " +
            "AND [DashboardNode].Deleted = 0 " +
            "LEFT JOIN [Permission] " +
            "ON [Permission].DashboardNodeID = [DashboardNode].ID " +
            "AND [Permission].Deleted = 0 " +
            "WHERE [DashboardNodeModule].Deleted = 0 " +
            "AND [DashboardNodeModule].Language = @Culture " +
            "AND ([DashboardNode].Language = @Culture OR [DashboardNode].Language IS NULL)",
            (module, node, permission) => {
                if (!toReturn.Any(m => m.Id.Equals(module.Id))) {
                    if (node != null) {
                        if (permission != null) node.Permissions.Add(permission);
                        module.Children.Add(node);
                    }

                    toReturn.Add(module);
                } else {
                    DashboardNodeModule dashboardNodeModule = toReturn.First(m => m.Id.Equals(module.Id));

                    if (permission == null) {
                        dashboardNodeModule.Children.Add(node);
                    } else {
                        if (dashboardNodeModule.Children.Any(n => n.Id.Equals(permission.DashboardNodeId))) {
                            dashboardNodeModule.Children.First(n => n.Id.Equals(permission.DashboardNodeId)).Permissions.Add(permission);
                        } else {
                            node.Permissions.Add(permission);
                            dashboardNodeModule.Children.Add(node);
                        }
                    }
                }

                return module;
            },
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public List<DashboardNodeModule> GetAllByUserRoleId(long id) {
        List<DashboardNodeModule> toReturn = new();

        _connection.Query<DashboardNodeModule, DashboardNode, DashboardNodeModule>(
            "SELECT " +
            "[DashboardNodeModule].* " +
            ", [DashboardNode].* " +
            "FROM [DashboardNodeModule] " +
            "LEFT JOIN [DashboardNode] " +
            "ON [DashboardNode].DashboardNodeModuleID = [DashboardNodeModule].ID " +
            "LEFT JOIN [UserRoleDashboardNode] " +
            "ON [UserRoleDashboardNode].DashboardNodeID = [DashboardNode].ID " +
            "LEFT JOIN [UserRole] " +
            "ON [UserRole].ID = [UserRoleDashboardNode].UserRoleID " +
            "WHERE [DashboardNodeModule].Deleted = 0 " +
            "AND [DashboardNode].Deleted = 0 " +
            "AND [DashboardNodeModule].Language = @Culture " +
            "AND ([DashboardNode].Language = @Culture OR [DashboardNode].Language IS NULL) " +
            "AND [UserRole].ID = @Id ",
            (module, node) => {
                if (!toReturn.Any(m => m.Id.Equals(module.Id))) {
                    if (node != null) module.Children.Add(node);

                    toReturn.Add(module);
                } else {
                    toReturn.First(m => m.Id.Equals(module.Id)).Children.Add(node);
                }

                return module;
            },
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Id = id }
        );

        return toReturn;
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [DashboardNodeModule] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [DashboardNodeModule].NetUID = @NetId",
            new { NetId = netId }
        );
    }
}