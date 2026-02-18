using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Messages.Consumables.Orders;
using GBA.Domain.Repositories.Consumables.Contracts;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Consumables;

public sealed class ConsumablesOrderActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsumablesRepositoriesFactory _consumablesRepositoriesFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public ConsumablesOrderActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IConsumablesRepositoriesFactory consumablesRepositoriesFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _consumablesRepositoriesFactory = consumablesRepositoriesFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;

        Receive<CalculateConsumablesOrdersMessage>(ProcessCalculateConsumablesOrdersMessage);

        Receive<AddNewConsumablesOrderMessage>(ProcessAddNewConsumablesOrderMessage);

        Receive<UpdateConsumablesOrderMessage>(ProcessUpdateConsumablesOrderMessage);

        Receive<GetAllConsumablesOrdersMessage>(ProcessGetAllConsumablesOrdersMessage);

        Receive<GetAllServicesConsumablesOrdersMessage>(ProcessGetAllServicesConsumablesOrdersMessage);

        Receive<GetAllUnpaidConsumablesOrdersMessage>(ProcessGetAllUnpaidConsumablesOrdersMessage);

        Receive<GetAllConsumablesOrdersFromSearchMessage>(ProcessGetAllConsumablesOrdersFromSearchMessage);

        Receive<GetConsumablesOrderByNetIdMessage>(ProcessGetConsumablesOrderByNetIdMessage);

        Receive<DeleteConsumablesOrderByNetIdMessage>(ProcessDeleteConsumablesOrderByNetIdMessage);
    }

    private void ProcessCalculateConsumablesOrdersMessage(CalculateConsumablesOrdersMessage message) {
        foreach (ConsumablesOrder order in message.ConsumablesOrders) {
            foreach (ConsumablesOrderItem orderItem in order.ConsumablesOrderItems) {
                if (orderItem.Qty > 0) {
                    if (!orderItem.PricePerItem.Equals(decimal.Zero))
                        orderItem.TotalPriceWithVAT = Math.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2);
                    else if (!orderItem.TotalPriceWithVAT.Equals(decimal.Zero))
                        orderItem.PricePerItem = Math.Round(orderItem.TotalPriceWithVAT / Convert.ToDecimal(orderItem.Qty), 2);
                } else {
                    if (!orderItem.PricePerItem.Equals(decimal.Zero))
                        orderItem.TotalPriceWithVAT = orderItem.PricePerItem;
                    else if (!orderItem.TotalPriceWithVAT.Equals(decimal.Zero)) orderItem.PricePerItem = orderItem.TotalPriceWithVAT;
                }

                if (orderItem.VatPercent > 0)
                    orderItem.VAT = Math.Round(orderItem.TotalPriceWithVAT * Convert.ToDecimal(orderItem.VatPercent) / (100 + Convert.ToDecimal(orderItem.VatPercent)), 2);
                else if (orderItem.VAT > decimal.Zero)
                    orderItem.VatPercent = Math.Round(Convert.ToDouble(orderItem.VAT / (orderItem.TotalPriceWithVAT - orderItem.VAT)), 2);

                orderItem.TotalPrice = orderItem.TotalPriceWithVAT - orderItem.VAT;
            }

            order.TotalAmount = Math.Round(order.ConsumablesOrderItems.Sum(i => i.TotalPriceWithVAT), 2);

            order.TotalAmountWithoutVAT = Math.Round(order.ConsumablesOrderItems.Sum(i => i.TotalPrice), 2);
        }

        Sender.Tell(
            new Tuple<IEnumerable<ConsumablesOrder>, decimal>(
                message.ConsumablesOrders,
                Math.Round(message.ConsumablesOrders.Sum(o => o.TotalAmount), 2)
            )
        );
    }

    private void ProcessAddNewConsumablesOrderMessage(AddNewConsumablesOrderMessage message) {
        if (message.ConsumablesOrder == null) {
            Sender.Tell(new Tuple<ConsumablesOrder, string>(null, "ConsumablesOrder entity can not be empty"));
        } else if (!message.ConsumablesOrder.IsNew()) {
            Sender.Tell(new Tuple<ConsumablesOrder, string>(null, "Existing ConsumablesOrder is not valid input for current request"));
        } else if (message.ConsumablesOrder.ConsumableProductOrganization == null || message.ConsumablesOrder.ConsumableProductOrganization.IsNew()) {
            Sender.Tell(new Tuple<ConsumablesOrder, string>(null, ConsumableOrderResourceNames.CONSUMABLE_PRODUCT_ORGANIZATION_NOT_SPECIFIED));
        } else if (message.ConsumablesOrder.ConsumablesOrderItems.Any(i =>
                       i.PaymentCostMovementOperation?.PaymentCostMovement == null)) {
            Sender.Tell(new Tuple<ConsumablesOrder, string>(null, ConsumableOrderResourceNames.PAYMENT_COST_MOVEMENT_NOT_SPECIFIED));
        } else if (message.ConsumablesOrder.ConsumablesOrderItems.Any(i => i.SupplyOrganizationAgreement == null || i.SupplyOrganizationAgreement.IsNew())) {
            Sender.Tell(new Tuple<ConsumablesOrder, string>(null, ConsumableOrderResourceNames.SUPPLY_ORGANIZATION_AGREEMENT_NOT_SPECIFIED));
        } else if (message.ConsumablesOrder.SupplyPaymentTask != null &&
                   message.ConsumablesOrder.SupplyPaymentTask.IsNew() &&
                   (message.ConsumablesOrder.SupplyPaymentTask.User == null || message.ConsumablesOrder.SupplyPaymentTask.User.IsNew())) {
            Sender.Tell(new Tuple<ConsumablesOrder, string>(null, ConsumableOrderResourceNames.SUPPLY_PAYMENT_TASK_USER_NOT_SPECIFIED));
        } else {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.ConsumablesOrder.ConsumablesStorage != null) message.ConsumablesOrder.ConsumablesStorageId = message.ConsumablesOrder.ConsumablesStorage.Id;

            IConsumablesOrderRepository consumablesOrderRepository = _consumablesRepositoriesFactory.NewConsumablesOrderRepository(connection);
            IConsumablesOrderItemRepository consumablesOrderItemRepository = _consumablesRepositoriesFactory.NewConsumablesOrderItemRepository(connection);
            IPaymentCostMovementOperationRepository paymentCostMovementOperationRepository =
                _paymentOrderRepositoriesFactory.NewPaymentCostMovementOperationRepository(connection);
            ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);
            ISupplyPaymentTaskRepository supplyPaymentTaskRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);

            ConsumablesOrder lastConsumablesOrder = consumablesOrderRepository.GetLastRecord();

            bool withPaymentTask;

            if (lastConsumablesOrder == null || !lastConsumablesOrder.Created.Year.Equals(DateTime.Now.Year))
                message.ConsumablesOrder.Number = string.Format("{0:D10}", 1);
            else
                message.ConsumablesOrder.Number = string.Format("{0:D10}", Convert.ToInt32(lastConsumablesOrder.Number) + 1);

            message.ConsumablesOrder.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

            if (message.ConsumablesOrder.OrganizationFromDate.Year.Equals(1)) message.ConsumablesOrder.OrganizationFromDate = DateTime.UtcNow;
            if (message.ConsumablesOrder.SupplyPaymentTask != null && message.ConsumablesOrder.SupplyPaymentTask.IsNew()) {
                message.ConsumablesOrder.SupplyPaymentTask.PayToDate = message.ConsumablesOrder.SupplyPaymentTask.PayToDate?.Date ?? DateTime.Now.Date;

                message.ConsumablesOrder.SupplyPaymentTask.UserId = message.ConsumablesOrder.SupplyPaymentTask.User.Id;

                message.ConsumablesOrder.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;

                message.ConsumablesOrder.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.ConsumablesOrder;

                message.ConsumablesOrder.SupplyPaymentTask.Id = supplyPaymentTaskRepository.Add(message.ConsumablesOrder.SupplyPaymentTask);

                message.ConsumablesOrder.SupplyPaymentTaskId = message.ConsumablesOrder.SupplyPaymentTask.Id;

                withPaymentTask = true;
            } else {
                message.ConsumablesOrder.SupplyPaymentTask =
                    new SupplyPaymentTask {
                        TaskStatus = TaskStatus.Done,
                        PayToDate = message.ConsumablesOrder.OrganizationFromDate.Date,
                        UserId = message.ConsumablesOrder.UserId,
                        TaskAssignedTo = TaskAssignedTo.ConsumablesOrder,
                        TaskStatusUpdated = message.ConsumablesOrder.OrganizationFromDate
                    };

                message.ConsumablesOrder.SupplyPaymentTask.Id = supplyPaymentTaskRepository.Add(message.ConsumablesOrder.SupplyPaymentTask);

                message.ConsumablesOrder.SupplyPaymentTaskId = message.ConsumablesOrder.SupplyPaymentTask.Id;

                withPaymentTask = true;
            }

            message.ConsumablesOrder.Id = consumablesOrderRepository.Add(message.ConsumablesOrder);

            if (message.ConsumablesOrder.ConsumablesOrderDocuments.Any())
                _consumablesRepositoriesFactory
                    .NewConsumablesOrderDocumentRepository(connection)
                    .Add(
                        message
                            .ConsumablesOrder
                            .ConsumablesOrderDocuments
                            .Select(document => {
                                document.ConsumablesOrderId = message.ConsumablesOrder.Id;

                                return document;
                            })
                    );

            message.ConsumablesOrder.TotalAmount = decimal.Zero;

            foreach (ConsumablesOrderItem orderItem in message
                         .ConsumablesOrder
                         .ConsumablesOrderItems
                         .Where(i => (i.ConsumableProductCategory != null || !i.ConsumableProductCategoryId.Equals(0)) && !i.Deleted)) {
                if (orderItem.ConsumableProductCategory != null) orderItem.ConsumableProductCategoryId = orderItem.ConsumableProductCategory.Id;
                if (orderItem.ConsumableProduct != null) orderItem.ConsumableProductId = orderItem.ConsumableProduct.Id;
                if (orderItem.ConsumableProductOrganization != null) orderItem.ConsumableProductOrganizationId = orderItem.ConsumableProductOrganization.Id;
                if (orderItem.SupplyOrganizationAgreement != null) orderItem.SupplyOrganizationAgreementId = orderItem.SupplyOrganizationAgreement.Id;
                if (!orderItem.TotalPrice.Equals(decimal.Zero))
                    orderItem.PricePerItem = Math.Round(orderItem.TotalPrice / Convert.ToDecimal(orderItem.Qty), 2);
                else
                    orderItem.TotalPrice = Math.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2);
                if (orderItem.VatPercent > 0)
                    orderItem.VAT = Math.Round(orderItem.TotalPrice * Convert.ToDecimal(orderItem.VatPercent / 100), 2);
                else if (orderItem.VAT > decimal.Zero) orderItem.VatPercent = Math.Round(Convert.ToDouble(orderItem.VAT * 100 / orderItem.TotalPrice), 2);

                orderItem.ConsumablesOrderId = message.ConsumablesOrder.Id;

                orderItem.Id = consumablesOrderItemRepository.Add(orderItem);

                orderItem.PaymentCostMovementOperation.PaymentCostMovementId = orderItem.PaymentCostMovementOperation.PaymentCostMovement.Id;
                orderItem.PaymentCostMovementOperation.ConsumablesOrderItemId = orderItem.Id;

                paymentCostMovementOperationRepository.Add(orderItem.PaymentCostMovementOperation);

                message.ConsumablesOrder.TotalAmount = Math.Round(message.ConsumablesOrder.TotalAmount + orderItem.TotalPrice + orderItem.VAT, 2);

                if (withPaymentTask) {
                    message.ConsumablesOrder.SupplyPaymentTask.NetPrice =
                        Math.Round(message.ConsumablesOrder.SupplyPaymentTask.NetPrice + orderItem.TotalPrice, 2);

                    message.ConsumablesOrder.SupplyPaymentTask.GrossPrice =
                        Math.Round(message.ConsumablesOrder.SupplyPaymentTask.GrossPrice + orderItem.TotalPrice + orderItem.VAT, 2);
                }
            }

            message.ConsumablesOrder.SupplyOrganizationAgreement =
                supplyOrganizationAgreementRepository
                    .GetById(message.ConsumablesOrder.SupplyOrganizationAgreement.Id);

            message.ConsumablesOrder.SupplyOrganizationAgreement.CurrentAmount =
                Math.Round(message.ConsumablesOrder.SupplyOrganizationAgreement.CurrentAmount - message.ConsumablesOrder.TotalAmount, 2);

            supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.ConsumablesOrder.SupplyOrganizationAgreement);

            if (withPaymentTask) supplyPaymentTaskRepository.UpdateTaskPrices(message.ConsumablesOrder.SupplyPaymentTask);

            Sender.Tell(new Tuple<ConsumablesOrder, string>(consumablesOrderRepository.GetById(message.ConsumablesOrder.Id), string.Empty));
        }
    }

    private void ProcessUpdateConsumablesOrderMessage(UpdateConsumablesOrderMessage message) {
        if (message.ConsumablesOrder == null) {
            Sender.Tell(new Tuple<ConsumablesOrder, string>(null, "ConsumablesOrder entity can not be empty"));
        } else if (message.ConsumablesOrder.IsNew()) {
            Sender.Tell(new Tuple<ConsumablesOrder, string>(null, "New ConsumablesOrder is not valid input for current request"));
        } else if (message.ConsumablesOrder.ConsumablesOrderItems.Any(i =>
                       i.PaymentCostMovementOperation?.PaymentCostMovement == null)) {
            Sender.Tell(new Tuple<ConsumablesOrder, string>(null, ConsumableOrderResourceNames.PAYMENT_COST_MOVEMENT_NOT_SPECIFIED));
        } else if (message.ConsumablesOrder.ConsumablesOrderItems.Any(i => i.SupplyOrganizationAgreement == null || i.SupplyOrganizationAgreement.IsNew())) {
            Sender.Tell(new Tuple<ConsumablesOrder, string>(null, ConsumableOrderResourceNames.SUPPLY_ORGANIZATION_AGREEMENT_NOT_SPECIFIED));
        } else if (message.ConsumablesOrder.SupplyPaymentTask != null &&
                   message.ConsumablesOrder.SupplyPaymentTask.IsNew() &&
                   (message.ConsumablesOrder.SupplyPaymentTask.User == null || message.ConsumablesOrder.SupplyPaymentTask.User.IsNew())) {
            Sender.Tell(new Tuple<ConsumablesOrder, string>(null, ConsumableOrderResourceNames.SUPPLY_PAYMENT_TASK_USER_NOT_SPECIFIED));
        } else {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.ConsumablesOrder.ConsumablesStorage != null) message.ConsumablesOrder.ConsumablesStorageId = message.ConsumablesOrder.ConsumablesStorage.Id;

            IConsumablesOrderRepository consumablesOrderRepository = _consumablesRepositoriesFactory.NewConsumablesOrderRepository(connection);

            ConsumablesOrder orderFromDb = consumablesOrderRepository.GetById(message.ConsumablesOrder.Id);

            if (orderFromDb != null) {
                ISupplyPaymentTaskRepository supplyPaymentTaskRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);

                orderFromDb.TotalAmount = Math.Round(
                    orderFromDb
                        .ConsumablesOrderItems
                        .Sum(i => i.TotalPrice + i.VAT)
                    , 2);

                if (message.ConsumablesOrder.OrganizationFromDate.Year.Equals(1)) message.ConsumablesOrder.OrganizationFromDate = DateTime.UtcNow;

                message.ConsumablesOrder.UserId = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId).Id;

                if (message.ConsumablesOrder.ConsumablesOrderDocuments.Any()) {
                    IConsumablesOrderDocumentRepository consumablesOrderDocumentRepository =
                        _consumablesRepositoriesFactory.NewConsumablesOrderDocumentRepository(connection);

                    consumablesOrderDocumentRepository
                        .Add(
                            message
                                .ConsumablesOrder
                                .ConsumablesOrderDocuments
                                .Where(d => d.IsNew() && !d.Deleted)
                                .Select(document => {
                                    document.ConsumablesOrderId = message.ConsumablesOrder.Id;

                                    return document;
                                })
                        );

                    consumablesOrderDocumentRepository
                        .Update(
                            message
                                .ConsumablesOrder
                                .ConsumablesOrderDocuments
                                .Where(d => !d.IsNew() && !d.Deleted)
                        );

                    consumablesOrderDocumentRepository
                        .Remove(
                            message
                                .ConsumablesOrder
                                .ConsumablesOrderDocuments
                                .Where(d => !d.IsNew() && d.Deleted)
                                .Select(d => d.Id)
                        );
                }

                IConsumablesOrderItemRepository consumablesOrderItemRepository = _consumablesRepositoriesFactory.NewConsumablesOrderItemRepository(connection);
                IPaymentCostMovementOperationRepository paymentCostMovementOperationRepository =
                    _paymentOrderRepositoriesFactory.NewPaymentCostMovementOperationRepository(connection);

                consumablesOrderItemRepository
                    .RemoveAllExceptProvided(
                        message.ConsumablesOrder.Id,
                        message
                            .ConsumablesOrder
                            .ConsumablesOrderItems
                            .Where(i => !i.IsNew() && !i.Deleted)
                            .Select(i => i.Id)
                    );

                foreach (ConsumablesOrderItem orderItem in message
                             .ConsumablesOrder
                             .ConsumablesOrderItems
                             .Where(i => i.IsNew() && (i.ConsumableProductCategory != null || !i.ConsumableProductCategoryId.Equals(0)) && !i.Deleted)) {
                    if (orderItem.ConsumableProductCategory != null) orderItem.ConsumableProductCategoryId = orderItem.ConsumableProductCategory.Id;
                    if (orderItem.ConsumableProduct != null) orderItem.ConsumableProductId = orderItem.ConsumableProduct.Id;
                    if (orderItem.ConsumableProductOrganization != null) orderItem.ConsumableProductOrganizationId = orderItem.ConsumableProductOrganization.Id;
                    if (!orderItem.TotalPrice.Equals(decimal.Zero))
                        orderItem.PricePerItem = Math.Round(orderItem.TotalPrice / Convert.ToDecimal(orderItem.Qty), 2);
                    else
                        orderItem.TotalPrice = Math.Round(orderItem.PricePerItem * Convert.ToDecimal(orderItem.Qty), 2);
                    if (orderItem.VatPercent > 0)
                        orderItem.VAT = Math.Round(orderItem.TotalPrice * Convert.ToDecimal(orderItem.VatPercent / 100), 2);
                    else if (orderItem.VAT > decimal.Zero) orderItem.VatPercent = Math.Round(Convert.ToDouble(orderItem.VAT * 100 / orderItem.TotalPrice), 2);

                    orderItem.ConsumablesOrderId = message.ConsumablesOrder.Id;

                    orderItem.Id = consumablesOrderItemRepository.Add(orderItem);

                    orderItem.PaymentCostMovementOperation.PaymentCostMovementId = orderItem.PaymentCostMovementOperation.PaymentCostMovement.Id;
                    orderItem.PaymentCostMovementOperation.ConsumablesOrderItemId = orderItem.Id;

                    paymentCostMovementOperationRepository.Add(orderItem.PaymentCostMovementOperation);
                }

                message.ConsumablesOrder.TotalAmount = Math.Round(
                    message.ConsumablesOrder
                        .ConsumablesOrderItems
                        .Where(i => !i.Deleted)
                        .Sum(i => i.TotalPrice + i.VAT)
                    , 2);

                message.ConsumablesOrder.TotalAmountWithoutVAT = Math.Round(
                    message.ConsumablesOrder
                        .ConsumablesOrderItems
                        .Where(i => !i.Deleted)
                        .Sum(i => i.TotalPrice)
                    , 2);

                if (message.ConsumablesOrder.SupplyPaymentTask != null && message.ConsumablesOrder.SupplyPaymentTask.IsNew()) {
                    message.ConsumablesOrder.SupplyPaymentTask.PayToDate = message.ConsumablesOrder.SupplyPaymentTask.PayToDate?.Date ?? DateTime.Now.Date;

                    message.ConsumablesOrder.SupplyPaymentTask.UserId = message.ConsumablesOrder.SupplyPaymentTask.User.Id;

                    message.ConsumablesOrder.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;

                    message.ConsumablesOrder.SupplyPaymentTask.NetPrice = message.ConsumablesOrder.TotalAmountWithoutVAT;
                    message.ConsumablesOrder.SupplyPaymentTask.GrossPrice = message.ConsumablesOrder.TotalAmount;

                    message.ConsumablesOrder.SupplyPaymentTask.Id = supplyPaymentTaskRepository.Add(message.ConsumablesOrder.SupplyPaymentTask);

                    message.ConsumablesOrder.SupplyPaymentTaskId = message.ConsumablesOrder.SupplyPaymentTask.Id;
                }

                consumablesOrderRepository.Update(message.ConsumablesOrder);

                if (!message.ConsumablesOrder.TotalAmount.Equals(orderFromDb.TotalAmount)) {
                    ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                        _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);

                    decimal differenceAmount = message.ConsumablesOrder.TotalAmount - orderFromDb.TotalAmount;

                    message.ConsumablesOrder.SupplyOrganizationAgreement =
                        supplyOrganizationAgreementRepository
                            .GetById(message.ConsumablesOrder.SupplyOrganizationAgreement.Id);

                    message.ConsumablesOrder.SupplyOrganizationAgreement.CurrentAmount =
                        Math.Round(message.ConsumablesOrder.SupplyOrganizationAgreement.CurrentAmount - differenceAmount, 2);

                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.ConsumablesOrder.SupplyOrganizationAgreement);

                    if (orderFromDb.SupplyPaymentTask != null) {
                        orderFromDb.SupplyPaymentTask.NetPrice = message.ConsumablesOrder.TotalAmountWithoutVAT;
                        orderFromDb.SupplyPaymentTask.GrossPrice = message.ConsumablesOrder.TotalAmount;

                        supplyPaymentTaskRepository.UpdateTaskPrices(orderFromDb.SupplyPaymentTask);
                    }
                }

                Sender.Tell(new Tuple<ConsumablesOrder, string>(consumablesOrderRepository.GetById(message.ConsumablesOrder.Id), string.Empty));
            } else {
                Sender.Tell(new Tuple<ConsumablesOrder, string>(null, "Such consumable order does not exists"));
            }
        }
    }

    private void ProcessGetAllConsumablesOrdersMessage(GetAllConsumablesOrdersMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_consumablesRepositoriesFactory.NewConsumablesOrderRepository(connection).GetAll(message.From, message.To));
    }

    private void ProcessGetAllServicesConsumablesOrdersMessage(GetAllServicesConsumablesOrdersMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_consumablesRepositoriesFactory.NewConsumablesOrderRepository(connection)
            .GetAllServices(message.From, message.To, message.Value, message.OrganizationNetId));
    }

    private void ProcessGetAllUnpaidConsumablesOrdersMessage(GetAllUnpaidConsumablesOrdersMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_consumablesRepositoriesFactory.NewConsumablesOrderRepository(connection).GetAllUnpaidByConsumableOrganizationNetId(message.OrganizationNetId));
    }

    private void ProcessGetAllConsumablesOrdersFromSearchMessage(GetAllConsumablesOrdersFromSearchMessage message) {
        if (string.IsNullOrEmpty(message.Value)) message.Value = string.Empty;

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_consumablesRepositoriesFactory.NewConsumablesOrderRepository(connection).GetAllFromSearch(message.Value));
    }

    private void ProcessGetConsumablesOrderByNetIdMessage(GetConsumablesOrderByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_consumablesRepositoriesFactory.NewConsumablesOrderRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessDeleteConsumablesOrderByNetIdMessage(DeleteConsumablesOrderByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _consumablesRepositoriesFactory.NewConsumablesOrderRepository(connection).Remove(message.NetId);
    }
}