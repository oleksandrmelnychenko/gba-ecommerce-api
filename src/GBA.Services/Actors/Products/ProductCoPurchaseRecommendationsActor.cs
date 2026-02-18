using System.Collections.Generic;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Products;
using GBA.Domain.Messages.Products;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Regions.Contracts;

// using machine.learning.concord.Common;
// using machine.learning.concord.CoPurchaseRecommendations;
// using machine.learning.concord.CoPurchaseRecommendations.Contracts;

namespace GBA.Services.Actors.Products;

public sealed class ProductCoPurchaseRecommendationsActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;

    private readonly IRegionRepositoriesFactory _regionRepositoriesFactory;
    // private readonly IRecommendationBuilderManager _recommendationBuilderManager;

    public ProductCoPurchaseRecommendationsActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IRegionRepositoriesFactory regionRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory
        // IRecommendationBuilderManager recommendationBuilderManager
    ) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _regionRepositoriesFactory = regionRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        // _recommendationBuilderManager = recommendationBuilderManager;

        Receive<GetProductCoPurchaseRecommendationsMessage>(ProcessGetProductCoPurchaseRecommendationsMessage);
    }

    private void ProcessGetProductCoPurchaseRecommendationsMessage(GetProductCoPurchaseRecommendationsMessage message) {
        Sender.Tell(new List<Product>());
        // try {
        //     using IDbConnection connection = _connectionFactory.NewSqlConnection();
        //     Guid clientNetId = message.ClientNetId;
        //
        //     Client client = _clientRepositoriesFactory
        //                     .NewClientRepository(connection)
        //                     .GetByNetIdWithoutIncludes(clientNetId);
        //
        //     if (client == null || string.IsNullOrEmpty(client.RefId)) {
        //         Sender.Tell(new List<Product>());
        //         return;
        //     }
        //
        //     IProductRepository productRepository = _productRepositoriesFactory.NewProductRepository(connection);
        //     Product product = productRepository.GetByNetIdWithoutIncludes(message.ProductNetId);
        //
        //     if (product == null || product.SourceAmgCode <= 0) {
        //         Sender.Tell(new List<Product>());
        //         return;
        //     }
        //
        //     string refId = client.RefId;
        //
        //     if (message.ByRegion) {
        //         long? regionId = client.RegionId;
        //
        //         if (!regionId.HasValue) {
        //             Sender.Tell(new List<Product>());
        //             return;
        //         }
        //
        //         IRegionRepository regionRepository = _regionRepositoriesFactory.NewRegionRepository(connection);
        //         Region region = regionRepository.GetById(regionId.Value);
        //
        //         if (region == null || region.Deleted || string.IsNullOrEmpty(region.Name)) {
        //             Sender.Tell(new List<Product>());
        //             return;
        //         }
        //
        //         refId = region.Name.Trim();
        //     }
        //
        //     IEnumerable<long> productOldECommerceIds =
        //         _recommendationBuilderManager
        //             .GetCoPurchaseProductsByProductOldEcommerceIdAndRefId(
        //                 string.Format(
        //                     RecommendationBuilderConstants.OLD_ECOMMERCE_ID_ONE_C_FORMAT,
        //                     product.SourceAmgCode
        //                 ),
        //                 refId
        //             );
        //
        //     if (!productOldECommerceIds.Any()) {
        //         Sender.Tell(new List<Product>());
        //         return;
        //     }
        //
        //     IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
        //
        //     ClientAgreement nonVatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(clientNetId, false);
        //     ClientAgreement vatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(clientNetId, true);
        //
        //     Sender.Tell(productRepository
        //         .GetAllByOldECommerceIds(
        //             productOldECommerceIds,
        //             nonVatAgreement?.NetUid ?? Guid.Empty,
        //             vatAgreement?.NetUid
        //         )
        //     );
        // } catch (Exception exc) {
        //     MachineLearningLogger.LogProductRecommendationsException(exc.Message);
        //     Sender.Tell(exc);
        // }
    }
}