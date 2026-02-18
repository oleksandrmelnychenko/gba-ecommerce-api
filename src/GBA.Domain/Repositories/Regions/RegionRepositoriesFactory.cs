using System.Data;
using GBA.Domain.Repositories.Regions.Contracts;

namespace GBA.Domain.Repositories.Regions;

public sealed class RegionRepositoriesFactory : IRegionRepositoriesFactory {
    public IRegionRepository NewRegionRepository(IDbConnection connection) {
        return new RegionRepository(connection);
    }

    public IRegionCodeRepository NewRegionCodeRepository(IDbConnection connection) {
        return new RegionCodeRepository(connection);
    }
}