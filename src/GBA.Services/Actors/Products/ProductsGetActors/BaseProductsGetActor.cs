using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.ProductModels;
using GBA.Domain.Messages.Products;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Services.Actors.Products.ProductsGetActors;

public sealed class BaseProductsGetActor : ReceiveActor {
    private static readonly Regex SpecialCharactersReplace = new("[$&+,:;=?@#|/\\\\'\"�<>. ^*()%!\\-]", RegexOptions.Compiled);
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IPricingRepositoriesFactory _pricingRepositoriesFactory;
    private readonly IProductOneCRepositoriesFactory _productOneCRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;

    public BaseProductsGetActor(
        IDbConnectionFactory connectionFactory,
        IStorageRepositoryFactory storageRepositoryFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IProductOneCRepositoriesFactory productOneCRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IPricingRepositoriesFactory pricingRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _storageRepositoryFactory = storageRepositoryFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _productOneCRepositoriesFactory = productOneCRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _pricingRepositoriesFactory = pricingRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;

        Receive<GetAllLimitedProductsByGroupNetIdMessage>(ProcessGetAllLimitedProductsByGroupNetIdMessage);

        Receive<GetLimitedProductsMessage>(ProcessGetLimitedProductsMessage);

        Receive<GetTopTotalPurchasedProductsByOnlineShopMessage>(ProcessGetTopTotalPurchasedProductsByOnlineShopMessage);

        Receive<GetProductByNetIdMessage>(ProcessGetProductByNetIdMessage);

        Receive<GetAvailabilitiesByProductNetIdMessage>(ProcessGetAvailabilitiesByProductNetIdMessage);

        Receive<GetProductSpecificationCodesFromSearchMessage>(ProcessGetProductSpecificationCodesFromSearchMessage);

        Receive<GetAllProductsFromSearchBySpecificationCode>(ProcessGetAllProductsFromSearchBySpecificationCode);

        Receive<GetProductByNetIdWithAvailabilityByCurrentCultureMessage>(ProcessGetProductByNetIdWithAvailabilityByCurrentCultureMessage);

        Receive<GetAnaloguesByProductNetIdMessage>(ProcessGetAnaloguesByProductNetIdMessage);

        Receive<GetComponentsByProductNetIdMessage>(ProcessGetComponentsByProductNetIdMessage);

        Receive<GetProductsFromSearchWithDynamicPricesCalculated>(ProcessGetProductsFromSearchWithDynamicPricesCalculated);

        Receive<SearchForProductsByVendorCodeMessage>(ProcessSearchForProductsByVendorCodeMessage);

        Receive<SearchForProductsByVendorCodeAndSalesMessage>(ProcessSearchForProductsByVendorCodeAndSalesMessage);

        Receive<GetIncomeInfoByProductNetIdMessage>(ProcessGetIncomeInfoByProductNetIdMessage);

        Receive<GetLastUsedProductPlacementByProductAndStorageNetIdsMessage>(ProcessGetLastUsedProductPlacementByProductAndStorageNetIdsMessage);

        Receive<GetFilteredByProductGroupNetIdMessage>(ProcessGetFilteredByProductGroupNetId);
    }

