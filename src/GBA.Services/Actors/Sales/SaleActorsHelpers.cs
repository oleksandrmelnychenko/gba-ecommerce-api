using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using GBA.Common.Helpers;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Services.Actors.Sales;

public static class SaleActorsHelpers {
    public static void FormLifeCycleLine(
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

    public static void CalculatePricingsForSale(
        Sale sale,
        IDbConnection connection,
        IExchangeRateRepositoriesFactory exchangeRateRepositoryFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        bool force = false) {
        SaleExchangeRate saleExchangeRate = saleRepositoriesFactory.NewSaleExchangeRateRepository(connection).GetEuroSaleExchangeRateBySaleNetId(sale.NetUid);
        decimal vatRate = Convert.ToDecimal(sale.ClientAgreement.Agreement.Organization.VatRate?.Value ?? 0) / 100;

        if (saleExchangeRate != null) {
            foreach (OrderItem orderItem in sale.Order.OrderItems) {
                decimal currentPrice = force
                    ? orderItem.Product.ProductPricings.First().Price
                    : sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)
                        ? orderItem.Product.CurrentPrice
                        : orderItem.PricePerItem;

                orderItem.TotalAmount = Math.Round(currentPrice * Convert.ToDecimal(orderItem.Qty), 14);
                orderItem.TotalAmountLocal = Math.Round(currentPrice * Convert.ToDecimal(orderItem.Qty) * saleExchangeRate.Value, 14);

                if (sale.IsVatSale)
                    orderItem.TotalVat =
                        decimal.Round(
                            orderItem.TotalAmountLocal * (vatRate / (vatRate + 1)),
                            2,
                            MidpointRounding.AwayFromZero);
            }

            sale.Order.TotalAmount = sale.Order.OrderItems.Sum(o => o.TotalAmount);
            sale.Order.TotalAmountLocal = sale.Order.OrderItems.Sum(o => o.TotalAmountLocal);
            sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);

            if (sale.IsVatSale)
                sale.Order.TotalVat = sale.Order.TotalAmountLocal * (vatRate / (vatRate + 1));

            sale.TotalAmount = sale.Order.TotalAmount;
            sale.TotalAmountLocal = sale.Order.TotalAmountLocal;
            sale.TotalCount = sale.Order.TotalCount;
        } else {
            ExchangeRate exchangeRate = exchangeRateRepositoryFactory.NewExchangeRateRepository(connection).GetEuroExchangeRateByCurrentCulture();

            foreach (OrderItem orderItem in sale.Order.OrderItems) {
                decimal currentPrice = force
                    ? orderItem.Product.ProductPricings.First().Price
                    : sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)
                        ? orderItem.Product.CurrentPrice
                        : orderItem.PricePerItem;

                orderItem.TotalAmount = Math.Round(currentPrice * Convert.ToDecimal(orderItem.Qty), 14);
                orderItem.TotalAmountLocal = Math.Round(currentPrice * Convert.ToDecimal(orderItem.Qty) * exchangeRate.Amount, 14);

                if (sale.IsVatSale)
                    orderItem.TotalVat =
                        decimal.Round(
                            orderItem.TotalAmountLocal * (vatRate / (vatRate + 1)),
                            2,
                            MidpointRounding.AwayFromZero);
            }

