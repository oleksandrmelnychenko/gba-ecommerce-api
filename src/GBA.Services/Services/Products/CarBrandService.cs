using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GBA.Common.ResourceNames.ECommerce;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Services.Services.Products.Contracts;

namespace GBA.Services.Services.Products;

public sealed class CarBrandService : ICarBrandService {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;
    private readonly IPricingDependencyRevisionProvider _pricingDependencyRevisionProvider;
    private readonly IRetailCatalogSelectionProvider _retailCatalogSelectionProvider;

    public CarBrandService(
        IDbConnectionFactory connectionFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory,
        IPricingDependencyRevisionProvider pricingDependencyRevisionProvider,
        IRetailCatalogSelectionProvider retailCatalogSelectionProvider) {
        _connectionFactory = connectionFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _storageRepositoryFactory = storageRepositoryFactory;
        _pricingDependencyRevisionProvider = pricingDependencyRevisionProvider;
        _retailCatalogSelectionProvider = retailCatalogSelectionProvider;
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

        ProductPricingContextSet pricingContexts = ProductPricingContextResolver.ResolveSet(
            connection,
            _clientRepositoriesFactory,
            _storageRepositoryFactory,
            _pricingDependencyRevisionProvider,
            _retailCatalogSelectionProvider,
            currentClientNetId,
            withVat: false);

        if (pricingContexts == null) return Task.FromResult(new List<Product>());

        return Task.FromResult(
            getMultipleProductsRepository
                .GetAllProductsByCarBrandNetId(
                    carBrandNetId,
                    pricingContexts.NonVat?.Context.ClientAgreementNetId ?? Guid.Empty,
                    pricingContexts.Vat?.Context.ClientAgreementNetId,
                    pricingContexts.Selected.Context.OrganizationId,
                    pricingContexts.NonVat?.Context.Source ?? string.Empty,
                    pricingContexts.Vat?.Context.Source,
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

        ProductPricingContextSet pricingContexts = ProductPricingContextResolver.ResolveSet(
            connection,
            _clientRepositoriesFactory,
            _storageRepositoryFactory,
            _pricingDependencyRevisionProvider,
            _retailCatalogSelectionProvider,
            currentClientNetId,
            withVat: false);

        if (pricingContexts == null) return Task.FromResult(new List<Product>());

        return Task.FromResult(
            getMultipleProductsRepository
                .GetAllProductsByCarBrandNetId(
                    carBrandAlias,
                    pricingContexts.NonVat?.Context.ClientAgreementNetId ?? Guid.Empty,
                    pricingContexts.Vat?.Context.ClientAgreementNetId,
                    pricingContexts.Selected.Context.OrganizationId,
                    pricingContexts.NonVat?.Context.Source ?? string.Empty,
                    pricingContexts.Vat?.Context.Source,
                    limit,
                    offset
                )
        );
    }
}
