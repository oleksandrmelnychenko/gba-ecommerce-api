using System.Data;

namespace GBA.Domain.Repositories.Dashboards.Contracts;

public interface IDashboardRepositoriesFactory {
    IDashboardNodeRepository NewDashboardNodeRepository(IDbConnection connection);

    IDashboardNodeModuleRepository NewDashboardNodeModuleRepository(IDbConnection connection);
}