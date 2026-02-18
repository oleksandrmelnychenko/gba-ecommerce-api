using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Translations.PriceTypeTranslations;
using GBA.Domain.Repositories.Pricings.Contracts;

namespace GBA.Services.Actors.Translations;

public sealed class PriceTypeTranslationsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPricingRepositoriesFactory _pricingRepositoriesFactory;

    public PriceTypeTranslationsActor(
        IDbConnectionFactory connectionFactory,
        IPricingRepositoriesFactory pricingRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _pricingRepositoriesFactory = pricingRepositoriesFactory;

        Receive<AddPriceTypeTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IPriceTypeTranslationRepository priceTypeTranslationRepository = _pricingRepositoriesFactory.NewPriceTypeTranslationRepository(connection);

            message.PriceTypeTranslation.PriceTypeId = message.PriceTypeTranslation.PriceType.Id;

            Sender.Tell(priceTypeTranslationRepository.GetById(priceTypeTranslationRepository.Add(message.PriceTypeTranslation)));
        });

        Receive<UpdatePriceTypeTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IPriceTypeTranslationRepository priceTypeTranslationRepository = _pricingRepositoriesFactory.NewPriceTypeTranslationRepository(connection);

            priceTypeTranslationRepository.Update(message.PriceTypeTranslation);

            Sender.Tell(priceTypeTranslationRepository.GetByNetId(message.PriceTypeTranslation.NetUid));
        });

        Receive<GetAllPriceTypeTranslationsMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_pricingRepositoriesFactory.NewPriceTypeTranslationRepository(connection).GetAll());
        });

        Receive<GetPriceTypeTranslationByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_pricingRepositoriesFactory.NewPriceTypeTranslationRepository(connection).GetByNetId(message.NetId));
        });

        Receive<DeletePriceTypeTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _pricingRepositoriesFactory.NewPriceTypeTranslationRepository(connection).Remove(message.NetId);
        });
    }
}