using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using GBA.Domain.Messages.Logging;
using GBA.Domain.Messages.Sales.OrderItems;
using GBA.Domain.Messages.SchedulerTasks;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using Newtonsoft.Json;

namespace GBA.Services.Actors.SchedulerTasks;

public sealed class DeleteOldBillSalesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public DeleteOldBillSalesActor(
        IDbConnectionFactory connectionFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IUserRepositoriesFactory userRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _userRepositoriesFactory = userRepositoriesFactory;

        ReceiveAsync<InitiateCloseExpiredOrdersMessage>(ProcessDeleteOldBillSalesMessage);
    }

    private async Task ProcessDeleteOldBillSalesMessage(InitiateCloseExpiredOrdersMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();

            ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);

            User gbaUser = userRepository.GetGbaUser();

            IEnumerable<Sale> allExpiredOrders = saleRepository.GetAllExpiredOrders();
            ActorReferenceManager
                .Instance
                .Get(BaseActorNames.LOG_MANAGER_ACTOR)
                .Tell(
                    new AddExpiredBillLogMessage(
                        $"Found orders to close: {allExpiredOrders.Count()}",
                        string.Empty
                    )
                );

            foreach (Sale sale in allExpiredOrders) {
                Sale saleFromDb = saleRepository.GetByNetId(sale.NetUid);

                foreach (OrderItem orderItem in saleFromDb.Order.OrderItems)
                    orderItem.ShiftStatuses = new List<OrderItemBaseShiftStatus> {
                        new() { Qty = orderItem.Qty }
                    };

                object result = await ActorReferenceManager.Instance.Get(SalesActorNames.ORDER_ITEMS_ACTOR).Ask<object>(
                    new CloseExpiredOrdersMessage(saleFromDb, gbaUser.NetUid));

                if (result is Exception exc)
                    ActorReferenceManager
                        .Instance
                        .Get(BaseActorNames.LOG_MANAGER_ACTOR)
                        .Tell(
                            new AddExpiredBillLogMessage(
                                "SALE_EXCEPTION",
                                JsonConvert.SerializeObject(new {
                                    exc.Message,
                                    exc.StackTrace
                                })
                            )
                        );
                else
                    ActorReferenceManager
                        .Instance
                        .Get(BaseActorNames.LOG_MANAGER_ACTOR)
                        .Tell(
                            new AddExpiredBillLogMessage(
                                $"Sale.NetUID: {sale.NetUid}, Sale.Created: {sale.Created} returned to storage at {DateTime.UtcNow} UTC",
                                string.Empty
                            )
                        );
            }
        } catch (Exception exc) {
            ActorReferenceManager
                .Instance
                .Get(BaseActorNames.LOG_MANAGER_ACTOR)
                .Tell(
                    new AddExpiredBillLogMessage(
                        "SALE_EXCEPTION",
                        JsonConvert.SerializeObject(new {
                            exc.Message,
                            exc.StackTrace
                        })
                    )
                );
        }
    }
}