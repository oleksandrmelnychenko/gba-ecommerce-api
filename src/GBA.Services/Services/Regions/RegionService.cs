using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Repositories.Regions.Contracts;
using GBA.Services.Services.Regions.Contracts;

namespace GBA.Services.Services.Regions;

public sealed class RegionService : IRegionService {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IRegionRepositoriesFactory _regionRepositoriesFactory;

    public RegionService(
        IRegionRepositoriesFactory regionRepositoriesFactory,
        IDbConnectionFactory connectionFactory) {
        _regionRepositoriesFactory = regionRepositoriesFactory;

        _connectionFactory = connectionFactory;
    }

    public Task<List<Region>> GetAllRegions() {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        return Task.FromResult(_regionRepositoriesFactory.NewRegionRepository(connection).GetAll());
    }
}
