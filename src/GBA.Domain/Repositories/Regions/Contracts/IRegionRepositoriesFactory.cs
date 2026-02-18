using System.Data;

namespace GBA.Domain.Repositories.Regions.Contracts;

public interface IRegionRepositoriesFactory {
    IRegionRepository NewRegionRepository(IDbConnection connection);

    IRegionCodeRepository NewRegionCodeRepository(IDbConnection connection);
}