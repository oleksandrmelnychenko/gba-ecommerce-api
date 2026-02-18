using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Messages.Clients.RetailClients;
using GBA.Domain.Messages.Sales;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Services.Actors.Clients.RetailClientGetActors;

public sealed class BaseRetailClientGetActor : ReceiveActor {
    private static readonly Regex SpecialCharactersReplace = new("[$&+,:;=?@#|/\\\\'\"�<>. ^*()%!\\-]", RegexOptions.Compiled);

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IRetailClientRepositoriesFactory _retailClientRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;

    public BaseRetailClientGetActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IRetailClientRepositoriesFactory retailClientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _retailClientRepositoriesFactory = retailClientRepositoriesFactory;

        Receive<GetAllRetailClientsMessage>(ProcessGetAllRetailClientsMessage);

        Receive<GetAllSalesByRetailClientNetIdMessage>(ProcessGetAllSalesByRetailClientNetIdMessage);

        Receive<GetShoppingCartByRetailClientNetIdMessage>(ProcessGetShoppingCartByRetailClientNetIdMessage);

        Receive<GetAllRetailClientsFilteredMessage>(ProcessGetAllRetailClientsFilteredMessage);

        Receive<GetAllPaymentImagesMessage>(ProcessGetAllPaymentImagesMessage);

        Receive<GetAllPaymentImagesFilteredMessage>(ProcessGetAllPaymentImagesFilteredMessage);
    }

    private void ProcessGetAllPaymentImagesFilteredMessage(GetAllPaymentImagesFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IEnumerable<RetailClientPaymentImage> retailClientPaymentImages = _retailClientRepositoriesFactory.NewRetailClientPaymentImageRepository(connection)
            .GetAllFiltered(
                message.SaleDateFrom,
                message.SaleDateTo,
                message.SaleNumber.Trim(),
                message.ClientName,
                message.PhoneNumber.Trim());

        CalculatePricingsForSalesWithDynamicPrices(
            retailClientPaymentImages.Select(r => r.Sale).ToList(),
            _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
            _currencyRepositoriesFactory.NewCurrencyRepository(connection));

        Sender.Tell(retailClientPaymentImages);
    }

    private void ProcessGetAllPaymentImagesMessage(GetAllPaymentImagesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_retailClientRepositoriesFactory.NewRetailClientPaymentImageRepository(connection).GetAll());
    }

    private void ProcessGetAllRetailClientsFilteredMessage(GetAllRetailClientsFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (string.IsNullOrEmpty(message.Value)) {
            Sender.Tell(null);
        } else {
            message.Value = SpecialCharactersReplace.Replace(message.Value, string.Empty);
            message.Value = message.Value.Trim();

            Sender.Tell(_retailClientRepositoriesFactory.NewRetailClientRepository(connection)
                .GetAllFiltered(
                    message.Value,
                    message.Limit,
                    message.Offset));
        }
    }

    private void ProcessGetShoppingCartByRetailClientNetIdMessage(GetShoppingCartByRetailClientNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_retailClientRepositoriesFactory.NewRetailClientRepository(connection).GetByNetId(message.NetId).ShoppingCartJson);
    }

    private void ProcessGetAllSalesByRetailClientNetIdMessage(GetAllSalesByRetailClientNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        List<Sale> sales =
            _saleRepositoriesFactory
                .NewSaleRepository(connection)
                .GetAllRangedByLifeCycleType(
                    int.MaxValue,
                    0,
                    null,
                    null,
                    null,
                    new DateTime(2000, 1, 1),
                    DateTime.Now,
                    null,
                    string.Empty,
                    false,
                    message.NetId,
                    true,
                    true);

        CalculatePricingsForSalesWithDynamicPrices(sales, _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
            _currencyRepositoriesFactory.NewCurrencyRepository(connection));

        Sender.Tell(sales);
    }

    private void ProcessGetAllRetailClientsMessage(GetAllRetailClientsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_retailClientRepositoriesFactory.NewRetailClientRepository(connection).GetAll());
    }

    private static void CalculatePricingsForSalesWithDynamicPrices(
        List<Sale> sales,
        IExchangeRateRepository exchangeRateRepository,
        ICurrencyRepository currencyRepository) {
        sales.ForEach(sale => {
            decimal vatRate = Convert.ToDecimal(sale.ClientAgreement.Agreement.Organization.VatRate?.Value ?? 0) / 100;

            Currency uah = currencyRepository.GetUAHCurrencyIfExists();

            decimal currentExchangeRateEurToUah = exchangeRateRepository.GetExchangeRateToEuroCurrency(uah);

            if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New))
                foreach (OrderItem orderItem in sale.Order.OrderItems) {
                    orderItem.TotalAmount = decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal = orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty);

                    orderItem.Product.CurrentPriceEurToUah = orderItem.Product.CurrentPrice * currentExchangeRateEurToUah;
                    orderItem.TotalAmountEurToUah = decimal.Round(orderItem.Product.CurrentPriceEurToUah * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);

                    orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 14, MidpointRounding.AwayFromZero);
                    orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 14, MidpointRounding.AwayFromZero);

                    orderItem.TotalAmount = decimal.Round(orderItem.TotalAmount, 14, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmountLocal, 14, MidpointRounding.AwayFromZero);

                    if (sale.IsVatSale)
                        orderItem.TotalVat =
                            decimal.Round(
                                orderItem.TotalAmountLocal * (vatRate / (vatRate + 1)),
                                14,
                                MidpointRounding.AwayFromZero);
                }
            else
                foreach (OrderItem orderItem in sale.Order.OrderItems) {
                    orderItem.TotalAmount = orderItem.PricePerItem.Equals(0)
                        ? decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero)
                        : decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);

                    orderItem.Product.CurrentPriceEurToUah = orderItem.Product.CurrentPrice * currentExchangeRateEurToUah;
                    orderItem.TotalAmountEurToUah = decimal.Round(orderItem.Product.CurrentPriceEurToUah * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);

                    orderItem.TotalAmountLocal = orderItem.PricePerItem.Equals(0)
                        ? decimal.Round(
                            decimal.Round(orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero),
                            14,
                            MidpointRounding.AwayFromZero
                        )
                        : decimal.Round(
                            decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero)
                            * orderItem.ExchangeRateAmount,
                            14,
                            MidpointRounding.AwayFromZero
                        );

                    if (sale.IsVatSale)
                        orderItem.TotalVat =
                            decimal.Round(
                                orderItem.TotalAmountLocal * (vatRate / (vatRate + 1)),
                                14,
                                MidpointRounding.AwayFromZero);

                    orderItem.Product.CurrentPrice = decimal.Round(orderItem.PricePerItem, 14, MidpointRounding.AwayFromZero);
                    orderItem.Product.CurrentLocalPrice =
                        decimal.Round(orderItem.Product.CurrentLocalPrice, 14, MidpointRounding.AwayFromZero);
                }

            sale.Order.TotalAmount = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmount), 14, MidpointRounding.AwayFromZero);
            sale.Order.TotalAmountLocal = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmountLocal), 2, MidpointRounding.AwayFromZero);
            sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);
            sale.Order.TotalAmountEurToUah = sale.Order.OrderItems.Sum(o => o.TotalAmountEurToUah);

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
            sale.TotalAmountEurToUah = sale.Order.TotalAmountEurToUah;
            sale.TotalCount = sale.Order.TotalCount;
        });
    }
}