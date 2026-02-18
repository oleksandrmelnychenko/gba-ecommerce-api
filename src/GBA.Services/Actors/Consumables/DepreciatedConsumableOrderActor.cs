using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Messages.Consumables.Orders.Depreciated;
using GBA.Domain.Repositories.Consumables.Contracts;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Consumables;

public sealed class DepreciatedConsumableOrderActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsumablesRepositoriesFactory _consumablesRepositoriesFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public DepreciatedConsumableOrderActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IConsumablesRepositoriesFactory consumablesRepositoriesFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _consumablesRepositoriesFactory = consumablesRepositoriesFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;

        Receive<AddNewDepreciatedConsumableOrderMessage>(message => {
            if (message.DepreciatedConsumableOrder == null) {
                Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(null, "Empty DepreciatedConsumableOrder is not valid input for current request"));
            } else if (!message.DepreciatedConsumableOrder.IsNew()) {
                Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(null, "Existing DepreciatedConsumableOrder is not valid input for current request"));
            } else if (message.DepreciatedConsumableOrder.ConsumablesStorage == null && message.DepreciatedConsumableOrder.ConsumablesStorageId.Equals(0)) {
                Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(null, DepreciatedConsumableOrderResourceNames.CONSUMABLES_STORAGE_NOT_SPECIFIED));
            } else if (message.DepreciatedConsumableOrder.DepreciatedTo == null && message.DepreciatedConsumableOrder.DepreciatedToId.Equals(0)) {
                Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(null, DepreciatedConsumableOrderResourceNames.DEPRECIATED_TO_USER_NOT_SPECIFIED));
            } else if (message.DepreciatedConsumableOrder.CommissionHead == null && message.DepreciatedConsumableOrder.CommissionHeadId.Equals(0)) {
                Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(null, DepreciatedConsumableOrderResourceNames.COMMISSION_HEAD_USER_NOT_SPECIFIED));
            } else if (message.DepreciatedConsumableOrder.DepreciatedConsumableOrderItems.Any(i => i.Qty <= 0)) {
                Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(null, DepreciatedConsumableOrderResourceNames.ITEMS_QTY_SHOULD_BE_MORE_THAN_ZERO));
            } else if (message.DepreciatedConsumableOrder.DepreciatedConsumableOrderItems.Any(i =>
                           i.ConsumablesOrderItem?.ConsumableProduct == null)) {
                Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(null, DepreciatedConsumableOrderResourceNames.CONSUMABLE_PRODUCT_NOT_SPECIFIED));
            } else if (message.DepreciatedConsumableOrder.DepreciatedConsumableOrderItems.Any(i =>
                           i.PaymentCostMovementOperation?.PaymentCostMovement == null)) {
                Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(null, DepreciatedConsumableOrderResourceNames.PAYMENT_COST_MOVEMENT_NOT_SPECIFIED));
            } else {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                IDepreciatedConsumableOrderRepository depreciatedConsumableOrderRepository =
                    _consumablesRepositoriesFactory.NewDepreciatedConsumableOrderRepository(connection);
                IDepreciatedConsumableOrderItemRepository depreciatedConsumableOrderItemRepository =
                    _consumablesRepositoriesFactory.NewDepreciatedConsumableOrderItemRepository(connection);

                DepreciatedConsumableOrder lastRecord = depreciatedConsumableOrderRepository.GetLastRecord();

                if (lastRecord == null || !lastRecord.Created.Year.Equals(DateTime.Now.Year))
                    message.DepreciatedConsumableOrder.Number = string.Format("{0:D6}", 1);
                else
                    message.DepreciatedConsumableOrder.Number = string.Format("{0:D6}", Convert.ToInt32(lastRecord.Number) + 1);

                if (message.DepreciatedConsumableOrder.CommissionHead != null)
                    message.DepreciatedConsumableOrder.CommissionHeadId = message.DepreciatedConsumableOrder.CommissionHead.Id;
                if (message.DepreciatedConsumableOrder.DepreciatedTo != null)
                    message.DepreciatedConsumableOrder.DepreciatedToId = message.DepreciatedConsumableOrder.DepreciatedTo.Id;
                if (message.DepreciatedConsumableOrder.ConsumablesStorage != null)
                    message.DepreciatedConsumableOrder.ConsumablesStorageId = message.DepreciatedConsumableOrder.ConsumablesStorage.Id;

                message.DepreciatedConsumableOrder.CreatedById = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

                message.DepreciatedConsumableOrder.Id = depreciatedConsumableOrderRepository.Add(message.DepreciatedConsumableOrder);

                bool isSuccess = true;
                string errorMessage = string.Empty;

                if (message.DepreciatedConsumableOrder.DepreciatedConsumableOrderItems.Any()) {
                    IConsumablesOrderItemRepository consumablesOrderItemRepository = _consumablesRepositoriesFactory.NewConsumablesOrderItemRepository(connection);
                    IPaymentCostMovementOperationRepository paymentCostMovementOperationRepository =
                        _paymentOrderRepositoriesFactory.NewPaymentCostMovementOperationRepository(connection);

                    foreach (DepreciatedConsumableOrderItem item in message.DepreciatedConsumableOrder.DepreciatedConsumableOrderItems)
                        if (item.ConsumablesOrderItem.IsNew()) {
                            List<ConsumablesOrderItem> orderItems = message.ExpensiveFirst
                                ? consumablesOrderItemRepository
                                    .GetAllUnDepreciatedByProductAndStorageIdsMostExpensiveFirst(
                                        item.ConsumablesOrderItem.ConsumableProduct.Id,
                                        message.DepreciatedConsumableOrder.ConsumablesStorageId
                                    ).ToList()
                                : consumablesOrderItemRepository
                                    .GetAllUnDepreciatedByProductAndStorageIds(
                                        item.ConsumablesOrderItem.ConsumableProduct.Id,
                                        message.DepreciatedConsumableOrder.ConsumablesStorageId
                                    ).ToList();

                            double totalQty = orderItems.Sum(i => i.Qty);

                            if (totalQty < item.Qty) {
                                isSuccess = false;

                                errorMessage = DepreciatedConsumableOrderResourceNames.SPECIFIED_QTY_MORE_THAN_AVAILABLE_ON_STORAGE;

                                break;
                            }

                            foreach (ConsumablesOrderItem consumablesOrderItem in orderItems)
                                if (consumablesOrderItem.Qty >= item.Qty) {
                                    item.Id = depreciatedConsumableOrderItemRepository.Add(new DepreciatedConsumableOrderItem {
                                        Qty = item.Qty,
                                        ConsumablesOrderItemId = consumablesOrderItem.Id,
                                        DepreciatedConsumableOrderId = message.DepreciatedConsumableOrder.Id
                                    });

                                    paymentCostMovementOperationRepository.Add(new PaymentCostMovementOperation {
                                        DepreciatedConsumableOrderItemId = item.Id,
                                        PaymentCostMovementId = item.PaymentCostMovementOperation.PaymentCostMovement.Id
                                    });

                                    break;
                                } else {
                                    item.Id = depreciatedConsumableOrderItemRepository.Add(new DepreciatedConsumableOrderItem {
                                        Qty = consumablesOrderItem.Qty,
                                        ConsumablesOrderItemId = consumablesOrderItem.Id,
                                        DepreciatedConsumableOrderId = message.DepreciatedConsumableOrder.Id
                                    });

                                    item.Qty -= consumablesOrderItem.Qty;

                                    paymentCostMovementOperationRepository.Add(new PaymentCostMovementOperation {
                                        DepreciatedConsumableOrderItemId = item.Id,
                                        PaymentCostMovementId = item.PaymentCostMovementOperation.PaymentCostMovement.Id
                                    });
                                }
                        } else {
                            ConsumablesOrderItem specifiedOrderItem =
                                consumablesOrderItemRepository.GetByIdWithCalculatedUnDepreciatedQty(item.ConsumablesOrderItem.Id,
                                    message.DepreciatedConsumableOrder.ConsumablesStorageId);

                            if (specifiedOrderItem == null) {
                                isSuccess = false;

                                errorMessage = DepreciatedConsumableOrderResourceNames.CONSUMABLE_ORDER_ITEM_NOT_EXISTS;

                                break;
                            }

                            if (specifiedOrderItem.Qty >= item.Qty) {
                                item.Id = depreciatedConsumableOrderItemRepository.Add(new DepreciatedConsumableOrderItem {
                                    Qty = item.Qty,
                                    ConsumablesOrderItemId = specifiedOrderItem.Id,
                                    DepreciatedConsumableOrderId = message.DepreciatedConsumableOrder.Id
                                });

                                paymentCostMovementOperationRepository.Add(new PaymentCostMovementOperation {
                                    DepreciatedConsumableOrderItemId = item.Id,
                                    PaymentCostMovementId = item.PaymentCostMovementOperation.PaymentCostMovement.Id
                                });
                            } else {
                                if (specifiedOrderItem.Qty > 0) {
                                    item.Id = depreciatedConsumableOrderItemRepository.Add(new DepreciatedConsumableOrderItem {
                                        Qty = specifiedOrderItem.Qty,
                                        ConsumablesOrderItemId = specifiedOrderItem.Id,
                                        DepreciatedConsumableOrderId = message.DepreciatedConsumableOrder.Id
                                    });

                                    item.Qty -= specifiedOrderItem.Qty;

                                    paymentCostMovementOperationRepository.Add(new PaymentCostMovementOperation {
                                        DepreciatedConsumableOrderItemId = item.Id,
                                        PaymentCostMovementId = item.PaymentCostMovementOperation.PaymentCostMovement.Id
                                    });
                                }

                                List<ConsumablesOrderItem> orderItems = message.ExpensiveFirst
                                    ? consumablesOrderItemRepository
                                        .GetAllUnDepreciatedByProductAndStorageIdsExceptProvidedItemIdMostExpensiveFirst(
                                            item.ConsumablesOrderItem.ConsumableProduct.Id,
                                            message.DepreciatedConsumableOrder.ConsumablesStorageId,
                                            item.ConsumablesOrderItem.Id
                                        ).ToList()
                                    : consumablesOrderItemRepository
                                        .GetAllUnDepreciatedByProductAndStorageIdsExceptProvidedItemId(
                                            item.ConsumablesOrderItem.ConsumableProduct.Id,
                                            message.DepreciatedConsumableOrder.ConsumablesStorageId,
                                            item.ConsumablesOrderItem.Id
                                        ).ToList();

                                double totalQty = orderItems.Sum(i => i.Qty) + specifiedOrderItem.Qty;

                                if (totalQty < item.Qty) {
                                    isSuccess = false;

                                    errorMessage = DepreciatedConsumableOrderResourceNames.SPECIFIED_QTY_MORE_THAN_AVAILABLE_ON_STORAGE;

                                    break;
                                }

                                foreach (ConsumablesOrderItem consumableOrderItem in orderItems)
                                    if (consumableOrderItem.Qty >= item.Qty) {
                                        item.Id = depreciatedConsumableOrderItemRepository.Add(new DepreciatedConsumableOrderItem {
                                            Qty = item.Qty,
                                            ConsumablesOrderItemId = consumableOrderItem.Id,
                                            DepreciatedConsumableOrderId = message.DepreciatedConsumableOrder.Id
                                        });

                                        paymentCostMovementOperationRepository.Add(new PaymentCostMovementOperation {
                                            DepreciatedConsumableOrderItemId = item.Id,
                                            PaymentCostMovementId = item.PaymentCostMovementOperation.PaymentCostMovement.Id
                                        });

                                        break;
                                    } else {
                                        item.Id = depreciatedConsumableOrderItemRepository.Add(new DepreciatedConsumableOrderItem {
                                            Qty = consumableOrderItem.Qty,
                                            ConsumablesOrderItemId = consumableOrderItem.Id,
                                            DepreciatedConsumableOrderId = message.DepreciatedConsumableOrder.Id
                                        });

                                        item.Qty -= consumableOrderItem.Qty;

                                        paymentCostMovementOperationRepository.Add(new PaymentCostMovementOperation {
                                            DepreciatedConsumableOrderItemId = item.Id,
                                            PaymentCostMovementId = item.PaymentCostMovementOperation.PaymentCostMovement.Id
                                        });
                                    }
                            }
                        }
                }

                if (isSuccess) {
                    Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(depreciatedConsumableOrderRepository.GetById(message.DepreciatedConsumableOrder.Id),
                        string.Empty));
                } else {
                    depreciatedConsumableOrderRepository.Remove(message.DepreciatedConsumableOrder.Id);

                    depreciatedConsumableOrderItemRepository.RemoveAllByOrderId(message.DepreciatedConsumableOrder.Id);

                    Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(null, errorMessage));
                }
            }
        });

        Receive<UpdateNewDepreciatedConsumableOrderMessage>(message => {
            if (message.DepreciatedConsumableOrder == null) {
                Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(null, "Empty DepreciatedConsumableOrder is not valid input for current request"));
            } else if (message.DepreciatedConsumableOrder.IsNew()) {
                Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(null, "Non-Existing DepreciatedConsumableOrder is not valid input for current request"));
            } else if (message.DepreciatedConsumableOrder.ConsumablesStorage == null && message.DepreciatedConsumableOrder.ConsumablesStorageId.Equals(0)) {
                Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(null, DepreciatedConsumableOrderResourceNames.CONSUMABLES_STORAGE_NOT_SPECIFIED));
            } else if (message.DepreciatedConsumableOrder.DepreciatedTo == null && message.DepreciatedConsumableOrder.DepreciatedToId.Equals(0)) {
                Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(null, DepreciatedConsumableOrderResourceNames.DEPRECIATED_TO_USER_NOT_SPECIFIED));
            } else if (message.DepreciatedConsumableOrder.CommissionHead == null && message.DepreciatedConsumableOrder.CommissionHeadId.Equals(0)) {
                Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(null, DepreciatedConsumableOrderResourceNames.COMMISSION_HEAD_USER_NOT_SPECIFIED));
            } else if (message.DepreciatedConsumableOrder.DepreciatedConsumableOrderItems.Any(i => i.Qty <= 0)) {
                Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(null, DepreciatedConsumableOrderResourceNames.ITEMS_QTY_SHOULD_BE_MORE_THAN_ZERO));
            } else if (message.DepreciatedConsumableOrder.DepreciatedConsumableOrderItems.Any(i =>
                           i.ConsumablesOrderItem?.ConsumableProduct == null)) {
                Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(null, DepreciatedConsumableOrderResourceNames.CONSUMABLE_PRODUCT_NOT_SPECIFIED));
            } else if (message.DepreciatedConsumableOrder.DepreciatedConsumableOrderItems.Any(i =>
                           i.PaymentCostMovementOperation?.PaymentCostMovement == null)) {
                Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(null, DepreciatedConsumableOrderResourceNames.PAYMENT_COST_MOVEMENT_NOT_SPECIFIED));
            } else {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                IDepreciatedConsumableOrderRepository depreciatedConsumableOrderRepository =
                    _consumablesRepositoriesFactory.NewDepreciatedConsumableOrderRepository(connection);
                IDepreciatedConsumableOrderItemRepository depreciatedConsumableOrderItemRepository =
                    _consumablesRepositoriesFactory.NewDepreciatedConsumableOrderItemRepository(connection);

                if (message.DepreciatedConsumableOrder.CommissionHead != null)
                    message.DepreciatedConsumableOrder.CommissionHeadId = message.DepreciatedConsumableOrder.CommissionHead.Id;
                if (message.DepreciatedConsumableOrder.DepreciatedTo != null)
                    message.DepreciatedConsumableOrder.DepreciatedToId = message.DepreciatedConsumableOrder.DepreciatedTo.Id;
                if (message.DepreciatedConsumableOrder.ConsumablesStorage != null)
                    message.DepreciatedConsumableOrder.ConsumablesStorageId = message.DepreciatedConsumableOrder.ConsumablesStorage.Id;

                DepreciatedConsumableOrder beforeUpdateOrder = depreciatedConsumableOrderRepository.GetByNetIdWithoutIncludes(message.DepreciatedConsumableOrder.NetUid);

                message.DepreciatedConsumableOrder.UpdatedById = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

                depreciatedConsumableOrderRepository.Update(message.DepreciatedConsumableOrder);

                bool isSuccess = true;
                string errorMessage = string.Empty;
                IEnumerable<DepreciatedConsumableOrderItem> beforeUpdateItems =
                    depreciatedConsumableOrderItemRepository.GetAllByOrderId(message.DepreciatedConsumableOrder.Id);

                if (message.DepreciatedConsumableOrder.DepreciatedConsumableOrderItems.Any()) {
                    IConsumablesOrderItemRepository consumablesOrderItemRepository = _consumablesRepositoriesFactory.NewConsumablesOrderItemRepository(connection);
                    IPaymentCostMovementOperationRepository paymentCostMovementOperationRepository =
                        _paymentOrderRepositoriesFactory.NewPaymentCostMovementOperationRepository(connection);

                    List<long> existingDepreciatedProductIds = new();

                    foreach (DepreciatedConsumableOrderItem item in message.DepreciatedConsumableOrder.DepreciatedConsumableOrderItems.Where(i => !i.Deleted)) {
                        if (existingDepreciatedProductIds.Any(id => id.Equals(item.ConsumablesOrderItem.ConsumableProduct.Id)))
                            continue;
                        item.Qty = message
                            .DepreciatedConsumableOrder
                            .DepreciatedConsumableOrderItems
                            .Where(i => i.ConsumablesOrderItem.ConsumableProduct.Id.Equals(item.ConsumablesOrderItem.ConsumableProduct.Id))
                            .Sum(i => i.Qty);

                        if (!item.IsNew())
                            depreciatedConsumableOrderItemRepository.RemoveAllByOrderAndProductIds(message.DepreciatedConsumableOrder.Id,
                                item.ConsumablesOrderItem.ConsumableProduct.Id);

                        if (item.ConsumablesOrderItem.IsNew()) {
                            List<ConsumablesOrderItem> orderItems = message.ExpensiveFirst
                                ? consumablesOrderItemRepository
                                    .GetAllUnDepreciatedByProductAndStorageIdsMostExpensiveFirst(
                                        item.ConsumablesOrderItem.ConsumableProduct.Id,
                                        message.DepreciatedConsumableOrder.ConsumablesStorageId
                                    ).ToList()
                                : consumablesOrderItemRepository
                                    .GetAllUnDepreciatedByProductAndStorageIds(
                                        item.ConsumablesOrderItem.ConsumableProduct.Id,
                                        message.DepreciatedConsumableOrder.ConsumablesStorageId
                                    ).ToList();

                            double totalQty = orderItems.Sum(i => i.Qty);

                            if (totalQty < item.Qty) {
                                isSuccess = false;

                                errorMessage = DepreciatedConsumableOrderResourceNames.SPECIFIED_QTY_MORE_THAN_AVAILABLE_ON_STORAGE;

                                break;
                            }

                            foreach (ConsumablesOrderItem consumablesOrderItem in orderItems)
                                if (consumablesOrderItem.Qty >= item.Qty) {
                                    item.Id = depreciatedConsumableOrderItemRepository.Add(new DepreciatedConsumableOrderItem {
                                        Qty = item.Qty,
                                        ConsumablesOrderItemId = consumablesOrderItem.Id,
                                        DepreciatedConsumableOrderId = message.DepreciatedConsumableOrder.Id
                                    });

                                    paymentCostMovementOperationRepository.Add(new PaymentCostMovementOperation {
                                        DepreciatedConsumableOrderItemId = item.Id,
                                        PaymentCostMovementId = item.PaymentCostMovementOperation.PaymentCostMovement.Id
                                    });

                                    break;
                                } else {
                                    item.Id = depreciatedConsumableOrderItemRepository.Add(new DepreciatedConsumableOrderItem {
                                        Qty = consumablesOrderItem.Qty,
                                        ConsumablesOrderItemId = consumablesOrderItem.Id,
                                        DepreciatedConsumableOrderId = message.DepreciatedConsumableOrder.Id
                                    });

                                    item.Qty -= consumablesOrderItem.Qty;

                                    paymentCostMovementOperationRepository.Add(new PaymentCostMovementOperation {
                                        DepreciatedConsumableOrderItemId = item.Id,
                                        PaymentCostMovementId = item.PaymentCostMovementOperation.PaymentCostMovement.Id
                                    });
                                }
                        } else {
                            ConsumablesOrderItem specifiedOrderItem =
                                consumablesOrderItemRepository.GetByIdWithCalculatedUnDepreciatedQty(item.ConsumablesOrderItem.Id,
                                    message.DepreciatedConsumableOrder.ConsumablesStorageId);

                            if (specifiedOrderItem == null) {
                                isSuccess = false;

                                errorMessage = DepreciatedConsumableOrderResourceNames.CONSUMABLE_ORDER_ITEM_NOT_EXISTS;

                                break;
                            }

                            if (specifiedOrderItem.Qty >= item.Qty) {
                                item.Id = depreciatedConsumableOrderItemRepository.Add(new DepreciatedConsumableOrderItem {
                                    Qty = item.Qty,
                                    ConsumablesOrderItemId = specifiedOrderItem.Id,
                                    DepreciatedConsumableOrderId = message.DepreciatedConsumableOrder.Id
                                });

                                paymentCostMovementOperationRepository.Add(new PaymentCostMovementOperation {
                                    DepreciatedConsumableOrderItemId = item.Id,
                                    PaymentCostMovementId = item.PaymentCostMovementOperation.PaymentCostMovement.Id
                                });

                                existingDepreciatedProductIds.Add(item.ConsumablesOrderItem.ConsumableProduct.Id);

                                continue;
                            }

                            if (specifiedOrderItem.Qty > 0) {
                                item.Id = depreciatedConsumableOrderItemRepository.Add(new DepreciatedConsumableOrderItem {
                                    Qty = specifiedOrderItem.Qty,
                                    ConsumablesOrderItemId = specifiedOrderItem.Id,
                                    DepreciatedConsumableOrderId = message.DepreciatedConsumableOrder.Id
                                });

                                item.Qty -= specifiedOrderItem.Qty;

                                paymentCostMovementOperationRepository.Add(new PaymentCostMovementOperation {
                                    DepreciatedConsumableOrderItemId = item.Id,
                                    PaymentCostMovementId = item.PaymentCostMovementOperation.PaymentCostMovement.Id
                                });
                            }

                            List<ConsumablesOrderItem> orderItems = message.ExpensiveFirst
                                ? consumablesOrderItemRepository
                                    .GetAllUnDepreciatedByProductAndStorageIdsExceptProvidedItemIdMostExpensiveFirst(
                                        item.ConsumablesOrderItem.ConsumableProduct.Id,
                                        message.DepreciatedConsumableOrder.ConsumablesStorageId,
                                        item.ConsumablesOrderItem.Id
                                    ).ToList()
                                : consumablesOrderItemRepository
                                    .GetAllUnDepreciatedByProductAndStorageIdsExceptProvidedItemId(
                                        item.ConsumablesOrderItem.ConsumableProduct.Id,
                                        message.DepreciatedConsumableOrder.ConsumablesStorageId,
                                        item.ConsumablesOrderItem.Id
                                    ).ToList();

                            double totalQty = orderItems.Sum(i => i.Qty);

                            if (totalQty < item.Qty) {
                                isSuccess = false;

                                errorMessage = DepreciatedConsumableOrderResourceNames.SPECIFIED_QTY_MORE_THAN_AVAILABLE_ON_STORAGE;

                                break;
                            }

                            foreach (ConsumablesOrderItem consumablesOrderItem in orderItems)
                                if (consumablesOrderItem.Qty >= item.Qty) {
                                    item.Id = depreciatedConsumableOrderItemRepository.Add(new DepreciatedConsumableOrderItem {
                                        Qty = item.Qty,
                                        ConsumablesOrderItemId = consumablesOrderItem.Id,
                                        DepreciatedConsumableOrderId = message.DepreciatedConsumableOrder.Id
                                    });

                                    paymentCostMovementOperationRepository.Add(new PaymentCostMovementOperation {
                                        DepreciatedConsumableOrderItemId = item.Id,
                                        PaymentCostMovementId = item.PaymentCostMovementOperation.PaymentCostMovement.Id
                                    });

                                    break;
                                } else {
                                    item.Id = depreciatedConsumableOrderItemRepository.Add(new DepreciatedConsumableOrderItem {
                                        Qty = consumablesOrderItem.Qty,
                                        ConsumablesOrderItemId = consumablesOrderItem.Id,
                                        DepreciatedConsumableOrderId = message.DepreciatedConsumableOrder.Id
                                    });

                                    item.Qty -= consumablesOrderItem.Qty;

                                    paymentCostMovementOperationRepository.Add(new PaymentCostMovementOperation {
                                        DepreciatedConsumableOrderItemId = item.Id,
                                        PaymentCostMovementId = item.PaymentCostMovementOperation.PaymentCostMovement.Id
                                    });
                                }
                        }

                        existingDepreciatedProductIds.Add(item.ConsumablesOrderItem.ConsumableProduct.Id);
                    }

                    depreciatedConsumableOrderItemRepository.RemoveAllByIds(message.DepreciatedConsumableOrder.DepreciatedConsumableOrderItems.Where(i => i.Deleted)
                        .Select(i => i.Id));
                } else {
                    depreciatedConsumableOrderItemRepository.RemoveAllByOrderId(message.DepreciatedConsumableOrder.Id);
                }

                if (isSuccess) {
                    Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(depreciatedConsumableOrderRepository.GetById(message.DepreciatedConsumableOrder.Id),
                        string.Empty));
                } else {
                    depreciatedConsumableOrderItemRepository.RemoveAllByOrderId(message.DepreciatedConsumableOrder.Id);

                    if (beforeUpdateItems.Any()) depreciatedConsumableOrderItemRepository.UpdateAndRestore(beforeUpdateItems);

                    depreciatedConsumableOrderRepository.Update(beforeUpdateOrder);

                    Sender.Tell(new Tuple<DepreciatedConsumableOrder, string>(null, errorMessage));
                }
            }
        });

        Receive<GetAllDepreciatedConsumableOrdersMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_consumablesRepositoriesFactory.NewDepreciatedConsumableOrderRepository(connection).GetAll());
        });

        Receive<GetAllDepreciatedConsumableOrdersFilteredMessage>(message => {
            if (string.IsNullOrEmpty(message.Value)) message.Value = string.Empty;
            if (message.From.Year.Equals(1)) message.From = DateTime.UtcNow;
            if (message.To.Year.Equals(1)) message.To = DateTime.UtcNow;

            message.To = message.To.AddHours(23).AddMinutes(59).AddSeconds(59);

            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _consumablesRepositoriesFactory
                    .NewDepreciatedConsumableOrderRepository(connection)
                    .GetAllFiltered(
                        message.From,
                        message.To,
                        message.Value,
                        message.StorageNetId
                    )
            );
        });

        Receive<DeleteDepreciatedConsumableOrderByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IDepreciatedConsumableOrderRepository depreciatedConsumableOrderRepository =
                _consumablesRepositoriesFactory.NewDepreciatedConsumableOrderRepository(connection);

            DepreciatedConsumableOrder fromDb = depreciatedConsumableOrderRepository.GetByNetIdWithoutIncludes(message.NetId);

            if (fromDb != null) {
                depreciatedConsumableOrderRepository.Remove(message.NetId);

                _consumablesRepositoriesFactory.NewDepreciatedConsumableOrderItemRepository(connection).RemoveAllByOrderId(fromDb.Id);
            }
        });
    }
}