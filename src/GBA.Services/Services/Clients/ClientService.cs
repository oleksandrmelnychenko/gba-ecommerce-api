using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Ecommerce;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Clients.RetailClients.Contracts;
using GBA.Domain.Repositories.Ecommerce.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Services.Services.Clients.Contracts;
using GBA.Services.Services.Orders.Contracts;
using NLog;

namespace GBA.Services.Services.Clients;

public sealed class ClientService : IClientService {
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IEcommerceAdminPanelRepositoriesFactory _ecommerceRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IOrderService _orderService;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IRetailClientRepositoriesFactory _retailClientRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;

    public ClientService(
        IClientRepositoriesFactory clientRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        IRetailClientRepositoriesFactory retailClientRepositoriesFactory,
        IEcommerceAdminPanelRepositoriesFactory ecommerceRepositoriesFactory,
        IDbConnectionFactory connectionFactory,
        IOrderService orderService,
        IProductRepositoriesFactory productRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory
    ) {
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _retailClientRepositoriesFactory = retailClientRepositoriesFactory;
        _ecommerceRepositoriesFactory = ecommerceRepositoriesFactory;
        _connectionFactory = connectionFactory;
        _orderService = orderService;
        _productRepositoriesFactory = productRepositoriesFactory;
        _storageRepositoryFactory = storageRepositoryFactory;
    }

    public Task<Client> GetByNetId(Guid netId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);

            Client client = _clientRepositoriesFactory.NewClientRepository(connection).GetByNetId(netId, true);

            if (client == null) {
                Workplace workplace = _clientRepositoriesFactory.NewWorkplaceRepository(connection).GetByNetIdWithClient(netId);
                client = workplace.MainClient;
                client.CurrentWorkplace = workplace;
            }

            foreach (ClientAgreement clientAgreement in client.ClientAgreements)
                clientAgreement.AccountBalance = _clientRepositoriesFactory.NewClientCashFlowRepository(connection)
                    .GetAccountBalanceByClientAgreement(
                        clientAgreement.Id,
                        clientAgreement.Agreement.Currency != null && clientAgreement.Agreement.Currency.Code.ToUpper().Equals("EUR"));

            dynamic data = _clientRepositoriesFactory.NewClientRepository(connection).GetDebtTotalsForClientStructureWithRootByClientNetId(netId, true);

            ClientAgreement selectedClientAgreement = client.ClientAgreements.FirstOrDefault(ca => ca.Agreement.IsSelected);

            ExchangeRate agreementExchangeRate = selectedClientAgreement == null
                ? exchangeRateRepository.GetByCurrencyCodeAndCurrentCulture(client.ClientAgreements.First().Agreement.Currency.Code)
                : exchangeRateRepository.GetByCurrencyCodeAndCurrentCulture(selectedClientAgreement.Agreement.Currency.Code);

            decimal totalLocalDebt = data.TotalLocal ?? 0m;

            client.AccountBalance = agreementExchangeRate != null
                ? decimal.Round(totalLocalDebt / agreementExchangeRate.Amount, 2, MidpointRounding.AwayFromZero)
                : decimal.Round(totalLocalDebt, 2, MidpointRounding.AwayFromZero);

