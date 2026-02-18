using System.Data;

namespace GBA.Domain.Repositories.UpdateDataCarriers.Contracts;

public interface IUpdateDataCarrierRepositoryFactory {
    IUpdateDataCarrierRepository NewUpdateDataCarrierRepository(IDbConnection connection);
}