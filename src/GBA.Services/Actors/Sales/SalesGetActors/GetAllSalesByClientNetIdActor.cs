using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.SalesModels.Models;
using GBA.Domain.Messages.Sales;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.SaleReturns.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Services.Actors.Sales.SalesGetActors;

public sealed class GetAllSalesByClientNetIdActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly ISaleReturnRepositoriesFactory _saleReturnRepositoriesFactory;

    public GetAllSalesByClientNetIdActor(
        IDbConnectionFactory connectionFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        ISaleReturnRepositoriesFactory saleReturnRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _saleReturnRepositoriesFactory = saleReturnRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;

        Receive<GetAllSalesByClientNetIdMessage>(ProcessGetAllSalesByClientNetIdMessage);

        Receive<GetSalesRegisterByClientNetIdMessage>(ProcessGetSalesRegisterByClientNetIdMessage);
    }

    private void ProcessGetSalesRegisterByClientNetIdMessage(GetSalesRegisterByClientNetIdMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISaleExchangeRateRepository saleExchangeRateRepository = _saleRepositoriesFactory.NewSaleExchangeRateRepository(connection);
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            List<SalesRegisterModel> salesRegister = saleRepository.GetAllSalesWithReturnsByClientNetIdFiltered(message);
            List<Sale> sales = salesRegister.Where(s => s.SaleStatistic != null).Select(e => e.SaleStatistic.Sale).ToList();

            SaleActorsHelpers.CalculatePricingsForSalesWithDynamicPrices(sales, _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
                _currencyRepositoriesFactory.NewCurrencyRepository(connection));

            sales.ForEach(sale => {
                List<SaleExchangeRate> saleExchangeRates = saleExchangeRateRepository.GetAllBySaleNetId(sale.NetUid);

                dynamic[] lifeCycleLine = new dynamic[LifeCycleLineStatuses.STATUSES.Length];

                SaleActorsHelpers.FormLifeCycleLine(saleRepository, sale.NetUid, lifeCycleLine);

                SalesRegisterModel salesRegisterModel = salesRegister.First(e => e.SaleStatistic.Sale.Id.Equals(sale.Id));
                salesRegisterModel.SaleStatistic.SaleExchangeRates = saleExchangeRates;
                salesRegisterModel.SaleStatistic.LifeCycleLine = lifeCycleLine.ToList();
            });

            Sender.Tell(salesRegister);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetAllSalesByClientNetIdMessage(GetAllSalesByClientNetIdMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISaleExchangeRateRepository saleExchangeRateRepository = _saleRepositoriesFactory.NewSaleExchangeRateRepository(connection);
            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

            List<SaleStatistic> saleStatistics = new();

            List<Sale> sales = _saleRepositoriesFactory.NewSaleRepository(connection).GetAllByClientNetIdFiltered(message);

            SaleActorsHelpers.CalculatePricingsForSalesWithDynamicPricesWithUsdCalculations(sales, _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
                _currencyRepositoriesFactory.NewCurrencyRepository(connection));

            sales.ForEach(sale => {
                List<SaleExchangeRate> saleExchangeRates = saleExchangeRateRepository.GetAllBySaleNetId(sale.NetUid);

                dynamic[] lifeCycleLine = new dynamic[LifeCycleLineStatuses.STATUSES.Length];

                SaleActorsHelpers.FormLifeCycleLine(saleRepository, sale.NetUid, lifeCycleLine);

                SaleStatistic saleStatistic = new() {
                    Sale = sale,
                    SaleExchangeRates = saleExchangeRates,
                    LifeCycleLine = lifeCycleLine.ToList()
                };

                saleStatistics.Add(saleStatistic);
            });

            Sender.Tell(saleStatistics);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}