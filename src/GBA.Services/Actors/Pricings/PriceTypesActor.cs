using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Pricings;
using GBA.Domain.Repositories.Pricings.Contracts;

namespace GBA.Services.Actors.Pricings;

public sealed class PriceTypesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPricingRepositoriesFactory _pricingRepositoriesFactory;

    public PriceTypesActor(
        IDbConnectionFactory connectionFactory,
        IPricingRepositoriesFactory pricingRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _pricingRepositoriesFactory = pricingRepositoriesFactory;

        Receive<GetAllPriceCalculationTypes>(ProcessGetAllPriceCalculationTypes);
    }

    private void ProcessGetAllPriceCalculationTypes(GetAllPriceCalculationTypes message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_pricingRepositoriesFactory.NewPriceTypeRepository(connection).GetAll());
    }
}
