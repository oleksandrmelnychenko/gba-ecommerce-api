using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Messages.Sales;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Services.Actors.Sales.SalesGetActors;

public sealed class GetSaleByNetIdActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;

    public GetSaleByNetIdActor(
        IDbConnectionFactory connectionFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;

        Receive<GetSaleByNetIdMessage>(ProcessGetSaleByNetIdMessage);
    }

    private void ProcessGetSaleByNetIdMessage(GetSaleByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);

        Sale saleFromDb = saleRepository.GetByNetId(message.NetId);

        SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPrices(saleFromDb, _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
            _currencyRepositoriesFactory.NewCurrencyRepository(connection));

        dynamic[] toReturnData = new dynamic[LifeCycleLineStatuses.STATUSES.Length];

        SaleActorsHelpers.FormLifeCycleLine(saleRepository, saleFromDb.NetUid, toReturnData);

        List<SaleExchangeRate> saleExchangeRates = _saleRepositoriesFactory.NewSaleExchangeRateRepository(connection).GetAllBySaleNetId(saleFromDb.NetUid);

        SaleStatistic saleInfo = new() {
            Sale = saleFromDb,
            LifeCycleLine = toReturnData.ToList(),
            SaleExchangeRates = saleExchangeRates
        };

        Sender.Tell(saleInfo);
    }
}