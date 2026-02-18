using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Regions;
using GBA.Domain.EntityHelpers.ClientPredictionsDtos;
using GBA.Domain.EntityHelpers.ProductForecastApi;
using GBA.Domain.Messages.Products;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Regions.Contracts;
using Newtonsoft.Json;

// using machine.learning.concord.Common;

namespace GBA.Services.Actors.Products;

public sealed class ProductMostPurchasedActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IProductOneCRepositoriesFactory _productOneCRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IRegionRepositoriesFactory _regionRepositoriesFactory;

    public ProductMostPurchasedActor(
        IDbConnectionFactory connectionFactory,
        IRegionRepositoriesFactory regionRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IProductOneCRepositoriesFactory productOneCRepositoriesFactory,
        IHttpClientFactory httpClientFactory) {
        _connectionFactory = connectionFactory;
        _regionRepositoriesFactory = regionRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _productOneCRepositoriesFactory = productOneCRepositoriesFactory;
        _httpClientFactory = httpClientFactory;

        Receive<GetProductMostPurchasedMessage>(ProcessGetProductMostPurchasedMessage);

        ReceiveAsync<GetProductRecommendationsMessage>(ProcessGetProductRecommendationsMessageAsync);

        ReceiveAsync<GetProductForecastMessage>(ProcessGetProductForecastMessageAsync);
    }

    private async Task ProcessGetProductForecastMessageAsync(GetProductForecastMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            IClientRepository clientRepository = _clientRepositoriesFactory.NewClientRepository(connection);
            IGetSingleProductRepository productRepository = _productRepositoriesFactory.NewGetSingleProductRepository(connection);

            Product product = productRepository.GetByNetIdWithoutIncludes(message.ProductNetId);

            if (product == null) {
                Sender.Tell(new List<Product>());
                return;
            }

            string forecastApi = ConfigurationManager.RecommendationsURL + ConfigurationManager.SalesForecastEndpoint +
                                 $"/{product.Id}?as_of_date={message.AsOfDate.ToString("yyyy-MM-dd")}&forecast_weeks=12&use_cache=true";

            ForecastResponse forecastResponse = await DoGetRequestWithMappedResponseAsync<ForecastResponse>(forecastApi);

            Sender.Tell(forecastResponse);
        } catch (Exception exc) {
            LogError(exc);
            Sender.Tell(exc);
        }
    }

    private async Task ProcessGetProductRecommendationsMessageAsync(GetProductRecommendationsMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Guid clientNetId = message.ClientNetId;

            IClientRepository clientRepository = _clientRepositoriesFactory.NewClientRepository(connection);
            Client client = clientRepository.GetByNetIdWithoutIncludes(clientNetId);

            if (client == null) {
                Sender.Tell(new List<Product>());
                return;
            }

            string recommendationsApi = ConfigurationManager.RecommendationsURL + ConfigurationManager.RecommendationsEndpoint + $"/{client.Id}";

            ProductRecommendation recommendation = await DoGetRequestWithMappedResponseAsync<ProductRecommendation>(recommendationsApi);

            // if (clientRecommendation.recommendations == null || !clientRecommendation.recommendations.Any()) {
            //     Sender.Tell(new List<Product>());
            //     return;
            // }

            List<long> mockIds = new() {
                25360811,
                25231889,
                25304431,
                25343203,
                25347071,
                25318154,
                25317907,
                25311282,
                25309344,
                25377484,
                25405775,
                25335654,
                25312107,
                25422404,
                25068728,
                25394688,
                25166435,
                25230842,
                25100737,
                25415154
            };

            IGetMultipleProductsRepository getMultipleProductsRepository = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection);
            // List<Product> products = getMultipleProductsRepository.GetAllByIds(clientRecommendation.recommendations.Select(r => r.product_id).ToList());
            // List<Product> products = getMultipleProductsRepository.GetAllByIds(mockIds);

            Sender.Tell(null);
        } catch (Exception exc) {
            LogError(exc);
            Sender.Tell(exc);
        }
    }

    private async Task<T> DoGetRequestWithMappedResponseAsync<T>(string url) where T : new() {
        try {
            HttpClient httpClient = _httpClientFactory.CreateClient();

            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            T dto = JsonConvert.DeserializeObject<T>(json);

            return dto;
        } catch (Exception ex) {
            LogError(ex);
            return new T();
        }
    }

    private static void LogError(Exception ex) {
        string path = Path.Combine(NoltFolderManager.GetLogFolderPath(), "sync_error_log.txt");

        File.AppendAllText(
            path,
            $"{DateTime.UtcNow:dd.MM.yyyy HH:mm}\r\n{ex.Message}\r\n{ex.InnerException?.Message ?? string.Empty}\r\n\r\n"
        );
    }

    private void ProcessGetProductMostPurchasedMessage(GetProductMostPurchasedMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Guid clientNetId = message.ClientNetId;

            Client client = _clientRepositoriesFactory.NewClientRepository(connection).GetByNetIdWithoutIncludes(clientNetId);

            if (client == null || string.IsNullOrEmpty(client.RefId)) {
                Sender.Tell(new List<Product>());
                return;
            }

            using IDbConnection oneCConnection = _connectionFactory.NewFenixOneCSqlConnection();
            IProductOneCRepository productOneCRepository =
                _productOneCRepositoriesFactory.NewProductOneCRepository(oneCConnection);

            IEnumerable<long> productOldECommerceIds;

            if (message.ByRegion) {
                long? regionId = client.RegionId;

                if (!regionId.HasValue) {
                    Sender.Tell(new List<Product>());
                    return;
                }

                Region region = _regionRepositoriesFactory
                    .NewRegionRepository(connection)
                    .GetById(regionId.Value);

                if (region == null || region.Deleted || string.IsNullOrEmpty(region.Name)) {
                    Sender.Tell(new List<Product>());
                    return;
                }

                productOldECommerceIds = productOneCRepository.GetMostPurchasedByRegionName(region.Name);
            } else {
                productOldECommerceIds = productOneCRepository.GetMostPurchasedByClientRefId(client.RefId);
            }

            if (!productOldECommerceIds.Any()) {
                Sender.Tell(new List<Product>());
                return;
            }

            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

            ClientAgreement nonVatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(clientNetId, false);
            ClientAgreement vatAgreement = clientAgreementRepository.GetActiveByRootClientNetId(clientNetId, true);

            IGetMultipleProductsRepository getMultipleProductsRepository = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection);

            Sender.Tell(getMultipleProductsRepository
                .GetAllByOldECommerceIds(
                    productOldECommerceIds,
                    nonVatAgreement?.NetUid ?? Guid.Empty,
                    vatAgreement?.NetUid
                )
            );
        } catch (Exception exc) {
            // MachineLearningLogger.LogProductRecommendationsException(exc.Message);
            Sender.Tell(exc);
        }
    }
}