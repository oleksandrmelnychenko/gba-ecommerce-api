using System.Data;
using GBA.Domain.Repositories.UpdateDataCarriers.Contracts;

namespace GBA.Domain.Repositories.UpdateDataCarriers;

public class UpdateDataCarrierRepositoryFactory : IUpdateDataCarrierRepositoryFactory {
    public IUpdateDataCarrierRepository NewUpdateDataCarrierRepository(IDbConnection connection) {
        return new UpdateDataCarrierRepository(connection);
    }
}