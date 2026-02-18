using System;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Messages.Supplies.HelperServices;
using GBA.Domain.Messages.Supplies.HelperServices.PortWorks;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Supplies.HelperServices;

public sealed class PortWorkServicesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public PortWorkServicesActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;

        Receive<AddPortWorkServiceMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IPortWorkServiceRepository portWorkServiceRepository = _supplyRepositoriesFactory.NewPortWorkServiceRepository(connection);
            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            ISupplyPaymentTaskRepository supplyPaymentTaskRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);
            ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);

            User user = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

            if (message.PortWorkService.PortWorkOrganization != null) message.PortWorkService.PortWorkOrganizationId = message.PortWorkService.PortWorkOrganization.Id;

            if (message.PortWorkService.SupplyPaymentTask != null) {
                if (message.PortWorkService.SupplyPaymentTask.User != null)
                    message.PortWorkService.SupplyPaymentTask.UserId = message.PortWorkService.SupplyPaymentTask.User.Id;

                message.PortWorkService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.PortWorkService;
                message.PortWorkService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;

                message.PortWorkService.SupplyPaymentTask.PayToDate = !message.PortWorkService.SupplyPaymentTask.PayToDate.HasValue
                    ? DateTime.UtcNow.Date
                    : message.PortWorkService.SupplyPaymentTask.PayToDate.Value.Date;

                message.PortWorkService.SupplyPaymentTask.NetPrice = message.PortWorkService.NetPrice;
                message.PortWorkService.SupplyPaymentTask.GrossPrice = message.PortWorkService.GrossPrice;

                message.PortWorkService.SupplyPaymentTaskId = supplyPaymentTaskRepository
                    .Add(message.PortWorkService.SupplyPaymentTask);
            }

            if (message.PortWorkService.AccountingPaymentTask != null) {
                if (message.PortWorkService.AccountingPaymentTask.User != null)
                    message.PortWorkService.AccountingPaymentTask.UserId = message.PortWorkService.AccountingPaymentTask.User.Id;

                message.PortWorkService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.PortWorkService;
                message.PortWorkService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                message.PortWorkService.AccountingPaymentTask.IsAccounting = true;

                message.PortWorkService.AccountingPaymentTask.PayToDate = !message.PortWorkService.AccountingPaymentTask.PayToDate.HasValue
                    ? DateTime.UtcNow.Date
                    : message.PortWorkService.AccountingPaymentTask.PayToDate.Value.Date;

                message.PortWorkService.AccountingPaymentTask.NetPrice = message.PortWorkService.AccountingNetPrice;
                message.PortWorkService.AccountingPaymentTask.GrossPrice = message.PortWorkService.AccountingGrossPrice;

                message.PortWorkService.AccountingPaymentTaskId = supplyPaymentTaskRepository
                    .Add(message.PortWorkService.AccountingPaymentTask);
            }

            if (message.PortWorkService.SupplyOrganizationAgreement != null && !message.PortWorkService.SupplyOrganizationAgreement.IsNew()) {
                message.PortWorkService.SupplyOrganizationAgreement =
                    supplyOrganizationAgreementRepository.GetById(message.PortWorkService.SupplyOrganizationAgreement.Id);

                message.PortWorkService.SupplyOrganizationAgreement.CurrentAmount =
                    Math.Round(
                        message.PortWorkService.SupplyOrganizationAgreement.CurrentAmount - message.PortWorkService.GrossPrice, 2);

                message.PortWorkService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                    Math.Round(
                        message.PortWorkService.SupplyOrganizationAgreement.AccountingCurrentAmount -
                        message.PortWorkService.AccountingGrossPrice, 2);

                supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.PortWorkService.SupplyOrganizationAgreement);
            }

            message.PortWorkService.UserId = user.Id;

            long portWorkServiceId = portWorkServiceRepository.Add(message.PortWorkService);

            if (message.PortWorkService.InvoiceDocuments.Any()) {
                message.PortWorkService.InvoiceDocuments.ToList().ForEach(d => {
                    d.PortWorkServiceId = portWorkServiceId;
                });

                _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection).Add(message.PortWorkService.InvoiceDocuments);
            }

            Sender.Tell(portWorkServiceRepository.GetById(portWorkServiceId));
        });

        Receive<UpdatePortWorkServiceMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.PortWorkService.InvoiceDocuments.Any()) {
                message.PortWorkService.InvoiceDocuments.ToList().ForEach(d => {
                    d.PortWorkServiceId = message.PortWorkService.Id;
                });

                _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection).Update(message.PortWorkService.InvoiceDocuments);
            }

            if (message.PortWorkService.ActProvidingServiceDocument != null && !message.PortWorkService.ActProvidingServiceDocument.Id.Equals(0))
                _supplyRepositoriesFactory
                    .NewActProvidingServiceDocumentRepository(connection)
                    .Update(message.PortWorkService.ActProvidingServiceDocument);

            if (message.PortWorkService.SupplyServiceAccountDocument != null && !message.PortWorkService.SupplyServiceAccountDocument.Id.Equals(0))
                _supplyRepositoriesFactory
                    .NewSupplyServiceAccountDocumentRepository(connection)
                    .Update(message.PortWorkService.SupplyServiceAccountDocument);

            Sender.Tell(_supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetByNetId(message.NetId));
        });

        Receive<GetAllDetailItemsByServiceNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_supplyRepositoriesFactory
                .NewServiceDetailItemRepository(connection)
                .GetAllByNetIdAndType(message.NetId, SupplyServiceType.PortWork)
            );
        });
    }
}