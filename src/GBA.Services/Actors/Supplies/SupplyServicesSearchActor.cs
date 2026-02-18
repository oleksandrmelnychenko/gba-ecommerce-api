using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Supplies;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Services.Actors.Supplies;

public sealed class SupplyServicesSearchActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;

    public SupplyServicesSearchActor(
        IDbConnectionFactory connectionFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;

        Receive<SearchForOrganizationMessage>(ProcessSearchForOrganizationMessage);

        Receive<SearchForPaymentTasksByOrganizationAndServiceTypesMessage>(ProcessSearchForPaymentTasksByOrganizationAndServiceTypesMessage);
    }

    private void ProcessSearchForOrganizationMessage(SearchForOrganizationMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory.NewSupplyServicesSearchRepository(connection).SearchForServiceOrganizations(message.Value));
    }

    private void ProcessSearchForPaymentTasksByOrganizationAndServiceTypesMessage(SearchForPaymentTasksByOrganizationAndServiceTypesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyRepositoriesFactory
                .NewSupplyServicesSearchRepository(connection)
                .GetPaymentTasksFromSearchByOrganizationsAndServices(
                    message.OrganizationName,
                    message.ServiceTypes,
                    message.From,
                    message.To
                )
        );
    }
}