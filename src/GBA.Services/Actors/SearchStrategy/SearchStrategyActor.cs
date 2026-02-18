using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.FilterEntities;
using GBA.Domain.Helpers;
using GBA.Domain.Messages.SearchStrategy;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.DocumentMonths.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using Newtonsoft.Json;

namespace GBA.Services.Actors.SearchStrategy;

public sealed class SearchStrategyActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDocumentMonthRepositoryFactory _documentMonthRepositoryFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IPricingRepositoriesFactory _pricingRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public SearchStrategyActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IPricingRepositoriesFactory pricingRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IDocumentMonthRepositoryFactory documentMonthRepositoryFactory,
        IXlsFactoryManager xlsFactoryManager,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _pricingRepositoriesFactory = pricingRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _documentMonthRepositoryFactory = documentMonthRepositoryFactory;
        _xlsFactoryManager = xlsFactoryManager;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;

        Receive<GetAllByQueryMessage>(ProcessGetAllByQueryMessage);
        Receive<GetAllByQueryDocumentMessage>(ProcessGetAllByQueryDocumentMessage);
    }

    private void ProcessGetAllByQueryDocumentMessage(GetAllByQueryDocumentMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        string xlsxFile = string.Empty;
        string pdfFile = string.Empty;
        List<Client> clients = _clientRepositoriesFactory
            .NewClientRepository(connection)
            .GetAllFromSearch(
                0,
                0,
                string.IsNullOrEmpty(message.Value) ? string.Empty : message.Value,
                "ORDER BY Client.ID DESC ",
                string.Empty,
                string.Empty,
                false
            );
        (xlsxFile, pdfFile) = _xlsFactoryManager.NewSynchronizationXlsManager().ExportUkClientsToXlsx(
            message.SaleInvoicesFolderPath,
            clients,
            _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk"));
        Sender.Tell((xlsxFile, pdfFile));
    }

    private void ProcessGetAllByQueryMessage(GetAllByQueryMessage message) {
        switch (message.GetQuery.Table) {
            case "Client":
                Sender.Tell(RetrieveAllClientsByQuery(message.GetQuery));
                break;
            case "SupplyOrganization":
                Sender.Tell(RetrieveAllSupplyOrganizationByQuery(message.GetQuery));
                break;
            case "User":
                Sender.Tell(RetrieveAllUsersByQuery(message.GetQuery));
                break;
            case "Product":
                Sender.Tell(RetrieveAllProductsByQuery(message.GetQuery));
                break;
            case "Sale":
                Sender.Tell(RetrieveAllSalesByQuery(message.GetQuery));
                break;
            case "Order":
                Sender.Tell(RetrieveAllOrdersByQuery(message.GetQuery, message.UserNetId));
                break;
            default:
                Sender.Tell(new List<object>());
                break;
        }
    }

    private object RetrieveAllOrdersByQuery(GetQuery getQuery, Guid? userNetId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            BooleanFilterItem booleanFilterItem = JsonConvert.DeserializeObject<BooleanFilterItem>(getQuery.BooleanFilter);

            if (booleanFilterItem.SQL.Equals("self") && userNetId != null)
                return _saleRepositoriesFactory.NewOrderRepository(connection).GetAllShopOrdersByUserNetId(userNetId.Value);

            return _saleRepositoriesFactory.NewOrderRepository(connection).GetAllShopOrders(getQuery.Limit, getQuery.Offset);
        } catch (Exception) {
            return _saleRepositoriesFactory.NewOrderRepository(connection).GetAllShopOrders(getQuery.Limit, getQuery.Offset);
        }
    }

    private object RetrieveAllProductsByQuery(GetQuery getQuery) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            Filter filter = JsonConvert.DeserializeObject<Filter>(getQuery.Filter);

            if (Guid.TryParse(filter.FilterItem.Name, out Guid clientAgreementNetId)) {
                IGetMultipleProductsRepository getMultipleProductsRepository = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection);

                string orderBy = SqlBuilder.GenerateSQLOrderByStatement(getQuery);

                string sql = string.IsNullOrEmpty(filter.Value)
                    ? SqlBuilder.GenerateSQLWhereStatement(filter, getQuery.Table, true)
                    : SqlBuilder.GenerateSQLWhereStatement(filter, getQuery.Table);

                ClientAgreement clientAgreement = _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetByNetIdWithoutIncludes(clientAgreementNetId);

                //Pricing pricing = pricingRepositoriesFactory.NewPricingRepository(connection).GetByNetIdWithCalculatedExtraCharge(clientAgreement.Agreement.Pricing.NetUid);

                List<Product> productsToReturn = sql.Contains("Storage.NetUID")
                    ? getMultipleProductsRepository.GetAllWithDynamicPrices(
                        "WHERE Product.Deleted = 0 ",
                        orderBy.Equals("ORDER BY Product.ID ") ? "ORDER BY Product.VendorCode " : orderBy,
                        getQuery,
                        filter.Value,
                        clientAgreement?.NetUid ?? Guid.Empty
                    )
                    : getMultipleProductsRepository.GetAllWithDynamicPrices(
                        sql,
                        orderBy.Equals("ORDER BY Product.ID ") ? "ORDER BY Product.VendorCode " : orderBy,
                        getQuery,
                        filter.Value,
                        clientAgreement?.NetUid ?? Guid.Empty
                    );

                return productsToReturn;
            } else {
                List<Pricing> allPricings = _pricingRepositoriesFactory.NewPricingRepository(connection).GetAllWithCalculatedExtraChargeByCurrentCulture();

                if (filter.FilterItem == null) {
                    string order = SqlBuilder.GenerateSQLOrderByStatement(getQuery);

                    List<Product> products = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection).GetAll(order, getQuery.Limit, getQuery.Offset);

                    products.ForEach(product => {
                        allPricings.ForEach(pricing => {
                            if (pricing.BasePricing == null) return;

                            if (product.ProductPricings.Any(p => p.PricingId.Equals(pricing.Id)) ||
                                !product.ProductPricings.Any(p => p.PricingId.Equals(pricing.BasePricing.Id))) return;

                            ProductPricing defaultPricing = product.ProductPricings.First(p => p.PricingId.Equals(pricing.BasePricing.Id));

                            product.ProductPricings.Add(new ProductPricing {
                                PricingId = pricing.Id,
                                Pricing = pricing,
                                Price = Math.Round(defaultPricing.Price + defaultPricing.Price * Convert.ToDecimal(pricing.ExtraCharge) / 100, 2)
                            });
                        });

                        if (!product.HasAnalogue) return;

                        foreach (ProductAnalogue productAnalogue in product.AnalogueProducts)
                            allPricings.ForEach(pricing => {
                                if (pricing.BasePricing == null) return;
                                if (productAnalogue.AnalogueProduct.ProductPricings.Any(p => p.PricingId.Equals(pricing.Id)) ||
                                    !productAnalogue.AnalogueProduct.ProductPricings.Any(p => p.PricingId.Equals(pricing.BasePricing.Id))) return;

                                ProductPricing defaultPricing =
                                    productAnalogue.AnalogueProduct.ProductPricings.First(p => p.PricingId.Equals(pricing.BasePricing.Id));

                                productAnalogue.AnalogueProduct.ProductPricings.Add(new ProductPricing {
                                    PricingId = pricing.Id,
                                    Pricing = pricing,
                                    Price = Math.Round(defaultPricing.Price + defaultPricing.Price * Convert.ToDecimal(pricing.ExtraCharge) / 100, 2)
                                });
                            });
                    });

                    return products;
                }

                string orderBy = SqlBuilder.GenerateSQLOrderByStatement(getQuery);

                List<Product> productsToReturn = _productRepositoriesFactory.NewGetMultipleProductsRepository(connection).GetAllWithDynamicPrices(
                    "WHERE Product.Deleted = 0 ",
                    orderBy.Equals("ORDER BY Product.ID ") ? "ORDER BY Product.VendorCode " : orderBy,
                    getQuery,
                    filter.Value,
                    Guid.Empty
                );

                ExchangeRate exchangeRate = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection).GetEuroExchangeRateByCurrentCulture();

                foreach (Product product in productsToReturn) {
                    ProductPricing defaultPricing = product.ProductPricings.First();

                    product.CurrentPrice = Math.Round(defaultPricing.Price, 2);
                    product.CurrentLocalPrice = Math.Round(defaultPricing.Price * exchangeRate.Amount, 2);

                    allPricings.ForEach(pricing => {
                        product.ProductPricings.Add(new ProductPricing {
                            PricingId = pricing.Id,
                            Pricing = pricing,
                            ProductId = product.Id,
                            Price = pricing.ExtraCharge.HasValue ? defaultPricing.Price * Convert.ToDecimal(pricing.ExtraCharge) : defaultPricing.Price
                        });
                    });

                    if (product.AnalogueProducts.Any())
                        foreach (ProductAnalogue analogue in product.AnalogueProducts) {
                            ProductPricing analogueDefaultPricing = analogue.AnalogueProduct.ProductPricings.First();

                            analogue.AnalogueProduct.CurrentPrice = Math.Round(analogueDefaultPricing.Price, 2);
                            analogue.AnalogueProduct.CurrentLocalPrice = Math.Round(analogueDefaultPricing.Price * exchangeRate.Amount, 2);

                            allPricings.ForEach(pricing => {
                                analogue.AnalogueProduct.ProductPricings.Add(new ProductPricing {
                                    PricingId = pricing.Id,
                                    Pricing = pricing,
                                    ProductId = product.Id,
                                    Price = pricing.ExtraCharge.HasValue
                                        ? analogueDefaultPricing.Price * Convert.ToDecimal(pricing.ExtraCharge)
                                        : analogueDefaultPricing.Price
                                });
                            });
                        }

                    if (!product.ComponentProducts.Any()) continue;

                    foreach (ProductSet set in product.ComponentProducts) {
                        ProductPricing componentDefaultPricing = set.ComponentProduct.ProductPricings.First();

                        set.ComponentProduct.CurrentPrice = Math.Round(componentDefaultPricing.Price, 2);
                        set.ComponentProduct.CurrentLocalPrice = Math.Round(componentDefaultPricing.Price * exchangeRate.Amount, 2);

                        allPricings.ForEach(pricing => {
                            set.ComponentProduct.ProductPricings.Add(new ProductPricing {
                                PricingId = pricing.Id,
                                Pricing = pricing,
                                ProductId = product.Id,
                                Price = pricing.ExtraCharge.HasValue
                                    ? componentDefaultPricing.Price * Convert.ToDecimal(pricing.ExtraCharge)
                                    : componentDefaultPricing.Price
                            });
                        });

                        if (!set.ComponentProduct.AnalogueProducts.Any()) continue;

                        foreach (ProductAnalogue analogue in set.ComponentProduct.AnalogueProducts) {
                            ProductPricing analogueDefaultPricing = analogue.AnalogueProduct.ProductPricings.First();

                            analogue.AnalogueProduct.CurrentPrice = Math.Round(analogueDefaultPricing.Price, 2);
                            analogue.AnalogueProduct.CurrentLocalPrice = Math.Round(analogueDefaultPricing.Price * exchangeRate.Amount, 2);

                            allPricings.ForEach(pricing => {
                                analogue.AnalogueProduct.ProductPricings.Add(new ProductPricing {
                                    PricingId = pricing.Id,
                                    Pricing = pricing,
                                    ProductId = product.Id,
                                    Price = pricing.ExtraCharge.HasValue
                                        ? analogueDefaultPricing.Price * Convert.ToDecimal(pricing.ExtraCharge)
                                        : analogueDefaultPricing.Price
                                });
                            });
                        }
                    }
                }

                return productsToReturn;
            }
        } catch (Exception) {
            return _productRepositoriesFactory.NewGetMultipleProductsRepository(connection).GetAll(getQuery.Limit, getQuery.Offset);
        }
    }

    private object RetrieveAllSupplyOrganizationByQuery(GetQuery getQuery) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            Filter filter = JsonConvert.DeserializeObject<Filter>(getQuery.Filter);

            string orderBy = SqlBuilder.GenerateSQLOrderByStatement(getQuery, true);

            if (filter != null)
                return _supplyRepositoriesFactory.NewSupplyOrganizationRepository(connection).GetAllFromSearchFiltered(filter.Value, getQuery.Limit, getQuery.Offset);

            return _supplyRepositoriesFactory.NewSupplyOrganizationRepository(connection).GetAll(null);
        } catch (Exception) {
            return _supplyRepositoriesFactory.NewSupplyOrganizationRepository(connection).GetAll(null);
        }
    }

    private object RetrieveAllClientsByQuery(GetQuery getQuery) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            Filter filter = JsonConvert.DeserializeObject<Filter>(getQuery.Filter);

            string booleanFilterSql = string.Empty;
            string roleTypeSql = string.Empty;
            string orderBy = SqlBuilder.GenerateSQLOrderByStatement(getQuery, true);

            if (!string.IsNullOrEmpty(getQuery.BooleanFilter))
                booleanFilterSql =
                    SqlBuilder.GenerateAdditionalBoolSQLWhereStatement(JsonConvert.DeserializeObject<BooleanFilterItem>(getQuery.BooleanFilter), getQuery.Table);
            if (!string.IsNullOrEmpty(getQuery.TypeRoleFilter)) roleTypeSql = SqlBuilder.GenerateClientsSQLInStatement(getQuery.TypeRoleFilter);

            if (filter != null)
                return _clientRepositoriesFactory
                    .NewClientRepository(connection)
                    .GetAllFromSearch(
                        getQuery.Limit,
                        getQuery.Offset,
                        string.IsNullOrEmpty(filter.Value) ? string.Empty : filter.Value,
                        orderBy,
                        booleanFilterSql,
                        roleTypeSql,
                        filter.FilterItem == null || filter.FilterItem.Type.Equals(FilterEntityType.Client),
                        getQuery.forReSale
                    );
            return _clientRepositoriesFactory
                .NewClientRepository(connection)
                .GetAllFromSearch(
                    getQuery.Limit,
                    getQuery.Offset,
                    string.Empty,
                    orderBy,
                    booleanFilterSql,
                    roleTypeSql
                );
        } catch (Exception) {
            return _clientRepositoriesFactory.NewClientRepository(connection).GetAll(getQuery.Offset, getQuery.Limit);
        }
    }

    private object RetrieveAllUsersByQuery(GetQuery getQuery) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            Filter filter = JsonConvert.DeserializeObject<Filter>(getQuery.Filter);

            getQuery.Offset = 0;
            getQuery.Limit = 50000;

            if (filter?.FilterItem == null) {
                string order = SqlBuilder.GenerateSQLOrderByStatement(getQuery);

                return _userRepositoriesFactory.NewUserRepository(connection).GetAll(order, getQuery.Limit, getQuery.Offset);
            }

            string orderBy = SqlBuilder.GenerateSQLOrderByStatement(getQuery);

            string sql = string.IsNullOrEmpty(filter.Value)
                ? SqlBuilder.GenerateSQLWhereStatement(filter, getQuery.Table, true)
                : SqlBuilder.GenerateSQLWhereStatement(filter, getQuery.Table);

            return _userRepositoriesFactory.NewUserRepository(connection).GetAll(sql, orderBy, getQuery, filter.Value);
        } catch (Exception) {
            return _userRepositoriesFactory.NewUserRepository(connection).GetAll(getQuery.Limit, getQuery.Offset);
        }
    }

    private object RetrieveAllSalesByQuery(GetQuery getQuery) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            Filter filter = JsonConvert.DeserializeObject<Filter>(getQuery.Filter);

            if (filter?.FilterItem == null) {
                string order = SqlBuilder.GenerateSQLOrderByStatement(getQuery);

                return _saleRepositoriesFactory.NewSaleRepository(connection).GetAll(order, getQuery.Limit, getQuery.Offset);
            }

            string orderBy = SqlBuilder.GenerateSQLOrderByStatement(getQuery);

            string sql =
                string.IsNullOrEmpty(filter.Value)
                    ? SqlBuilder.GenerateSQLWhereStatement(filter, getQuery.Table, true)
                    : SqlBuilder.GenerateSQLWhereStatement(filter, getQuery.Table);

            return _saleRepositoriesFactory.NewSaleRepository(connection).GetAll(sql, orderBy, getQuery);
        } catch (Exception) {
            return new List<Sale>();
        }
    }
}