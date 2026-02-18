using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Translations.PricingTranslations;
using GBA.Domain.Repositories.Pricings.Contracts;

namespace GBA.Services.Actors.Translations;

public sealed class PricingTranslationsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPricingRepositoriesFactory _pricingRepositoriesFactory;

    public PricingTranslationsActor(
        IDbConnectionFactory connectionFactory,
        IPricingRepositoriesFactory pricingRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _pricingRepositoriesFactory = pricingRepositoriesFactory;

        Receive<AddPricingTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IPricingTranslationRepository pricingTranslationRepository = _pricingRepositoriesFactory.NewPricingTranslationRepository(connection);

            message.PricingTranslation.PricingId = message.PricingTranslation.Pricing.Id;

            Sender.Tell(pricingTranslationRepository.GetById(pricingTranslationRepository.Add(message.PricingTranslation)));
        });

        Receive<UpdatePricingTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IPricingTranslationRepository pricingTranslationRepository = _pricingRepositoriesFactory.NewPricingTranslationRepository(connection);

            pricingTranslationRepository.Update(message.PricingTranslation);

            Sender.Tell(pricingTranslationRepository.GetByNetId(message.PricingTranslation.NetUid));
        });

        Receive<GetAllPricingTranslationsMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_pricingRepositoriesFactory.NewPricingTranslationRepository(connection).GetAll());
        });

        Receive<GetPricingTranslationByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_pricingRepositoriesFactory.NewPricingTranslationRepository(connection).GetByNetId(message.NetId));
        });

        Receive<DeletePricingTranslationMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _pricingRepositoriesFactory.NewPricingTranslationRepository(connection).Remove(message.NetId);
        });
    }
}