            sale.Order.TotalAmount = sale.Order.OrderItems.Sum(o => o.TotalAmount);
            sale.Order.TotalAmountLocal = sale.Order.OrderItems.Sum(o => o.TotalAmountLocal);
            sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);

            if (sale.IsVatSale)
                sale.Order.TotalVat = sale.Order.TotalAmountLocal * (vatRate / (vatRate + 1));

            sale.TotalAmount = sale.Order.TotalAmount;
            sale.TotalAmountLocal = sale.Order.TotalAmountLocal;
            sale.TotalCount = sale.Order.TotalCount;
        }
    }

    public static void CalculatePricingsForSaleWithDynamicPrices(
        Sale sale,
        IExchangeRateRepository exchangeRateRepository,
        ICurrencyRepository currencyRepository) {
        decimal vatRate = Convert.ToDecimal(sale.ClientAgreement.Agreement.Organization.VatRate?.Value ?? 0) / 100;

        Currency uah = currencyRepository.GetUAHCurrencyIfExists();

        decimal currentExchangeRateEurToUah = exchangeRateRepository.GetExchangeRateToEuroCurrency(uah);

        if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New) && !sale.IsPaymentBillDownloaded)
            foreach (OrderItem orderItem in sale.Order.OrderItems) {
                orderItem.TotalAmount = decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty);

                orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 14, MidpointRounding.AwayFromZero);
                orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 14, MidpointRounding.AwayFromZero);
                orderItem.Product.CurrentPriceEurToUah = orderItem.Product.CurrentPrice * currentExchangeRateEurToUah;

                orderItem.TotalAmountEurToUah = orderItem.Product.CurrentPriceEurToUah * Convert.ToDecimal(orderItem.Qty);

                orderItem.TotalAmount = decimal.Round(orderItem.TotalAmount, 14, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmountLocal, 14, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountEurToUah = decimal.Round(orderItem.TotalAmountEurToUah, 14, MidpointRounding.AwayFromZero);

                if (sale.IsVatSale)
                    orderItem.TotalVat =
                        decimal.Round(
                            orderItem.TotalAmountLocal * (vatRate / (vatRate + 1)),
                            14,
                            MidpointRounding.AwayFromZero);
            }
        else
            foreach (OrderItem orderItem in sale.Order.OrderItems) {
                orderItem.TotalAmount =
                    decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal =
                    decimal.Round(
                        decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero)
                        * orderItem.ExchangeRateAmount,
                        14,
                        MidpointRounding.AwayFromZero
                    );

                if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.Packaged)) {
                    orderItem.Product.CurrentPrice = orderItem.PricePerItem;
                    orderItem.Product.CurrentLocalPrice = orderItem.PricePerItem * orderItem.ExchangeRateAmount;
                }

                orderItem.Product.CurrentPriceEurToUah = orderItem.Product.CurrentPrice * orderItem.ExchangeRateAmount;
                orderItem.TotalAmountEurToUah = decimal.Round(orderItem.TotalAmountEurToUah, 14, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountEurToUah = orderItem.Product.CurrentPriceEurToUah * Convert.ToDecimal(orderItem.Qty);

                if (sale.IsVatSale)
                    orderItem.TotalVat =
                        decimal.Round(
                            orderItem.TotalAmountLocal * (vatRate / (vatRate + 1)),
                            14,
                            MidpointRounding.AwayFromZero);

                orderItem.Product.CurrentPrice = decimal.Round(orderItem.PricePerItem, 14, MidpointRounding.AwayFromZero);
                orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 14, MidpointRounding.AwayFromZero);
            }

        sale.Order.TotalAmount = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmount), 14, MidpointRounding.AwayFromZero);
        sale.Order.TotalAmountLocal = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmountLocal), 14, MidpointRounding.AwayFromZero);
        sale.Order.TotalAmountEurToUah = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmountEurToUah), 14, MidpointRounding.AwayFromZero);
        sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);

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

    public static void CalculatePricingsForSales(
        List<Sale> sales,
        IDbConnection connection,
        IExchangeRateRepositoriesFactory exchangeRateRepositoryFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        bool ignoreLocalPrice = true) {
        sales.ForEach(sale => {
            decimal vatRate = Convert.ToDecimal(sale.ClientAgreement.Agreement.Organization.VatRate?.Value ?? 0) / 100;

            if (ignoreLocalPrice) {
                if (sale.ClientAgreement.Agreement.Currency.Code.Equals("EUR")) {
                    foreach (OrderItem orderItem in sale.Order.OrderItems) {
                        decimal currentPrice = sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)
                            ? orderItem.Product.ProductPricings.First().Price
                            : orderItem.PricePerItem;

                        if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) {
                            if (sale.ClientAgreement.Agreement.Pricing.ExtraCharge != null &&
                                sale.ClientAgreement.Agreement.Pricing.ExtraCharge > 0)
                                currentPrice += currentPrice * Convert.ToDecimal(sale.ClientAgreement.Agreement.Pricing.ExtraCharge) / 100;

                            if (orderItem.Product.ProductProductGroups.Any() &&
                                sale.ClientAgreement.ProductGroupDiscounts.Any(d =>
                                    d.ProductGroupId.Equals(orderItem.Product.ProductProductGroups.First().ProductGroupId) && d.IsActive))
                                currentPrice -= currentPrice * Convert.ToDecimal(sale.ClientAgreement.ProductGroupDiscounts
                                    .First(d => d.ProductGroupId.Equals(orderItem.Product.ProductProductGroups.First().ProductGroupId) && d.IsActive)
                                    .DiscountRate) / 100;
                        }

                        orderItem.TotalAmount = Math.Round(currentPrice * Convert.ToDecimal(orderItem.Qty), 14);

                        if (sale.IsVatSale)
                            orderItem.TotalVat =
                                decimal.Round(
                                    orderItem.TotalAmount * (vatRate / (vatRate + 1)),
                                    2,
                                    MidpointRounding.AwayFromZero);
                    }

                    sale.Order.TotalAmount = sale.Order.OrderItems.Sum(o => o.TotalAmount);
                    sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);

                    sale.TotalAmount = sale.Order.TotalAmount;
                    sale.TotalCount = sale.Order.TotalCount;

                    if (sale.IsVatSale)
                        sale.Order.TotalVat = sale.Order.TotalAmount * (vatRate / (vatRate + 1));
                } else {
                    SaleExchangeRate saleExchangeRate = saleRepositoriesFactory.NewSaleExchangeRateRepository(connection).GetEuroSaleExchangeRateBySaleNetId(sale.NetUid);

                    if (saleExchangeRate != null) {
                        foreach (OrderItem orderItem in sale.Order.OrderItems) {
                            decimal currentPrice = sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)
                                ? orderItem.Product.ProductPricings.First().Price
                                : orderItem.PricePerItem;

                            if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) {
                                if (sale.ClientAgreement.Agreement.Pricing.ExtraCharge != null &&
                                    sale.ClientAgreement.Agreement.Pricing.ExtraCharge > 0)
                                    currentPrice += currentPrice * Convert.ToDecimal(sale.ClientAgreement.Agreement.Pricing.ExtraCharge) / 100;

                                if (orderItem.Product.ProductProductGroups.Any() &&
                                    sale.ClientAgreement.ProductGroupDiscounts.Any(d =>
                                        d.ProductGroupId.Equals(orderItem.Product.ProductProductGroups.First().ProductGroupId) && d.IsActive))
                                    currentPrice -= currentPrice * Convert.ToDecimal(sale.ClientAgreement.ProductGroupDiscounts.First(d =>
                                            d.ProductGroupId.Equals(orderItem.Product.ProductProductGroups.First().ProductGroupId) && d.IsActive)
                                        .DiscountRate) / 100;
                            }

                            orderItem.TotalAmount = Math.Round(currentPrice * Convert.ToDecimal(orderItem.Qty) * saleExchangeRate.Value, 14);

                            if (sale.IsVatSale)
                                orderItem.TotalVat =
                                    decimal.Round(
                                        orderItem.TotalAmount * (vatRate / (vatRate + 1)),
                                        2,
                                        MidpointRounding.AwayFromZero);
                        }

                        sale.Order.TotalAmount = sale.Order.OrderItems.Sum(o => o.TotalAmount);
                        sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);

                        sale.TotalAmount = sale.Order.TotalAmount;
                        sale.TotalCount = sale.Order.TotalCount;

                        if (sale.IsVatSale)
                            sale.Order.TotalVat = sale.Order.TotalAmount * (vatRate / (vatRate + 1));
                    } else {
                        ExchangeRate exchangeRate = exchangeRateRepositoryFactory.NewExchangeRateRepository(connection).GetEuroExchangeRateByCurrentCulture();

                        foreach (OrderItem orderItem in sale.Order.OrderItems) {
                            decimal currentPrice = sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)
                                ? orderItem.Product.ProductPricings.First().Price
                                : orderItem.PricePerItem;

                            if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) {
                                if (sale.ClientAgreement.Agreement.Pricing.ExtraCharge != null &&
                                    sale.ClientAgreement.Agreement.Pricing.ExtraCharge > 0)
                                    currentPrice += currentPrice * Convert.ToDecimal(sale.ClientAgreement.Agreement.Pricing.ExtraCharge) / 100;

                                if (orderItem.Product.ProductProductGroups.Any() &&
                                    sale.ClientAgreement.ProductGroupDiscounts.Any(d =>
                                        d.ProductGroupId.Equals(orderItem.Product.ProductProductGroups.First().ProductGroupId) && d.IsActive))
                                    currentPrice -= currentPrice * Convert.ToDecimal(sale.ClientAgreement.ProductGroupDiscounts.First(d =>
                                            d.ProductGroupId.Equals(orderItem.Product.ProductProductGroups.First().ProductGroupId) && d.IsActive)
                                        .DiscountRate) / 100;
                            }

                            orderItem.TotalAmount = Math.Round(currentPrice * Convert.ToDecimal(orderItem.Qty) * exchangeRate.Amount, 14);

                            if (sale.IsVatSale)
                                orderItem.TotalVat =
                                    decimal.Round(
                                        orderItem.TotalAmount * (vatRate / (vatRate + 1)),
                                        2,
                                        MidpointRounding.AwayFromZero);
                        }

                        sale.Order.TotalAmount = sale.Order.OrderItems.Sum(o => o.TotalAmount);
                        sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);

                        sale.TotalAmount = sale.Order.TotalAmount;
                        sale.TotalCount = sale.Order.TotalCount;
                    }
                }
            } else {
                if (sale.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("usd")) {
                    ExchangeRate exchangeRate = exchangeRateRepositoryFactory.NewExchangeRateRepository(connection).GetEuroExchangeRateByCurrentCulture();

                    CrossExchangeRate crossExchangeRate = exchangeRateRepositoryFactory.NewCrossExchangeRateRepository(connection)
                        .GetByCurrenciesIds(2, sale.ClientAgreement.Agreement.Currency.Id);

                    foreach (OrderItem orderItem in sale.Order.OrderItems) {
                        decimal currentPrice = sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)
                            ? orderItem.Product.ProductPricings.First().Price
                            : orderItem.PricePerItem;

                        if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) {
                            if (sale.ClientAgreement.Agreement.Pricing.ExtraCharge != null &&
                                sale.ClientAgreement.Agreement.Pricing.ExtraCharge > 0)
                                currentPrice += currentPrice * Convert.ToDecimal(sale.ClientAgreement.Agreement.Pricing.ExtraCharge) / 100;

                            if (orderItem.Product.ProductProductGroups.Any() &&
                                sale.ClientAgreement.ProductGroupDiscounts.Any(d =>
                                    d.ProductGroupId.Equals(orderItem.Product.ProductProductGroups.First().ProductGroupId) && d.IsActive))
                                currentPrice -= currentPrice * Convert.ToDecimal(sale.ClientAgreement.ProductGroupDiscounts
                                    .First(d => d.ProductGroupId.Equals(orderItem.Product.ProductProductGroups.First().ProductGroupId) && d.IsActive)
                                    .DiscountRate) / 100;
                        }

                        orderItem.TotalAmount = Math.Round(currentPrice * Convert.ToDecimal(orderItem.Qty) * crossExchangeRate.Amount, 14);
                        orderItem.TotalAmountLocal = Math.Round(currentPrice * Convert.ToDecimal(orderItem.Qty) * exchangeRate.Amount, 14);

                        if (sale.IsVatSale)
                            orderItem.TotalVat =
                                decimal.Round(
                                    orderItem.TotalAmountLocal * (vatRate / (vatRate + 1)),
                                    2,
                                    MidpointRounding.AwayFromZero);
                    }

                    sale.Order.TotalAmount = sale.Order.OrderItems.Sum(o => o.TotalAmount);
                    sale.Order.TotalAmountLocal = sale.Order.OrderItems.Sum(o => o.TotalAmountLocal);
                    sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);

                    sale.TotalAmount = sale.Order.TotalAmount;
                    sale.TotalAmountLocal = sale.Order.TotalAmountLocal;
                    sale.TotalCount = sale.Order.TotalCount;

                    if (sale.IsVatSale)
                        sale.Order.TotalVat = sale.Order.TotalAmountLocal * (vatRate / (vatRate + 1));
                } else {
                    ExchangeRate exchangeRate = exchangeRateRepositoryFactory.NewExchangeRateRepository(connection).GetEuroExchangeRateByCurrentCulture();

                    foreach (OrderItem orderItem in sale.Order.OrderItems) {
                        decimal currentPrice = sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)
                            ? orderItem.Product.ProductPricings.First().Price
                            : orderItem.PricePerItem;

                        if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New)) {
                            if (sale.ClientAgreement.Agreement.Pricing.ExtraCharge != null &&
                                sale.ClientAgreement.Agreement.Pricing.ExtraCharge > 0)
                                currentPrice += currentPrice * Convert.ToDecimal(sale.ClientAgreement.Agreement.Pricing.ExtraCharge) / 100;

                            if (orderItem.Product.ProductProductGroups.Any() &&
                                sale.ClientAgreement.ProductGroupDiscounts.Any(d =>
                                    d.ProductGroupId.Equals(orderItem.Product.ProductProductGroups.First().ProductGroupId) && d.IsActive))
                                currentPrice -= currentPrice * Convert.ToDecimal(sale.ClientAgreement.ProductGroupDiscounts
                                    .First(d => d.ProductGroupId.Equals(orderItem.Product.ProductProductGroups.First().ProductGroupId) && d.IsActive)
                                    .DiscountRate) / 100;
                        }

                        orderItem.TotalAmount = Math.Round(currentPrice * Convert.ToDecimal(orderItem.Qty), 14);
                        orderItem.TotalAmountLocal = Math.Round(currentPrice * Convert.ToDecimal(orderItem.Qty) * exchangeRate.Amount, 14);

                        if (sale.IsVatSale)
                            orderItem.TotalVat =
                                decimal.Round(
                                    orderItem.TotalAmountLocal * (vatRate / (vatRate + 1)),
                                    2,
                                    MidpointRounding.AwayFromZero);
                    }

                    sale.Order.TotalAmount = sale.Order.OrderItems.Sum(o => o.TotalAmount);
                    sale.Order.TotalAmountLocal = sale.Order.OrderItems.Sum(o => o.TotalAmountLocal);
                    sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);

                    sale.TotalAmount = sale.Order.TotalAmount;
                    sale.TotalAmountLocal = sale.Order.TotalAmountLocal;
                    sale.TotalCount = sale.Order.TotalCount;

                    if (sale.IsVatSale)
                        sale.Order.TotalVat = sale.Order.TotalAmountLocal * (vatRate / (vatRate + 1));
                }
            }
        });
    }

    public static void CalculatePricingsForSalesWithDynamicPrices(
        List<Sale> sales,
        IExchangeRateRepository exchangeRateRepository,
        ICurrencyRepository currencyRepository) {
        sales.ForEach(sale => {
            decimal vatRate = Convert.ToDecimal(sale.ClientAgreement.Agreement.Organization.VatRate?.Value ?? 0) / 100;

            Currency uah = currencyRepository.GetUAHCurrencyIfExists();

            decimal currentExchangeRateEurToUah = exchangeRateRepository.GetExchangeRateToEuroCurrency(uah);

            if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New) && !sale.IsPaymentBillDownloaded)
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
            sale.Order.TotalAmountLocal = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmountLocal), 14, MidpointRounding.AwayFromZero);
            sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);
            sale.Order.TotalAmountEurToUah = sale.Order.OrderItems.Sum(o => o.TotalAmountEurToUah);
            sale.TotalWeight = sale.Order.OrderItems.Sum(o => o.Product.Weight * o.Qty);

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

    public static void CalculatePricingsForSalesWithDynamicPricesWithUsdCalculations(
        List<Sale> sales,
        IExchangeRateRepository exchangeRateRepository,
        ICurrencyRepository currencyRepository) {
        sales.ForEach(sale => {
            decimal vatRate = Convert.ToDecimal(sale.ClientAgreement.Agreement.Organization.VatRate?.Value ?? 0) / 100;

            if (sale.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("usd")) {
                if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New))
                    foreach (OrderItem orderItem in sale.Order.OrderItems) {
                        orderItem.TotalAmount = decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                        orderItem.TotalAmountLocal = orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty);

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
                        orderItem.TotalAmount =
                            decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                        orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmount * orderItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                        orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 14, MidpointRounding.AwayFromZero);
                        orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 14, MidpointRounding.AwayFromZero);

                        if (sale.IsVatSale)
                            orderItem.TotalVat =
                                decimal.Round(
                                    orderItem.TotalAmountLocal * (vatRate / (vatRate + 1)),
                                    2,
                                    MidpointRounding.AwayFromZero);
                    }
            } else {
                Currency uah = currencyRepository.GetUAHCurrencyIfExists();
                decimal currentExchangeRateEurToUah = exchangeRateRepository.GetExchangeRateToEuroCurrency(uah);

                if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New))
                    foreach (OrderItem orderItem in sale.Order.OrderItems) {
                        orderItem.TotalAmount = decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                        orderItem.TotalAmountLocal = orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty);

                        orderItem.Product.CurrentPriceEurToUah = orderItem.Product.CurrentPrice * currentExchangeRateEurToUah;
                        orderItem.TotalAmountEurToUah = orderItem.Product.CurrentPriceEurToUah * Convert.ToDecimal(orderItem.Qty);

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
                        orderItem.TotalAmount =
                            decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                        orderItem.TotalAmountLocal =
                            decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty) * orderItem.ExchangeRateAmount, 14, MidpointRounding.AwayFromZero);

                        orderItem.Product.CurrentPriceEurToUah = orderItem.Product.CurrentPrice * currentExchangeRateEurToUah;
                        orderItem.TotalAmountEurToUah = orderItem.Product.CurrentPriceEurToUah * Convert.ToDecimal(orderItem.Qty);

                        orderItem.Product.CurrentPrice = decimal.Round(orderItem.PricePerItem, 14, MidpointRounding.AwayFromZero);
                        orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 14, MidpointRounding.AwayFromZero);

                        if (sale.IsVatSale)
                            orderItem.TotalVat =
                                decimal.Round(
                                    orderItem.TotalAmountLocal * (vatRate / (vatRate + 1)),
                                    14,
                                    MidpointRounding.AwayFromZero);
                    }
            }

            sale.Order.TotalAmount = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmount), 14, MidpointRounding.AwayFromZero);
            sale.Order.TotalAmountLocal = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmountLocal), 14, MidpointRounding.AwayFromZero);
            sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);
            sale.Order.TotalAmountEurToUah = sale.Order.OrderItems.Sum(o => o.TotalAmountEurToUah);

            if (sale.IsVatSale)
                sale.Order.TotalVat = sale.Order.TotalAmountLocal * (vatRate / (vatRate + 1));

            sale.TotalAmount = sale.Order.TotalAmount;
            sale.TotalAmountLocal = sale.Order.TotalAmountLocal;
            sale.TotalCount = sale.Order.TotalCount;
            sale.TotalAmountEurToUah = sale.Order.TotalAmountEurToUah;
        });
    }

    public static void CalculatePricingsForSaleWithDynamicPricesWithUsdCalculations(
        Sale sale,
        IExchangeRateRepository exchangeRateRepository) {
        decimal vatRate = Convert.ToDecimal(sale.ClientAgreement.Agreement.Organization.VatRate?.Value ?? 0) / 100;

        if (sale.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("usd")) {
            decimal euroUsdExchangeRateAmount = exchangeRateRepository.GetEuroToUsdExchangeRateAmountByFromDate(sale.ChangedToInvoice ?? DateTime.UtcNow);

            if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New))
                foreach (OrderItem orderItem in sale.Order.OrderItems) {
                    orderItem.TotalAmount = decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal = orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty);

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
                    orderItem.TotalAmount =
                        decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal =
                        decimal.Round(
                            decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero)
                            * euroUsdExchangeRateAmount,
                            14,
                            MidpointRounding.AwayFromZero
                        );

                    if (sale.IsVatSale)
                        orderItem.TotalVat =
                            decimal.Round(
                                orderItem.TotalAmountLocal * (vatRate / (vatRate + 1)),
                                2,
                                MidpointRounding.AwayFromZero);

                    orderItem.Product.CurrentPrice = decimal.Round(orderItem.Product.CurrentPrice, 14, MidpointRounding.AwayFromZero);
                    orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 14, MidpointRounding.AwayFromZero);
                }
        } else {
            if (sale.BaseLifeCycleStatus.SaleLifeCycleType.Equals(SaleLifeCycleType.New) && sale.IsPaymentBillDownloaded)
                foreach (OrderItem orderItem in sale.Order.OrderItems) {
                    orderItem.TotalAmount = decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal = orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty);

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
                    orderItem.TotalAmount =
                        decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 14, MidpointRounding.AwayFromZero);
                    orderItem.TotalAmountLocal =
                        decimal.Round(
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
                    orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 14, MidpointRounding.AwayFromZero);
                }
        }

        sale.Order.TotalAmount = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmount), 14, MidpointRounding.AwayFromZero);
        sale.Order.TotalAmountLocal = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmountLocal), 14, MidpointRounding.AwayFromZero);
        sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);

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
    }
}