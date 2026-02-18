using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Pricings;
using GBA.Domain.Repositories.Pricings.Contracts;

namespace GBA.Services.Actors.Pricings;

public sealed class ProviderPricingsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPricingRepositoriesFactory _pricingRepositoriesFactory;

    public ProviderPricingsActor(
        IDbConnectionFactory connectionFactory,
        IPricingRepositoriesFactory pricingRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _pricingRepositoriesFactory = pricingRepositoriesFactory;

        Receive<AddProviderPricingMessage>(ProcessAddProviderPricingMessage);

        Receive<UpdateProviderPricingMessage>(ProcessUpdateProviderPricingMessage);

        Receive<GetAllProviderPricingsMessage>(ProcessGetAllProviderPricingsMessage);

        Receive<GetProviderPricingByNetIdMessage>(ProcessGetProviderPricingByNetIdMessage);

        Receive<DeleteProviderPricingMessage>(ProcessDeleteProviderPricingMessage);
    }

    private void ProcessAddProviderPricingMessage(AddProviderPricingMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IProviderPricingRepository providerPricingRepository = _pricingRepositoriesFactory.NewProviderPricingRepository(connection);

        message.ProviderPricing.BasePricingId = message.ProviderPricing.Pricing.Id;
        message.ProviderPricing.CurrencyId = message.ProviderPricing.Currency.Id;

        long pricingId = providerPricingRepository.Add(message.ProviderPricing);

        Sender.Tell(providerPricingRepository.GetById(pricingId));
    }

    private void ProcessUpdateProviderPricingMessage(UpdateProviderPricingMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IProviderPricingRepository providerPricingRepository = _pricingRepositoriesFactory.NewProviderPricingRepository(connection);

        providerPricingRepository.Update(message.ProviderPricing);

        Sender.Tell(providerPricingRepository.GetByNetId(message.ProviderPricing.NetUid));
    }

    private void ProcessGetAllProviderPricingsMessage(GetAllProviderPricingsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_pricingRepositoriesFactory.NewProviderPricingRepository(connection).GetAll());
    }

    private void ProcessGetProviderPricingByNetIdMessage(GetProviderPricingByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_pricingRepositoriesFactory.NewProviderPricingRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessDeleteProviderPricingMessage(DeleteProviderPricingMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _pricingRepositoriesFactory.NewProviderPricingRepository(connection).Remove(message.NetId);
    }
}