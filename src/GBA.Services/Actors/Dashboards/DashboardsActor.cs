using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Dashboards;
using GBA.Domain.Repositories.Dashboards.Contracts;

namespace GBA.Services.Actors.Dashboards;

public sealed class DashboardsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDashboardRepositoriesFactory _dashboardRepositoriesFactory;

    public DashboardsActor(
        IDbConnectionFactory connectionFactory,
        IDashboardRepositoriesFactory dashboardRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _dashboardRepositoriesFactory = dashboardRepositoriesFactory;

        Receive<AddDashboardNodeMessage>(ProcessAddDashboardNodeMessage);

        Receive<AddDashboardNodeModuleMessage>(ProcessAddDashboardNodeModuleMessage);

        Receive<UpdateDashboardNodeMessage>(ProcessUpdateDashboardNodeMessage);

        Receive<UpdateDashboardNodeModuleMessage>(ProcessUpdateDashboardNodeModuleMessage);

        Receive<DeleteDashboardNodeMessage>(ProcessDeleteDashboardNodeMessage);

        Receive<DeleteDashboardNodeModuleMessage>(ProcessDeleteDashboardNodeModuleMessage);
    }

    private void ProcessAddDashboardNodeMessage(AddDashboardNodeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IDashboardNodeRepository dashboardNodeRepository = _dashboardRepositoriesFactory.NewDashboardNodeRepository(connection);

        message.DashboardNode.DashboardNodeModuleId = message.DashboardNode.DashboardNodeModule?.Id;
        message.DashboardNode.ParentDashboardNodeId = message.DashboardNode.ParentDashboardNode?.Id;

        message.DashboardNode.Id = dashboardNodeRepository.Add(message.DashboardNode);

        Sender.Tell(dashboardNodeRepository.GetById(message.DashboardNode.Id));
    }

    private void ProcessAddDashboardNodeModuleMessage(AddDashboardNodeModuleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IDashboardNodeModuleRepository dashboardNodeModuleRepository = _dashboardRepositoriesFactory.NewDashboardNodeModuleRepository(connection);

        message.DashboardNodeModule.Id = dashboardNodeModuleRepository.Add(message.DashboardNodeModule);

        Sender.Tell(dashboardNodeModuleRepository.GetById(message.DashboardNodeModule.Id));
    }

    private void ProcessUpdateDashboardNodeMessage(UpdateDashboardNodeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IDashboardNodeRepository dashboardNodeRepository = _dashboardRepositoriesFactory.NewDashboardNodeRepository(connection);

        if (!message.DashboardNode.DashboardNodeModuleId.HasValue)
            message.DashboardNode.DashboardNodeModuleId = message.DashboardNode.DashboardNodeModule?.Id;
        if (!message.DashboardNode.ParentDashboardNodeId.HasValue)
            message.DashboardNode.ParentDashboardNodeId = message.DashboardNode.ParentDashboardNode?.Id;

        dashboardNodeRepository.Update(message.DashboardNode);

        Sender.Tell(dashboardNodeRepository.GetByNetId(message.DashboardNode.NetUid));
    }

    private void ProcessUpdateDashboardNodeModuleMessage(UpdateDashboardNodeModuleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IDashboardNodeModuleRepository dashboardNodeModuleRepository = _dashboardRepositoriesFactory.NewDashboardNodeModuleRepository(connection);

        dashboardNodeModuleRepository.Update(message.DashboardNodeModule);

        Sender.Tell(dashboardNodeModuleRepository.GetByNetId(message.DashboardNodeModule.NetUid));
    }

    private void ProcessDeleteDashboardNodeMessage(DeleteDashboardNodeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _dashboardRepositoriesFactory.NewDashboardNodeRepository(connection).Remove(message.NetId);
    }

    private void ProcessDeleteDashboardNodeModuleMessage(DeleteDashboardNodeModuleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _dashboardRepositoriesFactory.NewDashboardNodeModuleRepository(connection).Remove(message.NetId);
    }
}