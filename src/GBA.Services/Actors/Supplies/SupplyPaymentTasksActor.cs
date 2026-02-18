using System;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.EntityHelpers.Supplies;
using GBA.Domain.Messages.Supplies.PaymentTasks;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Services.Actors.Supplies;

public sealed class SupplyPaymentTasksActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;

    public SupplyPaymentTasksActor(
        IDbConnectionFactory connectionFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;

        Receive<SetSupplyPaymentTaskAvailableForPaymentByNetIdMessage>(ProcessSetSupplyPaymentTaskAvailableForPaymentByNetIdMessage);

        Receive<MergeSupplyPaymentTasksMessage>(ProcessMergeSupplyPaymentTasksMessage);

        Receive<RemoveSupplyPaymentTaskByNetIdMessage>(ProcessRemoveSupplyPaymentTaskByNetIdMessage);

        Receive<GetSupplyPaymentTaskByNetIdMessage>(ProcessGetSupplyPaymentTaskByNetIdMessage);
    }

    private void ProcessSetSupplyPaymentTaskAvailableForPaymentByNetIdMessage(SetSupplyPaymentTaskAvailableForPaymentByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyPaymentTaskRepository supplyPaymentTaskRepository =
            _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);

        if (message.SupplyPaymentTask.SupplyPaymentTaskDocuments.Any()) {
            ISupplyPaymentTaskDocumentRepository taskDocumentRepository =
                _supplyRepositoriesFactory
                    .NewSupplyPaymentTaskDocumentRepository(connection);

            taskDocumentRepository
                .Add(
                    message
                        .SupplyPaymentTask
                        .SupplyPaymentTaskDocuments
                        .Where(d => d.IsNew())
                        .Select(d => {
                            d.SupplyPaymentTaskId = message.SupplyPaymentTask.Id;

                            return d;
                        })
                );

            message.SupplyPaymentTask.SupplyPaymentTaskDocuments = taskDocumentRepository.GetAllByTaskId(message.SupplyPaymentTask.Id);
        }

        if (!message.SupplyPaymentTask.IsAvailableForPayment) {
            message.SupplyPaymentTask.IsAvailableForPayment = true;

            supplyPaymentTaskRepository.SetTaskAvailableForPayment(message.SupplyPaymentTask);
        }

        Sender.Tell(supplyPaymentTaskRepository.GetById(message.SupplyPaymentTask.Id));
    }

    private void ProcessMergeSupplyPaymentTasksMessage(MergeSupplyPaymentTasksMessage message) {
        if (!message.Tasks.Any()) {
            Sender.Tell(new Tuple<SupplyPaymentTask, string>(null, "Empty collection"));
        } else if (message.Tasks.Count().Equals(1)) {
            Sender.Tell(new Tuple<SupplyPaymentTask, string>(null, "You need to provide at least two tasks"));
        } else if (!message.Tasks.All(t => t.TaskStatus.Equals(TaskStatus.NotDone))) {
            Sender.Tell(new Tuple<SupplyPaymentTask, string>(null, "Only not paid tasks can be merged"));
        } else {
            if (message.Tasks.All(t => t.ContainerServices.Any())) {
                SupplyPaymentTask fromList = message.Tasks.First();

                long? organizationId = fromList.ContainerServices.First().ContainerOrganizationId;

                if (message.Tasks.Any(t => t.ContainerServices.Any(s => !s.ContainerOrganizationId.Equals(organizationId)))) {
                    Sender.Tell(new Tuple<SupplyPaymentTask, string>(null, "All Container services should have same organization"));
                } else {
                    using IDbConnection connection = _connectionFactory.NewSqlConnection();
                    ISupplyPaymentTaskRepository supplyPaymentTaskRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);

                    SupplyPaymentTask mergedTask = new() {
                        NetPrice = Math.Round(message.Tasks.Sum(t => t.NetPrice), 2),
                        EuroNetPrice = Math.Round(message.Tasks.Sum(t => t.EuroNetPrice), 2),
                        GrossPrice = Math.Round(message.Tasks.Sum(t => t.GrossPrice), 2),
                        EuroGrossPrice = Math.Round(message.Tasks.Sum(t => t.EuroGrossPrice), 2),
                        TaskAssignedTo = TaskAssignedTo.ContainerService,
                        UserId = fromList.UserId,
                        User = fromList.User,
                        TaskStatus = TaskStatus.NotDone,
                        PayToDate = fromList.PayToDate
                    };

                    mergedTask.Id = supplyPaymentTaskRepository.Add(mergedTask);

                    foreach (SupplyPaymentTask task in message.Tasks)
                    foreach (ContainerService service in task.ContainerServices) {
                        service.SupplyPaymentTaskId = mergedTask.Id;

                        mergedTask.ContainerServices.Add(service);
                    }

                    _supplyRepositoriesFactory.NewContainerServiceRepository(connection).UpdateSupplyPaymentTaskId(mergedTask.ContainerServices);

                    supplyPaymentTaskRepository.RemoveAllByIds(message.Tasks.Select(t => t.Id));

                    Sender.Tell(new Tuple<SupplyPaymentTask, string>(mergedTask, string.Empty));
                }
            } else if (message.Tasks.All(t => t.PortWorkServices.Any())) {
                SupplyPaymentTask fromList = message.Tasks.First();

                long? organizationId = fromList.PortWorkServices.First().PortWorkOrganizationId;

                if (message.Tasks.Any(t => t.PortWorkServices.Any(s => !s.PortWorkOrganizationId.Equals(organizationId)))) {
                    Sender.Tell(new Tuple<SupplyPaymentTask, string>(null, "All Port work services should have same organization"));
                } else {
                    using IDbConnection connection = _connectionFactory.NewSqlConnection();
                    ISupplyPaymentTaskRepository supplyPaymentTaskRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);

                    SupplyPaymentTask mergedTask = new() {
                        NetPrice = Math.Round(message.Tasks.Sum(t => t.NetPrice), 2),
                        EuroNetPrice = Math.Round(message.Tasks.Sum(t => t.EuroNetPrice), 2),
                        GrossPrice = Math.Round(message.Tasks.Sum(t => t.GrossPrice), 2),
                        EuroGrossPrice = Math.Round(message.Tasks.Sum(t => t.EuroGrossPrice), 2),
                        TaskAssignedTo = TaskAssignedTo.PortWorkService,
                        UserId = fromList.UserId,
                        User = fromList.User,
                        TaskStatus = TaskStatus.NotDone,
                        PayToDate = fromList.PayToDate
                    };

                    mergedTask.Id = supplyPaymentTaskRepository.Add(mergedTask);

                    foreach (SupplyPaymentTask task in message.Tasks)
                    foreach (PortWorkService service in task.PortWorkServices) {
                        service.SupplyPaymentTaskId = mergedTask.Id;

                        mergedTask.PortWorkServices.Add(service);
                    }

                    _supplyRepositoriesFactory.NewPortWorkServiceRepository(connection).UpdateSupplyPaymentTaskId(mergedTask.PortWorkServices);

                    supplyPaymentTaskRepository.RemoveAllByIds(message.Tasks.Select(t => t.Id));

                    Sender.Tell(new Tuple<SupplyPaymentTask, string>(mergedTask, string.Empty));
                }
            } else {
                Sender.Tell(new Tuple<SupplyPaymentTask, string>(null, "Collection should have same Container or PortWork services"));
            }
        }
    }

    private void ProcessRemoveSupplyPaymentTaskByNetIdMessage(RemoveSupplyPaymentTaskByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyPaymentTaskRepository supplyPaymentTaskRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);

        SupplyPaymentTask task = supplyPaymentTaskRepository.GetByNetId(message.NetId);

        if (task != null) {
            if (task.IsAvailableForPayment)
                Sender.Tell(
                    new RemovePaymentTaskResponse(
                        false,
                        "Current PaymentTask is set as available for payment and can not be removed"
                    )
                );
            else if (!task.TaskStatus.Equals(TaskStatus.NotDone))
                Sender.Tell(
                    new RemovePaymentTaskResponse(
                        false,
                        "Current PaymentTask is already paid or partially paid and can not be removed"
                    )
                );
        } else {
            Sender.Tell(
                new RemovePaymentTaskResponse(
                    false,
                    "Payment task with provided NetId does not exists in database"
                )
            );
        }
    }

    private void ProcessGetSupplyPaymentTaskByNetIdMessage(GetSupplyPaymentTaskByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection).GetByNetId(message.NetId));
    }
}