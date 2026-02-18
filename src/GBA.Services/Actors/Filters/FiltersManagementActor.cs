using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Filters;
using GBA.Domain.Repositories.Filters.Contracts;

namespace GBA.Services.Actors.Filters;

public sealed class FiltersManagementActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IFilterItemRepositoriesFactory _filterItemRepositoriesFactory;

    public FiltersManagementActor(
        IDbConnectionFactory connectionFactory,
        IFilterItemRepositoriesFactory filterItemRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _filterItemRepositoriesFactory = filterItemRepositoriesFactory;

        Receive<GetAllFilterItemsMessage>(ProcessGetAllFilterItemsMessage);

        Receive<GetAllFilterOperationItemsMessage>(ProcessGetAllFilterOperationItemsMessage);

        Receive<GetClientTypeRoleFilterItemMessage>(ProcessGetClientTypeRoleFilterItemMessage);
    }

    private void ProcessGetAllFilterItemsMessage(GetAllFilterItemsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_filterItemRepositoriesFactory.NewFilterItemRepository(connection).GetAllByType(message.Type));
    }

    private void ProcessGetAllFilterOperationItemsMessage(GetAllFilterOperationItemsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_filterItemRepositoriesFactory.NewFilterOperationItemRepository(connection).GetAll());
    }

    private void ProcessGetClientTypeRoleFilterItemMessage(GetClientTypeRoleFilterItemMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_filterItemRepositoriesFactory
            .NewFilterItemRepository(connection)
            .GetClientTypeRoleFilterItem()
        );
    }
}