    private void ProcessGetAllLimitedProductsByGroupNetIdMessage(GetAllLimitedProductsByGroupNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_productRepositoriesFactory
            .NewGetMultipleProductsRepository(connection)
            .GetAllByGroupNetId(message.NetId, message.Limit, message.Offset)
        );
    }

    private void ProcessGetLimitedProductsMessage(GetLimitedProductsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_productRepositoriesFactory.NewGetMultipleProductsRepository(connection).GetAll(message.Limit, message.Offset));
    }

    private void ProcessGetTopTotalPurchasedProductsByOnlineShopMessage(GetTopTotalPurchasedProductsByOnlineShopMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_productRepositoriesFactory.NewGetMultipleProductsRepository(connection).GetTopTotalPurchasedByOnlineShop());
    }

    private void ProcessGetProductByNetIdMessage(GetProductByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetId(message.NetId);

        if (product != null) {
            IPricingRepository pricingRepository = _pricingRepositoriesFactory.NewPricingRepository(connection);

            List<Pricing> pricings = pricingRepository.GetAllWithCalculatedExtraChargeWithDynamicDiscounts(product.NetUid);

            if (product.ProductPricings.Any()) {
                ExchangeRate exchangeRate;

                if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl"))
                    exchangeRate =
                        _exchangeRateRepositoriesFactory
                            .NewExchangeRateRepository(connection)
                            .GetByCurrencyIdAndCode(
                                _currencyRepositoriesFactory
                                    .NewCurrencyRepository(connection)
                                    .GetPLNCurrencyIfExists()
                                    .Id,
                                "EUR",
                                DateTime.UtcNow.AddDays(-1)
                            );
                else
                    exchangeRate = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection).GetEuroExchangeRateByCurrentCulture();

                foreach (Pricing pricing in pricings)
                    if (!pricing.BasePricingId.HasValue) {
                        decimal price =
                            product.ProductPricings.Any(p => p.PricingId.Equals(pricing.Id))
                                ? product.ProductPricings.First(p => p.PricingId.Equals(pricing.Id)).Price
                                : 0m;

                        CalculatedPricingsWithDiscounts calculatedPrice = new() {
                            Pricing = pricing,
                            RetailPriceEUR = pricing.ExtraCharge.HasValue
                                ? Math.Round(Convert.ToDecimal(pricing.ExtraCharge.Value) * price / 100 + price, 14)
                                : Math.Round(price, 14)
                        };

                        calculatedPrice.RetailPriceLocal = exchangeRate != null
                            ? Math.Round(exchangeRate.Amount * calculatedPrice.RetailPriceEUR, 14)
                            : calculatedPrice.RetailPriceEUR;

                        product.CalculatedPrices.Add(calculatedPrice);
                    } else {
                        Pricing basePricing = pricingRepository.GetByNetId(pricing.BasePricing.NetUid);

                        while (basePricing.BasePricingId.HasValue) basePricing = pricingRepository.GetByNetId(basePricing.BasePricing.NetUid);

                        decimal price =
                            product.ProductPricings.Any(p => p.PricingId.Equals(basePricing.Id))
                                ? product.ProductPricings.First(p => p.PricingId.Equals(basePricing.Id)).Price
                                : 0m;

                        CalculatedPricingsWithDiscounts calculatedPrice = new() {
                            Pricing = pricing,
                            RetailPriceEUR = pricing.ExtraCharge.HasValue
                                ? Math.Round(Convert.ToDecimal(pricing.ExtraCharge.Value) * price / 100 + price, 14)
                                : Math.Round(price, 14)
                        };

                        calculatedPrice.RetailPriceLocal = exchangeRate != null
                            ? Math.Round(exchangeRate.Amount * calculatedPrice.RetailPriceEUR, 14)
                            : calculatedPrice.RetailPriceEUR;

                        product.CalculatedPrices.Add(calculatedPrice);
                    }
            } else {
                foreach (Pricing pricing in pricings) product.CalculatedPrices.Add(new CalculatedPricingsWithDiscounts { Pricing = pricing });
            }
        }

        Sender.Tell(product);
    }

    private void ProcessGetAvailabilitiesByProductNetIdMessage(GetAvailabilitiesByProductNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ClientAgreement clientAgreement =
            _clientRepositoriesFactory
                .NewClientAgreementRepository(connection)
                .GetByNetIdWithOrganizationInfo(
                    message.ClientAgreementNetId
                );

        IEnumerable<ProductAvailability> availabilities =
            _productRepositoriesFactory
                .NewGetMultipleProductsRepository(connection)
                .GetAvailabilitiesByProductNetId(
                    message.ProductNetId
                );

        ProductAvailabilitiesModel toReturn = new() {
            ProductAvailabilities = availabilities
        };

        foreach (ProductAvailability availability in availabilities) {
            if (availability.Storage.ForDefective) continue;
            if (availability.Storage.OrganizationId.Equals(clientAgreement.Agreement.OrganizationId))
                if (availability.Storage.Locale.ToLower().Equals("pl")) {
                    if (availability.Storage.ForVatProducts)
                        toReturn.AvailableQtyPlVAT += availability.Amount;
                    else
                        toReturn.AvailableQtyPl += availability.Amount;
                } else {
                    if (availability.Storage.ForVatProducts) {
                        toReturn.AvailableQtyUkVAT += availability.Amount;

                        if (availability.Storage.AvailableForReSale)
                            toReturn.AvailableQtyUkReSale += availability.Amount;
                    } else {
                        toReturn.AvailableQtyUk += availability.Amount;
                    }
                }
            else if (availability.Storage.AvailableForReSale && !availability.Storage.Locale.ToLower().Equals("pl"))
                toReturn.AvailableQtyUkReSale += availability.Amount;
            else if (clientAgreement.Agreement.Organization.StorageId.Equals(availability.StorageId))
                toReturn.AvailableQtyUk += availability.Amount;
        }

        Sender.Tell(
            toReturn
        );
    }

    private void ProcessGetProductSpecificationCodesFromSearchMessage(GetProductSpecificationCodesFromSearchMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_productRepositoriesFactory.NewProductSpecificationRepository(connection).GetAllFromSearch(message.Value));
    }

    private void ProcessGetAllProductsFromSearchBySpecificationCode(GetAllProductsFromSearchBySpecificationCode message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_productRepositoriesFactory.NewGetMultipleProductsRepository(connection).GetAllByActiveProductSpecificationCode(message.Code));
    }

    private void ProcessGetProductByNetIdWithAvailabilityByCurrentCultureMessage(GetProductByNetIdWithAvailabilityByCurrentCultureMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithAvailabilityByCurrentCulture(message.NetId));
    }

    private void ProcessGetAnaloguesByProductNetIdMessage(GetAnaloguesByProductNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ClientAgreement clientAgreement =
            _clientRepositoriesFactory
                .NewClientAgreementRepository(connection)
                .GetByNetIdWithAgreementAndOrganization(
                    message.ClientAgreementNetId
                );

        Sender.Tell(_productRepositoriesFactory.NewGetMultipleProductsRepository(connection)
            .GetAllAnaloguesByProductNetId(
                message.ProductNetId,
                message.ClientAgreementNetId,
                clientAgreement?.Agreement?.OrganizationId));
    }

    private void ProcessGetComponentsByProductNetIdMessage(GetComponentsByProductNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_productRepositoriesFactory.NewGetMultipleProductsRepository(connection).GetAllComponentsByProductNetId(message.ProductNetId, message.ClientAgreementNetId));
    }

    private void ProcessGetProductsFromSearchWithDynamicPricesCalculated(GetProductsFromSearchWithDynamicPricesCalculated message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ClientAgreement clientAgreement =
            _clientRepositoriesFactory
                .NewClientAgreementRepository(connection)
                .GetByNetIdWithAgreement(
                    message.NetId
                );

        Sender.Tell(
            _productRepositoriesFactory
                .NewGetMultipleProductsRepository(connection)
                .GetAllFromSearch(
                    SpecialCharactersReplace.Replace(message.Value, string.Empty),
                    message.Limit,
                    message.Offset,
                    message.NetId,
                    clientAgreement?.Agreement?.WithVATAccounting ?? false
                )
        );
    }

    private void ProcessSearchForProductsByVendorCodeMessage(SearchForProductsByVendorCodeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewGetMultipleProductsRepository(connection)
                .SearchForProductsByVendorCode(
                    SpecialCharactersReplace.Replace(message.Value, string.Empty),
                    message.Limit,
                    message.Offset
                )
        );
    }

    private void ProcessSearchForProductsByVendorCodeAndSalesMessage(SearchForProductsByVendorCodeAndSalesMessage message) {
        if (string.IsNullOrEmpty(message.SearchValue)) {
            Sender.Tell(Enumerable.Empty<Product>());
            return;
        }

        using IDbConnection oneCConnection = _connectionFactory.NewFenixOneCSqlConnection();
        IEnumerable<long> productOldECommerceIds =
            _productOneCRepositoriesFactory
                .NewProductOneCRepository(oneCConnection)
                .GetFromSearchBySales(
                    SpecialCharactersReplace.Replace(message.SearchValue, string.Empty)
                );

        if (!productOldECommerceIds.Any()) {
            Sender.Tell(Enumerable.Empty<Product>());
            return;
        }

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _productRepositoriesFactory
                .NewGetMultipleProductsRepository(connection)
                .GetByOldECommerceIdsFromSearch(
                    productOldECommerceIds,
                    message.Limit,
                    message.Offset
                )
        );
    }

    private void ProcessGetIncomeInfoByProductNetIdMessage(GetIncomeInfoByProductNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(message.NetId);

        List<ProductIncomeInfo> incomeInfos = new();

        if (product != null) {
            List<PackingListPackageOrderItem> packingListsItems =
                _supplyRepositoriesFactory
                    .NewPackingListPackageOrderItemRepository(connection)
                    .GetRemainingInfoByProductId(
                        product.Id
                    );

            incomeInfos.AddRange(packingListsItems.Select(item => new ProductIncomeInfo {
                FromDate = item.PackingList.SupplyInvoice.DateFrom ?? item.PackingList.SupplyInvoice.Created,
                Supplier = item.Supplier,
                RemainingQty = item.RemainingQty,
                PackingListPackageOrderItem = item,
                NetPrice = decimal.Round(item.UnitPriceEur * item.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero),
                GrossPrice = decimal.Round(item.GrossUnitPriceEur * item.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero),
                TotalAmount = decimal.Round(item.UnitPriceEur * Convert.ToDecimal(item.RemainingQty), 2, MidpointRounding.AwayFromZero),
                TotalNetWeight = Math.Round(item.GrossWeight * item.RemainingQty, 3),
                Storage = item.ProductIncomeItem.ProductIncome.Storage,
                ClientAgreement = item.Supplier.ClientAgreements.First()
            }));

            IEnumerable<SupplyOrderUkraineItem> ukraineItems =
                _supplyUkraineRepositoriesFactory
                    .NewSupplyOrderUkraineItemRepository(connection)
                    .GetRemainingInfoByProductId(
                        product.Id
                    );

            incomeInfos.AddRange(ukraineItems.Select(item => new ProductIncomeInfo {
                FromDate = item.SupplyOrderUkraine.FromDate,
                Supplier = item.Supplier,
                RemainingQty = item.ProductIncomeItems.Sum(i => i.RemainingQty),
                SupplyOrderUkraineItem = item,
                TotalAmount =
                    decimal.Round(item.GrossUnitPrice * Convert.ToDecimal(item.ProductIncomeItems.Sum(i => i.RemainingQty)), 2, MidpointRounding.AwayFromZero),
                TotalNetWeight = Math.Round(item.NetWeight * item.ProductIncomeItems.Sum(i => i.RemainingQty), 3),
                NetPrice = item.UnitPrice,
                GrossPrice = item.GrossUnitPrice,
                Storage = item.ProductIncomeItems.First().ProductIncome.Storage,
                ClientAgreement = item.SupplyOrderUkraine.ClientAgreement
            }));
        }

        Sender.Tell(incomeInfos);
    }

    private void ProcessGetLastUsedProductPlacementByProductAndStorageNetIdsMessage(GetLastUsedProductPlacementByProductAndStorageNetIdsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetByNetId(message.StorageNetId);
        Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(message.ProductNetId);

        if (storage != null && product != null) {
            ProductPlacement placement =
                _productRepositoriesFactory
                    .NewProductPlacementRepository(connection)
                    .GetLastByProductAndStorageId(
                        product.Id,
                        storage.Id
                    );

            if (placement != null)
                Sender.Tell(placement);
            else
                Sender.Tell(
                    new ProductPlacement {
                        CellNumber = "N",
                        RowNumber = "N",
                        StorageNumber = "N"
                    }
                );
        } else {
            Sender.Tell(
                new ProductPlacement {
                    CellNumber = "N",
                    RowNumber = "N",
                    StorageNumber = "N"
                }
            );
        }
    }

    private void ProcessGetFilteredByProductGroupNetId(GetFilteredByProductGroupNetIdMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _productRepositoriesFactory
                    .NewProductProductGroupRepository(connection)
                    .GetFilteredByProductGroupNetId(
                        message.NetId,
                        message.Limit,
                        message.Offset,
                        message.Value));
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}