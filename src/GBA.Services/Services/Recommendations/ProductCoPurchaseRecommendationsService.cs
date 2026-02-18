using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Services.Services.Recommendations.Contracts;
// using machine.learning.concord.CoPurchaseRecommendations;
// using machine.learning.concord.CoPurchaseRecommendations.Contracts;

namespace GBA.Services.Services.Recommendations;

public sealed class ProductCoPurchaseRecommendationsService : IProductCoPurchaseRecommendationsService {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;

    private readonly IDbConnectionFactory _connectionFactory;

    // private readonly IRecommendationBuilderManager _recommendationBuilderManager;

    private readonly IProductRepositoriesFactory _productRepositoriesFactory;

    public ProductCoPurchaseRecommendationsService(
        // IRecommendationBuilderManager recommendationBuilderManager,
        IProductRepositoriesFactory productRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IDbConnectionFactory connectionFactory
    ) {
        // _recommendationBuilderManager = recommendationBuilderManager;

        _productRepositoriesFactory = productRepositoriesFactory;

        _clientRepositoriesFactory = clientRepositoriesFactory;

        _connectionFactory = connectionFactory;
    }

    public Task<List<FromSearchProduct>> GetCoPurchaseProductsByProductClientNetIds(
        Guid productNetId,
        Guid clientNetId
    ) {
        return Task.Run(() => {
            // using IDbConnection connection = _connectionFactory.NewSqlConnection();
            // Client client = _clientRepositoriesFactory.NewClientRepository(connection).GetByNetIdWithoutIncludes(clientNetId);
            //
            // if (client == null || string.IsNullOrEmpty(client.RefId))
            //     return new List<FromSearchProduct>();
            //
            // IProductRepository productRepository = _productRepositoriesFactory.NewProductRepository(connection);
            // Product product = productRepository.GetByNetIdWithoutIncludes(productNetId);
            //
            // if (product == null || product.SourceAmgCode <= 0)
            //     return new List<FromSearchProduct>();
            //
            // IEnumerable<long> productOldEcommerceIds =
            //     _recommendationBuilderManager
            //         .GetCoPurchaseProductsByProductOldEcommerceIdAndRefId(
            //             string.Format(
            //                 RecommendationBuilderConstants.OLD_ECOMMERCE_ID_ONE_C_FORMAT,
            //                 product.SourceAmgCode
            //             ),
            //             client.RefId
            //         );
            //
            // if (!productOldEcommerceIds.Any())
            //     return new List<FromSearchProduct>();
            //
            // IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
            //
            // ClientAgreement nonVatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(clientNetId, false);
            // ClientAgreement vatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(clientNetId, true);
            //
            // return productRepository
            //     .GetProductsByOldECommerceIds(
            //         productOldEcommerceIds,
            //         nonVatAgreement?.NetUid ?? Guid.Empty,
            //         vatAgreement?.NetUid
            //     );

            return new List<FromSearchProduct>();
        });
    }
}