using System;
using System.Collections.Generic;
using System.Data;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Messages.Sales;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Services.Actors.Sales.SalesGetActors;

public sealed class GetSalesFilteredActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;

    public GetSalesFilteredActor(
        IDbConnectionFactory connectionFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;

        Receive<GetSalesFilteredMessage>(ProcessGetSalesFilteredMessage);
    }

    private void ProcessGetSalesFilteredMessage(GetSalesFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            List<Sale> sales =
                _saleRepositoriesFactory
                    .NewSaleRepository(connection)
                    .GetAllRangedByLifeCycleType(
                        message.Limit,
                        message.Offset,
                        message.ClientId,
                        message.OrganisationIds,
                        message.SaleLifeCycleType,
                        message.From ?? DateTime.Now.Date,
                        message.To ?? DateTime.Now,
                        message.Type.Equals(QueryType.Self) || message.Type.Equals(QueryType.By) ? message.UserNetId : null,
                        message.Value,
                        message.FromShipments,
                        forEcommerce: message.ForEcommerce,
                        fastEcommerce: message.FastEcommerce
                    );

            SaleActorsHelpers.CalculatePricingsForSalesWithDynamicPrices(sales, _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
                _currencyRepositoriesFactory.NewCurrencyRepository(connection));

            Sender.Tell(sales);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}