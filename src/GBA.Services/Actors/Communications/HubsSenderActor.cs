using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Common.HubRouting;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Messages.Communications.Hubs;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.ReSales.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Services.Hubs;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace GBA.Services.Actors.Communications;

public sealed class HubsSenderActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IHubContext<DataSyncHub> _dataSyncHub;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IHubContext<ProductReservationHub> _productReservationHub;
    private readonly IHubContext<ReSaleHub> _reSaleHub;
    private readonly IReSaleRepositoriesFactory _reSaleRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;

    public HubsSenderActor(
        IDbConnectionFactory connectionFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        IHubContext<ProductReservationHub> productReservationHub,
        IHubContext<DataSyncHub> dataSyncHub,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IHubContext<ReSaleHub> reSaleHub,
        IReSaleRepositoriesFactory reSaleRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _productReservationHub = productReservationHub;
        _dataSyncHub = dataSyncHub;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _reSaleHub = reSaleHub;
        _reSaleRepositoriesFactory = reSaleRepositoriesFactory;

        Receive<AddedSaleNotificationMessage>(ProcessAddedSaleNotificationMessage);

        Receive<UpdatedSaleNotificationMessage>(ProcessUpdatedSaleNotificationMessage);

        Receive<GetProductNotificationMessage>(ProcessGetProductNotificationMessage);

        Receive<PushDataSyncNotificationMessage>(ProcessPushDataSyncNotificationMessage);

        Receive<UpdatedReSaleAvailabilitiesMessage>(ProcessUpdatedReSaleAvailabilities);
    }

    private void ProcessUpdatedReSaleAvailabilities(UpdatedReSaleAvailabilitiesMessage message) {
        try {
            _reSaleHub.Clients.All.SendAsync(
                ReSaleHubSegments.UPDATED_RESALE_AVAILABILITIES,
                JsonConvert.SerializeObject(GetAllReSaleAvailabilities())
            ).PipeTo(Self);
        } catch (Exception) {
            //Ignored
        }
    }

    private void ProcessAddedSaleNotificationMessage(AddedSaleNotificationMessage message) {
        try {
            _productReservationHub.Clients.All.SendAsync(
                ProductReservationSegments.ADDED_NEW_SALE,
                JsonConvert.SerializeObject(GetSaleStatisticByNetId(message.SaleNetId))
            ).PipeTo(Self);
        } catch (Exception) {
            //Ignored
        }
    }

    private void ProcessUpdatedSaleNotificationMessage(UpdatedSaleNotificationMessage message) {
        try {
            _productReservationHub.Clients.All.SendAsync(
                ProductReservationSegments.SALE_UPDATED,
                JsonConvert.SerializeObject(GetSaleStatisticByNetId(message.SaleNetId))
            ).PipeTo(Self);
        } catch (Exception) {
            //Ignored
        }
    }

    private void ProcessGetProductNotificationMessage(GetProductNotificationMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ClientAgreement clientAgreement =
            _clientRepositoriesFactory
                .NewClientAgreementRepository(connection)
                .GetByNetIdWithAgreement(
                    message.ClientAgreementId
                );

        if (clientAgreement == null) return;

        Product product =
            _productRepositoriesFactory
                .NewGetSingleProductRepository(connection)
                .GetByIdWithCalculatedAvailability(
                    message.ProductId,
                    clientAgreement.Agreement.OrganizationId ?? 0,
                    clientAgreement.Agreement.WithVATAccounting,
                    clientAgreement.NetUid
                );

        // CalculatedLocalPricingForSearchedProduct(product,
        //     _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
        //     _currencyRepositoriesFactory.NewCurrencyRepository(connection));

        try {
            _productReservationHub.Clients.All.SendAsync(
                ProductReservationSegments.GET_PRODUCT_WITHOUT_RESERVED_COUNT,
                JsonConvert.SerializeObject(product)
            ).PipeTo(Self);
        } catch (Exception) {
            //Ignored
        }
    }

    private void ProcessPushDataSyncNotificationMessage(PushDataSyncNotificationMessage message) {
        if (!string.IsNullOrEmpty(message.Message))
            _dataSyncHub.Clients.All.SendAsync(
                DataSyncHubSegments.PROCESS_NOTIFICATION_MESSAGE,
                JsonConvert.SerializeObject(new { DisplayMessage = message.Message, message.StopProgressBar, message.IsError })
            );
    }

    private IEnumerable<ReSaleAvailability> GetAllReSaleAvailabilities() {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        return _reSaleRepositoriesFactory
            .NewReSaleAvailabilityRepository(connection)
            .GetAllForSignal();
    }

    private SaleStatistic GetSaleStatisticByNetId(Guid netId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

        Sale saleFromDb = saleRepository.GetByNetId(netId);

        saleFromDb.Created = TimeZoneInfo.ConvertTimeFromUtc(
            saleFromDb.Created,
            TimeZoneInfo.FindSystemTimeZoneById(
                CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                    ? "FLE Standard Time"
                    : "Central European Standard Time"
            )
        );

        saleFromDb.Updated = TimeZoneInfo.ConvertTimeFromUtc(
            saleFromDb.Updated,
            TimeZoneInfo.FindSystemTimeZoneById(
                CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                    ? "FLE Standard Time"
                    : "Central European Standard Time"
            )
        );

        if (saleFromDb.ShipmentDate.HasValue)
            saleFromDb.ShipmentDate = TimeZoneInfo.ConvertTimeFromUtc(
                saleFromDb.ShipmentDate.Value,
                TimeZoneInfo.FindSystemTimeZoneById(
                    CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                        ? "FLE Standard Time"
                        : "Central European Standard Time"
                )
            );

        if (saleFromDb.ChangedToInvoice.HasValue)
            saleFromDb.ChangedToInvoice = TimeZoneInfo.ConvertTimeFromUtc(
                saleFromDb.ChangedToInvoice.Value,
                TimeZoneInfo.FindSystemTimeZoneById(
                    CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                        ? "FLE Standard Time"
                        : "Central European Standard Time"
                )
            );

        CalculatePricingsForSaleWithDynamicPrices(saleFromDb,
            _currencyRepositoriesFactory.NewCurrencyRepository(connection),
            _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection));

        dynamic[] toReturnData = new dynamic[LifeCycleLineStatuses.STATUSES.Length];

        FormLifeCycleLine(saleRepository, saleFromDb.NetUid, toReturnData);

        List<SaleExchangeRate> saleExchangeRates = _saleRepositoriesFactory.NewSaleExchangeRateRepository(connection).GetAllBySaleNetId(saleFromDb.NetUid);

        return new SaleStatistic {
            Sale = saleFromDb,
            LifeCycleLine = toReturnData.ToList(),
            SaleExchangeRates = saleExchangeRates
        };
    }

    private static void FormLifeCycleLine(
        ISaleRepository saleRepository,
        Guid saleNetId,
        IList<dynamic> toReturnData) {
        List<dynamic> lifeCycleLine = saleRepository.GetSaleLifeCycleLine(saleNetId);

        for (int index = 0; index < LifeCycleLineStatuses.STATUSES.Length; index++) {
            dynamic result = new ExpandoObject();

            result.Name = nameof(SaleLifeCycleType);
            result.Value = LifeCycleLineStatuses.STATUSES[index];

            if (lifeCycleLine != null && lifeCycleLine.Any()) {
                if (lifeCycleLine.First()?.Value != null && lifeCycleLine.Any(i => i.Value.Equals(LifeCycleLineStatuses.STATUSES[index]))) {
                    dynamic fromList = lifeCycleLine.First(i => i.Value.Equals(LifeCycleLineStatuses.STATUSES[index]));

                    result.Updated = fromList.Updated;
                    result.IsActive = true;
                }
            } else {
                result.Updated = null;
                result.IsActive = false;
            }

            toReturnData[index] = result;
        }
    }

    private static void CalculatePricingsForSaleWithDynamicPrices(
        Sale sale,
        ICurrencyRepository currencyRepository,
        IExchangeRateRepository exchangeRateRepository) {
        decimal vatRate = Convert.ToDecimal(sale.ClientAgreement.Agreement.Organization.VatRate?.Value ?? 0) / 100;
        Currency uah = currencyRepository.GetUAHCurrencyIfExists();

        decimal currentExchangeRateEurToUah = exchangeRateRepository.GetExchangeRateToEuroCurrency(uah);

        if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New))
            foreach (OrderItem orderItem in sale.Order.OrderItems) {
                orderItem.Created = TimeZoneInfo.ConvertTimeFromUtc(
                    orderItem.Created,
                    TimeZoneInfo.FindSystemTimeZoneById(
                        CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                            ? "FLE Standard Time"
                            : "Central European Standard Time"
                    )
                );

                orderItem.Updated = TimeZoneInfo.ConvertTimeFromUtc(
                    orderItem.Updated,
                    TimeZoneInfo.FindSystemTimeZoneById(
                        CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                            ? "FLE Standard Time"
                            : "Central European Standard Time"
                    )
                );

                orderItem.TotalAmount = decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = decimal.Round(orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);

                orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 14, MidpointRounding.AwayFromZero);
                orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 14, MidpointRounding.AwayFromZero);

                orderItem.Product.CurrentPriceEurToUah = orderItem.Product.CurrentPrice * currentExchangeRateEurToUah;

                orderItem.TotalAmount = decimal.Round(orderItem.TotalAmount, 14, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmountLocal, 14, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountEurToUah = decimal.Round(orderItem.Product.CurrentPriceEurToUah * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);

                if (sale.IsVatSale)
                    orderItem.TotalVat =
                        decimal.Round(
                            orderItem.TotalAmountLocal * (vatRate / (vatRate + 1)),
                            14,
                            MidpointRounding.AwayFromZero);
            }
        else
            foreach (OrderItem orderItem in sale.Order.OrderItems) {
                orderItem.Created = TimeZoneInfo.ConvertTimeFromUtc(
                    orderItem.Created,
                    TimeZoneInfo.FindSystemTimeZoneById(
                        CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                            ? "FLE Standard Time"
                            : "Central European Standard Time"
                    )
                );

                orderItem.Updated = TimeZoneInfo.ConvertTimeFromUtc(
                    orderItem.Updated,
                    TimeZoneInfo.FindSystemTimeZoneById(
                        CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                            ? "FLE Standard Time"
                            : "Central European Standard Time"
                    )
                );

                orderItem.TotalAmount =
                    decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal =
                    decimal.Round(
                        decimal.Round(orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero),
                        14,
                        MidpointRounding.AwayFromZero
                    );

                orderItem.Product.CurrentPriceEurToUah = orderItem.Product.CurrentPrice * currentExchangeRateEurToUah;
                orderItem.TotalAmountEurToUah = decimal.Round(orderItem.Product.CurrentPriceEurToUah * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);

                if (sale.IsVatSale)
                    orderItem.TotalVat =
                        decimal.Round(
                            orderItem.TotalAmountLocal * (vatRate / (vatRate + 1)),
                            14,
                            MidpointRounding.AwayFromZero);

                orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 14, MidpointRounding.AwayFromZero);
                orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 14, MidpointRounding.AwayFromZero);
            }

        sale.Order.TotalAmount = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmount), 2, MidpointRounding.AwayFromZero);
        sale.Order.TotalAmountLocal = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmountLocal), 2, MidpointRounding.AwayFromZero);
        sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);
        sale.Order.TotalAmountEurToUah = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmountEurToUah), 2, MidpointRounding.AwayFromZero);
        if (sale.IsVatSale)
            sale.Order.TotalVat = sale.Order.TotalAmountLocal * (vatRate / (vatRate + 1));

        if (sale.SaleInvoiceDocument != null) {
            sale.SaleInvoiceDocument.ShippingAmount =
                decimal.Round(
                    sale.SaleInvoiceDocument.ShippingAmountEur * sale.SaleInvoiceDocument.ExchangeRateAmount,
                    14,
                    MidpointRounding.AwayFromZero
                );
            sale.SaleInvoiceDocument.ShippingAmountWithoutVat =
                decimal.Round(
                    sale.SaleInvoiceDocument.ShippingAmountEurWithoutVat * sale.SaleInvoiceDocument.ExchangeRateAmount,
                    14,
                    MidpointRounding.AwayFromZero
                );

            sale.SaleInvoiceDocument.ShippingAmountEur = decimal.Round(sale.SaleInvoiceDocument.ShippingAmountEur, 14, MidpointRounding.AwayFromZero);

            sale.Order.TotalAmount =
                decimal.Round(sale.Order.TotalAmount + sale.SaleInvoiceDocument.ShippingAmountEur, 14, MidpointRounding.AwayFromZero);
            sale.Order.TotalAmountLocal =
                decimal.Round(sale.Order.TotalAmountLocal + sale.SaleInvoiceDocument.ShippingAmount, 14, MidpointRounding.AwayFromZero);
        }

        sale.TotalAmount = sale.Order.TotalAmount;
        sale.TotalAmountLocal = sale.Order.TotalAmountLocal;
        sale.TotalCount = sale.Order.TotalCount;
        sale.TotalAmountEurToUah = sale.Order.TotalAmountEurToUah;
    }

    private static void CalculatedLocalPricingForSearchedProduct(
        Product product,
        IExchangeRateRepository exchangeRateRepository,
        ICurrencyRepository currencyRepository) {
        Currency uah = currencyRepository.GetUAHCurrencyIfExists();

        decimal currentExchangeRateEurToUah = exchangeRateRepository.GetExchangeRateToEuroCurrency(uah);

        product.CurrentPriceEurToUah = product.CurrentPrice * currentExchangeRateEurToUah;
        product.CurrentPriceReSaleEurToUah = product.CurrentPriceReSale * currentExchangeRateEurToUah;
    }
}