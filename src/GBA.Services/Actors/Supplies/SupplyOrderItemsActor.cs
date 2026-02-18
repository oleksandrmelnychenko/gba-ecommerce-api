using System;
using System.Collections.Generic;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Messages.Auditing;
using GBA.Domain.Messages.Supplies;
using GBA.Domain.Repositories.Auditing.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Supplies;

public sealed class SupplyOrderItemsActor : ReceiveActor {
    private readonly IAuditRepositoriesFactory _auditRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;

    public SupplyOrderItemsActor(
        IDbConnectionFactory connectionFactory,
        IAuditRepositoriesFactory auditRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _auditRepositoriesFactory = auditRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;

        Receive<UpdateAllSupplyOrderItemsMessage>(ProcessUpdateAllSupplyOrderItemsMessage);

        Receive<AddAllSupplyOrderItemsMessage>(ProcessAddAllSupplyOrderItemsMessage);

        Receive<RestoreDefaultTestDataMessage>(ProcessRestoreDefaultTestDataMessage);

        Receive<UpdateSupplyOrderItemMessage>(ProcessUpdateSupplyOrderItemMessage);
    }

    private void ProcessUpdateAllSupplyOrderItemsMessage(UpdateAllSupplyOrderItemsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyOrderItemRepository supplyOrderItemRepository = _supplyRepositoriesFactory.NewSupplyOrderItemRepository(connection);

        supplyOrderItemRepository.Update(message.SupplyOrderItems);

        Sender.Tell(supplyOrderItemRepository.GetAll());
    }

    private void ProcessAddAllSupplyOrderItemsMessage(AddAllSupplyOrderItemsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _supplyRepositoriesFactory.NewSupplyOrderItemRepository(connection).Add(message.SupplyOrderItems);
    }

    private void ProcessRestoreDefaultTestDataMessage(RestoreDefaultTestDataMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyOrderItemRepository supplyOrderItemRepository = _supplyRepositoriesFactory.NewSupplyOrderItemRepository(connection);

        List<SupplyOrderItem> items = supplyOrderItemRepository.GetAll();

        items.ForEach(item => {
            Random r = new();

            item.ItemNo = r.Next(10000, 100000).ToString();
            item.StockNo = r.Next(10000, 100000).ToString();
            item.NetWeight = r.Next(15, 250);
            item.GrossWeight = item.NetWeight + (double)r.Next(1, 25) * r.Next(1, 99) / 100;
            item.UnitPrice = r.Next(10, 500);
            item.Qty = r.Next(99);
            item.TotalAmount = Convert.ToDecimal(item.Qty) * item.UnitPrice;
        });

        supplyOrderItemRepository.Update(items);

        Sender.Tell(supplyOrderItemRepository.GetAll());
    }

    private void ProcessUpdateSupplyOrderItemMessage(UpdateSupplyOrderItemMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (message.SupplyOrderItem.Equals(null)) {
            Sender.Tell(new Tuple<SupplyOrderItem, string>(null, "SupplyOrderItem entity can not be null"));
        } else if (message.SupplyOrderItem.IsNew()) {
            Sender.Tell(new Tuple<SupplyOrderItem, string>(null, "New SupplyOrderItem is not valid input for current request"));
        } else {
            ISupplyOrderItemRepository supplyOrderItemRepository = _supplyRepositoriesFactory.NewSupplyOrderItemRepository(connection);

            SupplyOrderItem supplyOrderItemFromDb = supplyOrderItemRepository.GetByNetId(message.SupplyOrderItem.NetUid);

            ActorReferenceManager.Instance.Get(BaseActorNames.AUDIT_MANAGEMENT_ACTOR).Tell(
                new RetrieveAndStoreAuditDataMessage(
                    message.UpdatedByNetId,
                    supplyOrderItemFromDb.Product.NetUid,
                    "Product",
                    message.SupplyOrderItem,
                    supplyOrderItemFromDb
                )
            );

            message.SupplyOrderItem.TotalAmount = Convert.ToDecimal(message.SupplyOrderItem.Qty) * message.SupplyOrderItem.UnitPrice;

            supplyOrderItemRepository.Update(message.SupplyOrderItem);

            Sender.Tell(new Tuple<SupplyOrderItem, string>(supplyOrderItemRepository.GetByNetId(message.SupplyOrderItem.NetUid), string.Empty));
        }
    }
}