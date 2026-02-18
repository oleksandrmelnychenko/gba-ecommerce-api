using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Services.Services.Recommendations.Contracts;

namespace GBA.Services.Services.Recommendations;

public sealed class ProductMostPurchasedService : IProductMostPurchasedService {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;

    private readonly IDbConnectionFactory _connectionFactory;

    private readonly IProductOneCRepositoriesFactory _productOneCRepositoriesFactory;

    private readonly IProductRepositoriesFactory _productRepositoriesFactory;

    public ProductMostPurchasedService(
        IProductRepositoriesFactory productRepositoriesFactory,
        IProductOneCRepositoriesFactory productOneCRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IDbConnectionFactory connectionFactory
    ) {
        _productRepositoriesFactory = productRepositoriesFactory;

        _productOneCRepositoriesFactory = productOneCRepositoriesFactory;

        _clientRepositoriesFactory = clientRepositoriesFactory;

        _connectionFactory = connectionFactory;
    }

    public Task<List<FromSearchProduct>> GetMostPurchasedProductsByClientNetId(Guid clientNetId) {
        return Task.Run(() => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Client client = _clientRepositoriesFactory.NewClientRepository(connection).GetByNetIdWithoutIncludes(clientNetId);

            if (client == null || string.IsNullOrEmpty(client.RefId))
                return new List<FromSearchProduct>();

            using IDbConnection oneCConnection = _connectionFactory.NewFenixOneCSqlConnection();
            IProductOneCRepository productOneCRepository = _productOneCRepositoriesFactory
                .NewProductOneCRepository(oneCConnection);

            IEnumerable<long> productOldEcommerceIds = productOneCRepository.GetMostPurchasedByClientRefId(client.RefId);

            if (!productOldEcommerceIds.Any())
                return new List<FromSearchProduct>();

            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

            ClientAgreement nonVatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(clientNetId, false);
            ClientAgreement vatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(clientNetId, true);

            IGetMultipleProductsRepository getSingleProductRepository = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection);

            return getSingleProductRepository
                .GetProductsByOldECommerceIds(
                    productOldEcommerceIds,
                    nonVatAgreement?.NetUid ?? Guid.Empty,
                    vatAgreement?.NetUid
                );
        });
    }
}