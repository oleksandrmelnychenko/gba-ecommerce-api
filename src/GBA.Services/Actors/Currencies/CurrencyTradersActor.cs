using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Messages.Currencies.CurrencyTraders;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Services.Actors.Currencies;

public sealed class CurrencyTradersActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;

    public CurrencyTradersActor(
        IDbConnectionFactory connectionFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;

        Receive<AddNewCurrencyTraderMessage>(ProcessAddNewCurrencyTraderMessage);

        Receive<UpdateCurrencyTraderMessage>(ProcessUpdateCurrencyTraderMessage);

        Receive<GetAllCurrencyTradersMessage>(ProcessGetAllCurrencyTradersMessage);

        Receive<GetCurrencyTraderByNetIdMessage>(ProcessGetCurrencyTraderByNetIdMessage);

        Receive<FindCurrencyTradersByPaymentCurrencyRegisterNetIdMessage>(ProcessFindCurrencyTradersByPaymentCurrencyRegisterNetIdMessage);

        Receive<GetAllCurrencyTraderExchangeRatesByTraderNetIdFilteredMessage>(ProcessGetAllCurrencyTraderExchangeRatesByTraderNetIdFilteredMessage);

        Receive<GetAllCurrencyTradersFromSearchMessage>(ProcessGetAllCurrencyTradersFromSearchMessage);

        Receive<DeleteCurrencyTraderByNetIdMessage>(ProcessDeleteCurrencyTraderByNetIdMessage);
    }

    private void ProcessAddNewCurrencyTraderMessage(AddNewCurrencyTraderMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ICurrencyTraderRepository currencyTraderRepository = _currencyRepositoriesFactory.NewCurrencyTraderRepository(connection);

        message.CurrencyTrader.Id = currencyTraderRepository.Add(message.CurrencyTrader);

        if (message.CurrencyTrader.CurrencyTraderExchangeRates.Any())
            _currencyRepositoriesFactory
                .NewCurrencyTraderExchangeRateRepository(connection)
                .Add(
                    message
                        .CurrencyTrader
                        .CurrencyTraderExchangeRates
                        .Select(r => {
                            r.CurrencyTraderId = message.CurrencyTrader.Id;

                            if (r.FromDate.Year.Equals(1)) r.FromDate = DateTime.Now;

                            return r;
                        })
                );

        Sender.Tell(currencyTraderRepository.GetById(message.CurrencyTrader.Id));
    }

    private void ProcessUpdateCurrencyTraderMessage(UpdateCurrencyTraderMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ICurrencyTraderRepository currencyTraderRepository = _currencyRepositoriesFactory.NewCurrencyTraderRepository(connection);
        ICurrencyTraderExchangeRateRepository currencyTraderExchangeRateRepository =
            _currencyRepositoriesFactory
                .NewCurrencyTraderExchangeRateRepository(connection);

        currencyTraderRepository.Update(message.CurrencyTrader);

        if (message.CurrencyTrader.CurrencyTraderExchangeRates.Any(r => r.IsNew() && !r.Deleted))
            currencyTraderExchangeRateRepository
                .Add(
                    message
                        .CurrencyTrader
                        .CurrencyTraderExchangeRates
                        .Where(r => r.IsNew() && !r.Deleted)
                        .Select(r => {
                            r.CurrencyTraderId = message.CurrencyTrader.Id;

                            if (r.FromDate.Year.Equals(1)) r.FromDate = DateTime.Now;

                            return r;
                        })
                );

        if (message.CurrencyTrader.CurrencyTraderExchangeRates.Any(r => !r.IsNew() && !r.Deleted))
            currencyTraderExchangeRateRepository
                .Update(
                    message
                        .CurrencyTrader
                        .CurrencyTraderExchangeRates
                        .Where(r => !r.IsNew() && !r.Deleted)
                );

        if (message.CurrencyTrader.CurrencyTraderExchangeRates.Any(r => !r.IsNew() && r.Deleted))
            foreach (CurrencyTraderExchangeRate exchangeRate in message
                         .CurrencyTrader
                         .CurrencyTraderExchangeRates
                         .Where(r => !r.IsNew() && r.Deleted))
                currencyTraderExchangeRateRepository.RemoveByFromDateAndTraderId(exchangeRate.FromDate, exchangeRate.CurrencyTraderId);

        Sender.Tell(currencyTraderRepository.GetById(message.CurrencyTrader.Id));
    }

    private void ProcessGetAllCurrencyTradersMessage(GetAllCurrencyTradersMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_currencyRepositoriesFactory.NewCurrencyTraderRepository(connection).GetAll());
    }

    private void ProcessGetCurrencyTraderByNetIdMessage(GetCurrencyTraderByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_currencyRepositoriesFactory.NewCurrencyTraderRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessFindCurrencyTradersByPaymentCurrencyRegisterNetIdMessage(FindCurrencyTradersByPaymentCurrencyRegisterNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        PaymentCurrencyRegister paymentCurrencyRegister = _paymentOrderRepositoriesFactory.NewPaymentCurrencyRegisterRepository(connection).GetByNetId(message.NetId);

        Sender.Tell(paymentCurrencyRegister != null
            ? _currencyRepositoriesFactory.NewCurrencyTraderRepository(connection).FindByPaymentCurrencyRegisterNetId(paymentCurrencyRegister.Currency.Code)
            : new List<CurrencyTrader>());
    }

    private void ProcessGetAllCurrencyTraderExchangeRatesByTraderNetIdFilteredMessage(GetAllCurrencyTraderExchangeRatesByTraderNetIdFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        CurrencyTrader currencyTrader = _currencyRepositoriesFactory.NewCurrencyTraderRepository(connection).GetByNetId(message.NetId);

        if (currencyTrader != null) {
            message.To = message.To.AddHours(23).AddMinutes(59).AddSeconds(59);

            Sender.Tell(_currencyRepositoriesFactory.NewCurrencyTraderExchangeRateRepository(connection)
                .GetAllByTraderIdFiltered(currencyTrader.Id, message.From, message.To));
        } else {
            Sender.Tell(new List<CurrencyTraderExchangeRate>());
        }
    }

    private void ProcessGetAllCurrencyTradersFromSearchMessage(GetAllCurrencyTradersFromSearchMessage message) {
        message.Value = string.IsNullOrEmpty(message.Value) ? "%" : $"%{message.Value.Trim().Replace(' ', '%')}%";

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_currencyRepositoriesFactory.NewCurrencyTraderRepository(connection).GetAllFromSearch(message.Value));
    }

    private void ProcessDeleteCurrencyTraderByNetIdMessage(DeleteCurrencyTraderByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _currencyRepositoriesFactory.NewCurrencyTraderRepository(connection).Remove(message.NetId);
    }
}