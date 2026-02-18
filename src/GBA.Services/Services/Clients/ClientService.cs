using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Clients.RetailClients.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Services.Services.Clients.Contracts;
using GBA.Services.Services.Orders.Contracts;
using Newtonsoft.Json;

namespace GBA.Services.Services.Clients;

public sealed class ClientService : IClientService {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IOrderService _orderService;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IRetailClientRepositoriesFactory _retailClientRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;

    public ClientService(
        IClientRepositoriesFactory clientRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        IRetailClientRepositoriesFactory retailClientRepositoriesFactory,
        IDbConnectionFactory connectionFactory,
        IOrderService orderService,
        IProductRepositoriesFactory productRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory
    ) {
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _retailClientRepositoriesFactory = retailClientRepositoriesFactory;
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
            List<OrderItem> orderItems = JsonConvert.DeserializeObject<List<OrderItem>>(client.ShoppingCartJson);
            List<OrderItem> anyOrderItems = new();

            foreach (OrderItem orderItem in orderItems.Where(i => i.IsNew() && i.Qty > 0)) {
                ProductAvailability productAvailability =
                    productAvailabilityRepository.GetByProductAndStorageIds(orderItem.Product.Id, storage.Id);
                if (!productAvailability.Amount.Equals(0)) {
                    orderItem.IsMisplacedItem = true;
                    anyOrderItems.Add(orderItem);
                }
            }

            client.ShoppingCartJson = JsonConvert.SerializeObject(anyOrderItems);

            return Task.FromResult(client);
    }

    public Task<(RetailClient, string)> GetRetailClientByNetIdCheckOrderItems(Guid netId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);

            RetailClient client = _retailClientRepositoriesFactory.NewRetailClientRepository(connection).GetByNetId(netId);
            IStorageRepository storageRepository = _storageRepositoryFactory.NewStorageRepository(connection);

            Storage storage = storageRepository.GetWithHighestPriority();
            List<OrderItem> orderItems = JsonConvert.DeserializeObject<List<OrderItem>>(client.ShoppingCartJson);
            List<OrderItem> anyOrderItems = new();
            List<OrderItem> notHaveOrderItems = new();
            string notHaveOrderItemsInfo = string.Empty;


            foreach (OrderItem orderItem in orderItems.Where(i => i.IsNew() && i.Qty > 0)) {
                ProductAvailability productAvailability =
                    productAvailabilityRepository.GetByProductAndStorageIds(orderItem.Product.Id, storage.Id);
                if (!productAvailability.Amount.Equals(0))
                    anyOrderItems.Add(orderItem);
                else
                    notHaveOrderItems.Add(orderItem);
            }

            foreach (OrderItem orderItem in notHaveOrderItems) notHaveOrderItemsInfo += $"������� {orderItem.Product.VendorCode} �������� �� �����;";

            client.ShoppingCartJson = JsonConvert.SerializeObject(anyOrderItems);

            _retailClientRepositoriesFactory.NewRetailClientRepository(connection).Update(client);

            return Task.FromResult((client, notHaveOrderItemsInfo));
    }

    public async Task<RetailClient> AddRetailClient(RetailClient client) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            IRetailClientRepository retailClientRepository = _retailClientRepositoriesFactory.NewRetailClientRepository(connection);

            RetailClient exists = retailClientRepository.GetByPhoneNumber(client.PhoneNumber);

            List<OrderItem> orderItems = JsonConvert.DeserializeObject<List<OrderItem>>(client.ShoppingCartJson);

            if (exists != null) {
                orderItems = await _orderService.RemoveUnavailableProducts(orderItems, exists.Id);

                client.Id = exists.Id;
                client.NetUid = exists.NetUid;
                client.Name = exists.Name;
                client.EcommerceRegionId = client.EcommerceRegion.Id;

                if (!orderItems.All(o => o.IsMisplacedItem))
                    orderItems.First().TotalAmount = orderItems
                        .Where(o => !o.IsMisplacedItem)
                        .Sum(o => o.Product.CurrentLocalPrice * Convert.ToInt32(o.Qty));

                client.ShoppingCartJson = JsonConvert.SerializeObject(orderItems);

                retailClientRepository.Update(client);
            } else {
                client.EcommerceRegionId = client.EcommerceRegion.Id;

                client.Id = retailClientRepository.Add(client);

                orderItems = await _orderService.RemoveUnavailableProducts(orderItems, client.Id);

                if (!orderItems.All(o => o.IsMisplacedItem))
                    orderItems.First().TotalAmount = orderItems
                        .Where(o => !o.IsMisplacedItem)
                        .Sum(o => o.Product.CurrentLocalPrice * Convert.ToInt32(o.Qty));

                client.ShoppingCartJson = JsonConvert.SerializeObject(orderItems);

                retailClientRepository.Update(client);
            }

            return retailClientRepository.GetRetailClientById(client.Id);
        } catch (Exception) {
            // Ignored
            return null;
        }
    }

    public Task<RetailClient> GetByPhoneNumber(string phone) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        return Task.FromResult(_retailClientRepositoriesFactory.NewRetailClientRepository(connection).GetByPhoneNumber(phone));
    }
}
