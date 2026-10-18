using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GBA.Common.Configuration;
using GBA.Common.Helpers;
using GBA.Common.Models;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Services.Services.Orders.Contracts;
using Microsoft.Extensions.Logging;

namespace GBA.Services.Services.Orders;

public sealed class PreOrderService : IPreOrderService {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PreOrderService> _logger;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;

    public PreOrderService(
        IDbConnectionFactory connectionFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<PreOrderService> logger
    ) {
        _connectionFactory = connectionFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _saleRepositoriesFactory = saleRepositoriesFactory;

        _clientRepositoriesFactory = clientRepositoriesFactory;
    }

    public async Task<PreOrder> AddNewPreOrder(PreOrder preOrder, Guid clientNetId) {
        if (preOrder == null) throw new Exception("Entity can not be null");

        if (preOrder.Product == null || preOrder.Product.IsNew())
            throw new Exception("Product need to be specified");

        PreOrder committedPreOrder;
        using (IDbConnection connection = _connectionFactory.NewSqlConnection()) {
            if (!clientNetId.Equals(Guid.Empty)) {
                preOrder.ClientId =
                    _clientRepositoriesFactory
                        .NewClientRepository(connection)
                        .GetByNetIdWithoutIncludes(clientNetId)
                        ?.Id;
            } else {
                if (!string.IsNullOrEmpty(preOrder.MobileNumber))
                    preOrder.ClientId =
                        _clientRepositoriesFactory
                            .NewClientRepository(connection)
                            .SearchClientByMobileNumber(preOrder.MobileNumber)
                            ?.Id;
                else
                    preOrder.ClientId = null;
            }

            preOrder.Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            preOrder.ProductId = preOrder.Product.Id;

            IPreOrderRepository preOrderRepository = _saleRepositoriesFactory.NewPreOrderRepository(connection);

            preOrder.Id = preOrderRepository.Add(preOrder);
            committedPreOrder = preOrderRepository.GetById(preOrder.Id);
        }

        await NotifyCrmAboutCommittedPreOrder(committedPreOrder);
        return committedPreOrder;
    }

    private async Task NotifyCrmAboutCommittedPreOrder(PreOrder preOrder) {
        if (preOrder == null || preOrder.NetUid == Guid.Empty) {
            _logger.LogWarning(
                "Skipping CRM pre-order notification because the committed pre-order identity is missing.");
            return;
        }

        try {
            string configPath = NoltFolderManager.GetEcommerceCrmConfigJsonFilePath();
            if (!File.Exists(configPath)) {
                _logger.LogWarning(
                    "Skipping CRM pre-order notification because the CRM configuration file is missing.");
                return;
            }

            EcommerceCrmConfig data = JsonSerializer.Deserialize<EcommerceCrmConfig>(
                await File.ReadAllTextAsync(configPath)) ?? new EcommerceCrmConfig();
#if DEBUG
            string crmServerUrl = data.CrmServerUrl;
#else
            string crmServerUrl = data.CrmServerUrlRelease;
#endif
            if (string.IsNullOrWhiteSpace(crmServerUrl)) {
                _logger.LogWarning(
                    "Skipping CRM pre-order notification because the CRM server URL is empty.");
                return;
            }

            string notificationUrl =
                $"{crmServerUrl.TrimEnd('/')}/api/v1/{CultureInfo.CurrentCulture}/preorders/sync/new?netId={preOrder.NetUid:D}";
            using HttpClient httpClient = _httpClientFactory.CreateClient(
                EcommerceInternalHttpClientDefaults.ClientName);
            using HttpResponseMessage response = await httpClient.PostAsync(
                notificationUrl,
                null);

            if (!response.IsSuccessStatusCode) {
                _logger.LogWarning(
                    "CRM rejected pre-order notification {PreOrderNetUid} with status {StatusCode}.",
                    preOrder.NetUid,
                    (int)response.StatusCode);
            }
        } catch (Exception exception) {
            _logger.LogWarning(
                exception,
                "CRM pre-order notification failed for {PreOrderNetUid}; the committed pre-order remains valid.",
                preOrder.NetUid);
        }
    }
}