            return Task.FromResult(client);
    }

    public Task<Client> GetRootClientBySubClientNerId(Guid netId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);

            Client client = _clientRepositoriesFactory.NewClientRepository(connection).GetRootClientBySubClientNetId(netId);

            dynamic data = _clientRepositoriesFactory.NewClientRepository(connection).GetDebtTotalsForClientStructureWithRootByClientNetId(netId, true);

            ClientAgreement clientAgreement = client.ClientAgreements.FirstOrDefault(ca => ca.Agreement.IsSelected);

            ExchangeRate agreementExchangeRate = clientAgreement == null
                ? exchangeRateRepository.GetByCurrencyCodeAndCurrentCulture(client.ClientAgreements.First().Agreement.Currency.Code)
                : exchangeRateRepository.GetByCurrencyCodeAndCurrentCulture(clientAgreement.Agreement.Currency.Code);

            decimal totalLocalDebt = data.TotalLocal ?? 0m;

            client.AccountBalance = agreementExchangeRate != null
                ? decimal.Round(totalLocalDebt / agreementExchangeRate.Amount, 2, MidpointRounding.AwayFromZero)
                : decimal.Round(totalLocalDebt, 2, MidpointRounding.AwayFromZero);

            return Task.FromResult(client);
    }

    public Task<RetailClient> GetRetailClientByNetId(Guid netId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            RetailClient client = _retailClientRepositoriesFactory.NewRetailClientRepository(connection).GetByNetId(netId);
            IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);

            Storage storage = storageRepository.GetWithHighestPriority();
            List<OrderItem> orderItems = JsonSerializer.Deserialize<List<OrderItem>>(client.ShoppingCartJson, _jsonSerializerOptions) ?? new List<OrderItem>();
            List<OrderItem> anyOrderItems = new();

            foreach (OrderItem orderItem in orderItems.Where(i => i.IsNew() && i.Qty > 0)) {
                ProductAvailability productAvailability =
                    productAvailabilityRepository.GetByProductAndStorageIds(orderItem.Product.Id, storage.Id);
                if (productAvailability != null && !productAvailability.Amount.Equals(0)) {
                    orderItem.IsMisplacedItem = true;
                    anyOrderItems.Add(orderItem);
                }
            }

            client.ShoppingCartJson = JsonSerializer.Serialize(anyOrderItems);

            return Task.FromResult(client);
    }

    public Task<(RetailClient, string)> GetRetailClientByNetIdCheckOrderItems(Guid netId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            RetailClient client = _retailClientRepositoriesFactory.NewRetailClientRepository(connection).GetByNetId(netId);
            IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);

            Storage storage = storageRepository.GetWithHighestPriority();
            List<OrderItem> orderItems = JsonSerializer.Deserialize<List<OrderItem>>(client.ShoppingCartJson, _jsonSerializerOptions) ?? new List<OrderItem>();
            List<OrderItem> anyOrderItems = new();
            List<OrderItem> notHaveOrderItems = new();
            string notHaveOrderItemsInfo = string.Empty;


            foreach (OrderItem orderItem in orderItems.Where(i => i.IsNew() && i.Qty > 0)) {
                ProductAvailability productAvailability =
                    productAvailabilityRepository.GetByProductAndStorageIds(orderItem.Product.Id, storage.Id);
                if (productAvailability != null && !productAvailability.Amount.Equals(0))
                    anyOrderItems.Add(orderItem);
                else
                    notHaveOrderItems.Add(orderItem);
            }

            foreach (OrderItem orderItem in notHaveOrderItems) notHaveOrderItemsInfo += $"������� {orderItem.Product.VendorCode} �������� �� �����;";

            client.ShoppingCartJson = JsonSerializer.Serialize(anyOrderItems);

            _retailClientRepositoriesFactory.NewRetailClientRepository(connection).Update(client);

            return Task.FromResult((client, notHaveOrderItemsInfo));
    }

    public async Task<RetailClient> AddRetailClient(RetailClient client) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            IRetailClientRepository retailClientRepository = _retailClientRepositoriesFactory.NewRetailClientRepository(connection);

            List<OrderItem> orderItems = JsonSerializer.Deserialize<List<OrderItem>>(client.ShoppingCartJson, _jsonSerializerOptions) ?? new List<OrderItem>();
            if (orderItems.Count == 0 || orderItems.Count > 100)
                throw new ArgumentException("Shopping cart must contain between 1 and 100 items.");

            if (client.EcommerceRegion == null || client.EcommerceRegion.NetUid == Guid.Empty)
                throw new ArgumentException("A valid ecommerce region is required.");

            EcommerceRegion ecommerceRegion = _ecommerceRepositoriesFactory
                .NewEcommerceRegionRepository(connection)
                .GetByNetId(client.EcommerceRegion.NetUid);
            if (ecommerceRegion == null)
                throw new ArgumentException("A valid ecommerce region is required.");

            client.Name = client.Name?.Trim();
            client.PhoneNumber = client.PhoneNumber?.Trim();
            client.EcommerceRegion = ecommerceRegion;
            client.EcommerceRegionId = ecommerceRegion.Id;

            orderItems = await _orderService.RemoveUnavailableProducts(orderItems, 0);

            if (!orderItems.All(o => o.IsMisplacedItem))
                orderItems.First().TotalAmount = orderItems
                    .Where(o => !o.IsMisplacedItem)
                    .Sum(o => o.Product.CurrentLocalPrice * Convert.ToDecimal(o.Qty));

            client.ShoppingCartJson = JsonSerializer.Serialize(orderItems);

            // A phone number is contact data, not authentication. Reusing an existing
            // record here allowed anyone knowing a number to overwrite that guest's cart
            // and receive its bearer NetUid. Every checkout gets a separate capability.
            client.Id = retailClientRepository.Add(client);

            return retailClientRepository.GetRetailClientById(client.Id);
        } catch (ArgumentException) {
            throw;
        } catch (Exception exc) {
            _logger.Error(exc, "Failed to add retail client");
            return null;
        }
    }

    public Task<RetailClient> GetByPhoneNumber(string phone) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        return Task.FromResult(_retailClientRepositoriesFactory.NewRetailClientRepository(connection).GetByPhoneNumber(phone));
    }
}
