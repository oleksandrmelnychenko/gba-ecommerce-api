using System.Data;
using GBA.Domain.Repositories.ServicePayers.Contracts;

namespace GBA.Domain.Repositories.ServicePayers;

public sealed class ServicePayerRepositoryFactory : IServicePayerRepositoryFactory {
    public IServicePayerRepository New(IDbConnection connection) {
        return new ServicePayerRepository(connection);
    }
}