using System.Data;

namespace GBA.Domain.Repositories.ServicePayers.Contracts;

public interface IServicePayerRepositoryFactory {
    IServicePayerRepository New(IDbConnection connection);
}