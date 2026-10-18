using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GBA.Common.ResourceNames.ECommerce;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Services.Services.Products.Contracts;

namespace GBA.Services.Services.Products;

public sealed class CarBrandService : ICarBrandService {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;

    public CarBrandService(
        IDbConnectionFactory connectionFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
    }

    public Task<IEnumerable<CarBrand>> GetAllCarBrands() {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        return Task.FromResult(
            _productRepositoriesFactory
                .NewCarBrandRepository(connection)
                .GetAllCarBrands()
        );
    }

    public Task<List<Product>> GetAllProductsFilteredByCarBrand(Guid carBrandNetId, Guid currentClientNetId, long limit, long offset) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IGetMultipleProductsRepository getMultipleProductsRepository = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection);

        if (currentClientNetId.Equals(Guid.Empty))
            return Task.FromResult(getMultipleProductsRepository.GetAllProductsByCarBrandNetId(carBrandNetId, Guid.Empty, null, limit, offset));

        IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

        ClientAgreement nonVatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(currentClientNetId, false);
        ClientAgreement vatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(currentClientNetId, true);

        return Task.FromResult(
            getMultipleProductsRepository
                .GetAllProductsByCarBrandNetId(
                    carBrandNetId,
                    nonVatAgreement?.NetUid ?? Guid.Empty,
                    vatAgreement?.NetUid,
                    limit,
                    offset
                )
        );
    }

    public Task<List<Product>> GetAllProductsFilteredByCarBrand(string carBrandAlias, Guid currentClientNetId, long limit, long offset) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        CarBrand brand = _productRepositoriesFactory.NewCarBrandRepository(connection).GetByAliasIfExists(carBrandAlias);

        if (brand == null) throw new Exception(CarBrandsResourceNames.CAR_BRAND_ALIAS_NOT_EXISTS);

        IGetMultipleProductsRepository getMultipleProductsRepository = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection);

        if (currentClientNetId.Equals(Guid.Empty))
            return Task.FromResult(getMultipleProductsRepository.GetAllProductsByCarBrandNetId(carBrandAlias, Guid.Empty, Guid.Empty, limit, offset));

        IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

        ClientAgreement nonVatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(currentClientNetId, false);
        ClientAgreement vatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(currentClientNetId, true);

        return Task.FromResult(
            getMultipleProductsRepository
                .GetAllProductsByCarBrandNetId(
                    carBrandAlias,
                    nonVatAgreement?.NetUid ?? Guid.Empty,
                    vatAgreement?.NetUid,
                    limit,
                    offset
                )
        );
    }
}
