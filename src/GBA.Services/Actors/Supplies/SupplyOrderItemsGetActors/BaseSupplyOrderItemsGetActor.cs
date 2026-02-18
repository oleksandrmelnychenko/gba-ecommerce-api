using System.Collections.Generic;
using System.Data;
using Akka.Actor;
using GBA.Domain.AuditEntities;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Messages.Supplies;
using GBA.Domain.Repositories.Auditing.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Services.Actors.Supplies.SupplyOrderItemsGetActors;

public sealed class BaseSupplyOrderItemsGetActor : ReceiveActor {
    private readonly IAuditRepositoriesFactory _auditRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;

    public BaseSupplyOrderItemsGetActor(
        IDbConnectionFactory connectionFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IAuditRepositoriesFactory auditRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _auditRepositoriesFactory = auditRepositoriesFactory;

        Receive<GetAllSupplyOrderItemsBySupplyOrderNetIdMessage>(ProcessGetAllSupplyOrderItemsBySupplyOrderNetIdMessage);

        Receive<GetAllSupplyOrderItemsMessage>(ProcessGetAllSupplyOrderItemsMessage);

        Receive<GetSupplyOrderItemChangeHistoryByNetIdMessage>(ProcessGetSupplyOrderItemChangeHistoryByNetIdMessage);
    }

    private void ProcessGetAllSupplyOrderItemsBySupplyOrderNetIdMessage(GetAllSupplyOrderItemsBySupplyOrderNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory
            .NewSupplyOrderItemRepository(connection)
            .GetAllBySupplyOrderNetId(message.NetId)
        );
    }

    private void ProcessGetAllSupplyOrderItemsMessage(GetAllSupplyOrderItemsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory
            .NewSupplyOrderItemRepository(connection)
            .GetAll()
        );
    }

    private void ProcessGetSupplyOrderItemChangeHistoryByNetIdMessage(GetSupplyOrderItemChangeHistoryByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        SupplyOrderItem supplyOrderItem = _supplyRepositoriesFactory.NewSupplyOrderItemRepository(connection).GetByNetId(message.NetId);

        Sender.Tell(
            !supplyOrderItem.Equals(null)
                ? _auditRepositoriesFactory.NewAuditRepository(connection).GetProductChangeHistoryByNetUid(supplyOrderItem.Product.NetUid)
                : new List<AuditEntity>()
        );
    }
}