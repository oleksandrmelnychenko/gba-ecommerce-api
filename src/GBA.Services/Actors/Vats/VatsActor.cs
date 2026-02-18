using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Messages.Vats;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Services.Actors.Vats;

public sealed class VatsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;

    public VatsActor(
        IDbConnectionFactory connectionFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;

        Receive<GetVatInfoFilteredMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Currency pln = _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetPLNCurrencyIfExists();

            List<Sale> sales =
                _saleRepositoriesFactory
                    .NewSaleRepository(connection)
                    .GetAllPlSalesFiltered(
                        message.From,
                        message.To
                    );

            IExchangeRateRepository exchangeRateRepository = _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection);

            CalculatePricingForSalesWithDynamicPrices(
                sales,
                pln,
                exchangeRateRepository
            );

            List<VatInfo> infos = sales.Select(sale => new VatInfo {
                Sale = sale,
                FromDate = sale.ChangedToInvoice ?? sale.Created,
                VatPercent = sale.SaleInvoiceDocument?.Vat ?? 23m,
                VatAmountEU = sale.VatAmount,
                VatAmountPL = sale.VatAmountPln
            }).ToList();

            List<SupplyInvoice> invoices =
                _supplyRepositoriesFactory
                    .NewSupplyInvoiceRepository(connection)
                    .GetAllIncomedInvoicesFiltered(
                        message.From,
                        message.To
                    );

            foreach (SupplyInvoice invoice in invoices) {
                ExchangeRate exchangeRate =
                    exchangeRateRepository
                        .GetByCurrencyIdAndCode(
                            pln.Id,
                            "EUR",
                            invoice.DateFrom?.AddDays(-1) ?? invoice.Created.AddDays(-1)
                        );

                decimal vatAmount = decimal.Zero;
                decimal vatAmountLocal = decimal.Zero;

                foreach (PackingList packingList in invoice.PackingLists)
                foreach (PackingListPackageOrderItem item in packingList.PackingListPackageOrderItems) {
                    decimal currentAmount =
                        decimal.Round(Convert.ToDecimal(item.Qty) * item.GrossUnitPriceEur, 2, MidpointRounding.AwayFromZero);

                    vatAmount =
                        decimal.Round(
                            vatAmount + decimal.Round(currentAmount * 0.23m, 2, MidpointRounding.AwayFromZero),
                            2,
                            MidpointRounding.AwayFromZero
                        );

                    vatAmountLocal =
                        decimal.Round(
                            vatAmountLocal + decimal.Round(currentAmount * exchangeRate.Amount * 0.23m, 2, MidpointRounding.AwayFromZero),
                            2,
                            MidpointRounding.AwayFromZero
                        );
                }

                infos.Add(new VatInfo {
                    SupplyInvoice = invoice,
                    FromDate = invoice.DateFrom ?? invoice.Created,
                    VatPercent = 23m,
                    VatAmountEU = vatAmount,
                    VatAmountPL = vatAmountLocal
                });
            }

            Sender.Tell(infos.OrderBy(i => i.FromDate).Skip(message.Offset).Take(message.Limit));
        });
    }

    private static void CalculatePricingForSalesWithDynamicPrices(
        List<Sale> sales,
        Currency pln,
        IExchangeRateRepository exchangeRateRepository) {
        sales.ForEach(sale => {
            foreach (OrderItem orderItem in sale.Order.OrderItems) {
                decimal exchangeRateAmount = decimal.Zero;

                if (sale.ChangedToInvoice.HasValue) {
                    if (sale.ClientAgreement.Agreement.Currency.Code.ToLower().Equals("pln") && !orderItem.ExchangeRateAmount.Equals(decimal.Zero)) {
                        exchangeRateAmount = orderItem.ExchangeRateAmount;
                    } else {
                        ExchangeRate exchangeRate =
                            exchangeRateRepository
                                .GetByCurrencyIdAndCode(
                                    pln.Id,
                                    "EUR",
                                    sale.ChangedToInvoice.Value.AddDays(-1)
                                );

                        exchangeRateAmount = exchangeRate.Amount;
                    }
                }

                orderItem.Product.CurrentLocalPrice =
                    decimal.Round(orderItem.PricePerItem * exchangeRateAmount, 4, MidpointRounding.AwayFromZero);

                orderItem.TotalAmount =
                    decimal.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmount * exchangeRateAmount, 2, MidpointRounding.AwayFromZero);

                orderItem.Product.CurrentPrice = decimal.Round(orderItem.PricePerItem, 2, MidpointRounding.AwayFromZero);
                orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                decimal vatPercent = sale.SaleInvoiceDocument?.Vat ?? 23m;

                decimal currentVatAmount = decimal.Round(orderItem.TotalAmount * vatPercent / 100, 2, MidpointRounding.AwayFromZero);
                decimal currentVatAmountLocal = decimal.Round(orderItem.TotalAmountLocal * vatPercent / 100, 2, MidpointRounding.AwayFromZero);

                sale.VatAmount = decimal.Round(sale.VatAmount + currentVatAmount, 2, MidpointRounding.AwayFromZero);
                sale.VatAmountPln = decimal.Round(sale.VatAmountPln + currentVatAmountLocal, 2, MidpointRounding.AwayFromZero);
            }

            sale.Order.TotalAmount = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmount), 2, MidpointRounding.AwayFromZero);
            sale.Order.TotalAmountLocal = decimal.Round(sale.Order.OrderItems.Sum(o => o.TotalAmountLocal), 2, MidpointRounding.AwayFromZero);
            sale.Order.TotalCount = sale.Order.OrderItems.Sum(o => o.Qty);

            sale.TotalAmount = sale.Order.TotalAmount;
            sale.TotalAmountLocal = sale.Order.TotalAmountLocal;
            sale.TotalCount = sale.Order.TotalCount;
        });
    }
}