using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Dashboards;
using GBA.Domain.Messages.Dashboards;
using GBA.Domain.Repositories.Dashboards.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Dashboards.DashboardsGetActors;

public sealed class BaseDashboardsGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDashboardRepositoriesFactory _dashboardRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public BaseDashboardsGetActor(
        IDbConnectionFactory connectionFactory,
        IDashboardRepositoriesFactory dashboardRepositoriesFactory,
        IUserRepositoriesFactory userRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _dashboardRepositoriesFactory = dashboardRepositoriesFactory;
        _userRepositoriesFactory = userRepositoriesFactory;

        Receive<GetAllDashboardNodeModulesMessage>(ProcessGetAllDashboardNodeModulesMessage);
        Receive<GetAllDashboardNodeModulesByUserRoleMessage>(ProcessGetAllDashboardNodeModulesByUserRoleMessage);
    }

    private void ProcessGetAllDashboardNodeModulesByUserRoleMessage(GetAllDashboardNodeModulesByUserRoleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IDashboardNodeRepository dashboardNodeRepository = _dashboardRepositoriesFactory.NewDashboardNodeRepository(connection);
        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);

        User user = userRepository.GetByNetId(message.UserNetId);

        if (user.UserRole == null) {
            Sender.Tell(null); //Handle failed message
            return;
        }

        List<DashboardNodeModule> toReturn =
            _dashboardRepositoriesFactory.NewDashboardNodeModuleRepository(connection).GetAllByUserRoleId(user.UserRole.Id);

        foreach (DashboardNodeModule module in toReturn.Where(m => m.Children.Any())) LoadChildren(module.Children, dashboardNodeRepository);

        Sender.Tell(toReturn);
    }

    private void ProcessGetAllDashboardNodeModulesMessage(GetAllDashboardNodeModulesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IDashboardNodeRepository dashboardNodeRepository = _dashboardRepositoriesFactory.NewDashboardNodeRepository(connection);

        List<DashboardNodeModule> toReturn = _dashboardRepositoriesFactory.NewDashboardNodeModuleRepository(connection).GetAll();

        foreach (DashboardNodeModule module in toReturn.Where(m => m.Children.Any())) LoadChildren(module.Children, dashboardNodeRepository);

        Sender.Tell(toReturn);
    }

    private static void LoadChildren(IEnumerable<DashboardNode> children, IDashboardNodeRepository dashboardNodeRepository) {
        foreach (DashboardNode child in children) {
            child.Children = dashboardNodeRepository.GetAllChilds(child.Id);

            if (child.Children.Any()) LoadChildren(child.Children, dashboardNodeRepository);
        }
    }
}