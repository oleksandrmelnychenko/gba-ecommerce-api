using System;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Messages.Currencies;
using GBA.Domain.Repositories.Currencies.Contracts;

namespace GBA.Services.Actors.Currencies;

public sealed class CurrenciesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;

    public CurrenciesActor(
        IDbConnectionFactory connectionFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;

        Receive<AddCurrencyMessage>(ProcessAddCurrencyMessage);

        Receive<UpdateCurrencyMessage>(ProcessUpdateCurrencyMessage);

        Receive<DeleteCurrencyMessage>(ProcessDeleteCurrencyMessage);
    }

    private void ProcessAddCurrencyMessage(AddCurrencyMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(connection);

        long currencyId = currencyRepository.Add(message.Currency);

        if (message.Currency.CurrencyTranslations.Any(t => !string.IsNullOrEmpty(t.Name))) {
            message.Currency.CurrencyTranslations.ToList().ForEach(t => t.CurrencyId = currencyId);

            _currencyRepositoriesFactory.NewCurrencyTranslationRepository(connection).Add(message.Currency.CurrencyTranslations);
        }

        Sender.Tell(currencyRepository.GetById(currencyId));
    }

    private void ProcessUpdateCurrencyMessage(UpdateCurrencyMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(connection);

        currencyRepository.Update(message.Currency);

        if (message.Currency.CurrencyTranslations.Any(t => !string.IsNullOrEmpty(t.Name))) {
            ICurrencyTranslationRepository currencyTranslationRepository = _currencyRepositoriesFactory.NewCurrencyTranslationRepository(connection);

            message.Currency.CurrencyTranslations.ToList().ForEach(t => {
                if (t.IsNew()) {
                    t.CurrencyId = message.Currency.Id;

                    currencyTranslationRepository.Add(t);
                } else {
                    currencyTranslationRepository.Update(t);
                }
            });
        }

        Sender.Tell(currencyRepository.GetByNetId(message.Currency.NetUid));
    }

    private void ProcessDeleteCurrencyMessage(DeleteCurrencyMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ICurrencyRepository currencyRepository = _currencyRepositoriesFactory.NewCurrencyRepository(connection);

        Currency currency = currencyRepository.GetByNetId(message.NetId);

        if (currency != null) {
            if (currencyRepository.IsCurrencyAttachedToAnyPricing(currency.Id)) {
                Sender.Tell(new Tuple<string, Currency>(CurrencyResourceNames.ATTACHED_TO_PRICING, null));
            } else if (currencyRepository.IsCurrencyAttachedToAnyAgreement(currency.Id)) {
                Sender.Tell(new Tuple<string, Currency>(CurrencyResourceNames.ATTACHED_TO_AGREEMENT, null));
            } else {
                currencyRepository.Remove(message.NetId);

                Sender.Tell(new Tuple<string, Currency>(string.Empty, currency));
            }
        } else {
            Sender.Tell(new Tuple<string, Currency>(string.Empty, null));
        }
    }
}