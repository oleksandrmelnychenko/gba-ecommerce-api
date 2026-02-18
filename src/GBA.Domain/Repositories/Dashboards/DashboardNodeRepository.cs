using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Dashboards;
using GBA.Domain.Repositories.Dashboards.Contracts;

namespace GBA.Domain.Repositories.Dashboards;

public sealed class DashboardNodeRepository : IDashboardNodeRepository {
    private readonly IDbConnection _connection;

    public DashboardNodeRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(DashboardNode node) {
        return _connection.Query<long>(
                "INSERT INTO [DashboardNode] (Language, Module, Route, CssClass, ParentDashboardNodeId, DashboardNodeModuleId, Updated) " +
                "VALUES (@Language, @Module, @Route, @CssClass, @ParentDashboardNodeId, @DashboardNodeModuleId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                node
            )
            .Single();
    }

    public void Update(DashboardNode node) {
        _connection.Execute(
            "UPDATE [DashboardNode] " +
            "SET Language = @Language, Module = @Module, Route = @Route, CssClass = @CssClass, ParentDashboardNodeId = @ParentDashboardNodeId, " +
            "DashboardNodeModuleId = @DashboardNodeModuleId, Updated = getutcdate() " +
            "WHERE [DashboardNode].NetUID = @NetUid",
            node
        );
    }

    public DashboardNode GetById(long id) {
        return _connection.Query<DashboardNode>(
                "SELECT * " +
                "FROM [DashboardNode] " +
                "WHERE [DashboardNode].ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public DashboardNode GetByNetId(Guid netId) {
        return _connection.Query<DashboardNode>(
                "SELECT * " +
                "FROM [DashboardNode] " +
                "WHERE [DashboardNode].NetUID = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public List<DashboardNode> GetAllChilds(long id) {
        return _connection.Query<DashboardNode>(
                "SELECT * " +
                "FROM [DashboardNode] " +
                "WHERE [DashboardNode].ParentDashboardNodeID = @Id",
                new { Id = id }
            )
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [DashboardNode] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [DashboardNode].NetUID = @NetId",
            new { NetId = netId }
        );
    }
}