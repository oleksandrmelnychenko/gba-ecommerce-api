using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Messages.ColumnItems;
using GBA.Domain.Repositories.ColumnItems.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.ColumnItems;

public sealed class ColumnItemsActor : ReceiveActor {
    private readonly IColumnItemRepositoryFactory _columnItemRepositoryFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public ColumnItemsActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IColumnItemRepositoryFactory columnItemRepositoryFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _columnItemRepositoryFactory = columnItemRepositoryFactory;

        Receive<AddColumnItemMessage>(ProcessAddColumnItemMessage);

        Receive<UpdateColumnItemMessage>(ProcessUpdateColumnItemMessage);

        Receive<GetAllColumnItemsByTypeAndUserNetIdMessage>(ProcessGetAllColumnItemsByTypeAndUserNetIdMessage);

        Receive<GetColumnItemByNetId>(ProcessGetColumnItemByNetId);

        Receive<DeleteColumnItemMessage>(ProcessDeleteColumnItemMessage);
    }

    private void ProcessAddColumnItemMessage(AddColumnItemMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IColumnItemRepository columnItemRepository = _columnItemRepositoryFactory.NewColumnItemRepository(connection);

        User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.NetId);

        message.ColumnItem.UserId = user.Id;

        long columnItemId = columnItemRepository.Add(message.ColumnItem);

        Sender.Tell(columnItemRepository.GetById(columnItemId));
    }

    private void ProcessUpdateColumnItemMessage(UpdateColumnItemMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IColumnItemRepository columnItemRepository = _columnItemRepositoryFactory.NewColumnItemRepository(connection);

        columnItemRepository.Update(message.ColumnItem);

        Sender.Tell(columnItemRepository.GetByNetId(message.ColumnItem.NetUid));
    }

    private void ProcessGetAllColumnItemsByTypeAndUserNetIdMessage(GetAllColumnItemsByTypeAndUserNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IColumnItemRepository columnItemRepository = _columnItemRepositoryFactory.NewColumnItemRepository(connection);

        User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.NetId);

        Sender.Tell(columnItemRepository.GetAllByTypeAndUserId(message.Type, user.Id));
    }

    private void ProcessGetColumnItemByNetId(GetColumnItemByNetId message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_columnItemRepositoryFactory.NewColumnItemRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessDeleteColumnItemMessage(DeleteColumnItemMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _columnItemRepositoryFactory.NewColumnItemRepository(connection).Remove(message.NetId);
    }
}