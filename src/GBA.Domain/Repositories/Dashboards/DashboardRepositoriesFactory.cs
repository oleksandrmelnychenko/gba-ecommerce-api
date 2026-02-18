using System.Data;
using GBA.Domain.Repositories.Dashboards.Contracts;

namespace GBA.Domain.Repositories.Dashboards;

public sealed class DashboardRepositoriesFactory : IDashboardRepositoriesFactory {
    public IDashboardNodeRepository NewDashboardNodeRepository(IDbConnection connection) {
        return new DashboardNodeRepository(connection);
    }

    public IDashboardNodeModuleRepository NewDashboardNodeModuleRepository(IDbConnection connection) {
        return new DashboardNodeModuleRepository(connection);
    }
}