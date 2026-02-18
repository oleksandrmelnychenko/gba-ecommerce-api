using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Ecommerce;
using GBA.Domain.Repositories.Ecommerce.Contracts;
using GBA.Services.Services.EcommerceRegions.Contracts;

namespace GBA.Services.Services.EcommerceRegions;

public sealed class EcommerceRegionService : IEcommerceRegionService {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IEcommerceAdminPanelRepositoriesFactory _ecommerceRepositoriesFactory;

    public EcommerceRegionService(IDbConnectionFactory connectionFactory,
        IEcommerceAdminPanelRepositoriesFactory ecommerceRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _ecommerceRepositoriesFactory = ecommerceRepositoriesFactory;
    }

    public Task<IEnumerable<EcommerceRegion>> GetAllLocale() {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        return Task.FromResult(
            _ecommerceRepositoriesFactory
                .NewEcommerceRegionRepository(connection)
                .GetAll()
        );
    }

    public Task<IEnumerable<EcommerceRegion>> Update(EcommerceRegion ecommerceRegion) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IEcommerceRegionRepository ecommerceRegionRepository = _ecommerceRepositoriesFactory.NewEcommerceRegionRepository(connection);

        if (ecommerceRegion.IsNew())
            ecommerceRegionRepository.Add(ecommerceRegion);
        else
            ecommerceRegionRepository.Update(ecommerceRegion);

        return Task.FromResult(ecommerceRegionRepository.GetAll());
    }
}
