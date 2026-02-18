using System;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Messages.Supplies.HelperServices;
using GBA.Domain.Messages.Supplies.HelperServices.Transportations;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Supplies.HelperServices;

public sealed class TransportationServicesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public TransportationServicesActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;

        Receive<AddTransportationServiceMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ITransportationServiceRepository transportationServiceRepository = _supplyRepositoriesFactory.NewTransportationServiceRepository(connection);
            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            ISupplyPaymentTaskRepository supplyPaymentTaskRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);
            ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);

            User user = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

            long newPaymentTaskId = 0;

            if (message.TransportationService.TransportationOrganization != null)
                message.TransportationService.TransportationOrganizationId = message.TransportationService.TransportationOrganization.Id;

            if (message.TransportationService.SupplyPaymentTask != null) {
                if (message.TransportationService.SupplyPaymentTask.User != null)
                    message.TransportationService.SupplyPaymentTask.UserId = message.TransportationService.SupplyPaymentTask.User.Id;

                message.TransportationService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.TransportationService;
                message.TransportationService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;

                message.TransportationService.SupplyPaymentTask.PayToDate = message.TransportationService.SupplyPaymentTask.PayToDate?.Date ?? DateTime.UtcNow.Date;

                message.TransportationService.SupplyPaymentTask.NetPrice = message.TransportationService.NetPrice;
                message.TransportationService.SupplyPaymentTask.GrossPrice = message.TransportationService.GrossPrice;

                newPaymentTaskId = supplyPaymentTaskRepository
                    .Add(message.TransportationService.SupplyPaymentTask);

                message.TransportationService.SupplyPaymentTaskId = newPaymentTaskId;
            }

            if (message.TransportationService.AccountingPaymentTask != null) {
                if (message.TransportationService.AccountingPaymentTask.User != null)
                    message.TransportationService.AccountingPaymentTask.UserId = message.TransportationService.AccountingPaymentTask.User.Id;

                message.TransportationService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.TransportationService;
                message.TransportationService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                message.TransportationService.AccountingPaymentTask.IsAccounting = true;

                message.TransportationService.AccountingPaymentTask.PayToDate = message.TransportationService.AccountingPaymentTask.PayToDate?.Date ?? DateTime.UtcNow.Date;

                message.TransportationService.AccountingPaymentTask.NetPrice = message.TransportationService.AccountingNetPrice;
                message.TransportationService.AccountingPaymentTask.GrossPrice = message.TransportationService.AccountingGrossPrice;

                newPaymentTaskId = supplyPaymentTaskRepository
                    .Add(message.TransportationService.AccountingPaymentTask);

                message.TransportationService.AccountingPaymentTaskId = newPaymentTaskId;
            }

            if (message.TransportationService.SupplyOrganizationAgreement != null && !message.TransportationService.SupplyOrganizationAgreement.IsNew()) {
                message.TransportationService.SupplyOrganizationAgreement =
                    supplyOrganizationAgreementRepository.GetById(message.TransportationService.SupplyOrganizationAgreement.Id);

                message.TransportationService.SupplyOrganizationAgreement.CurrentAmount =
                    Math.Round(
                        message.TransportationService.SupplyOrganizationAgreement.CurrentAmount - message.TransportationService.GrossPrice,
                        2);

                message.TransportationService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                    Math.Round(
                        message.TransportationService.SupplyOrganizationAgreement.AccountingCurrentAmount - message.TransportationService.AccountingGrossPrice,
                        2);

                supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.TransportationService.SupplyOrganizationAgreement);
            }

            message.TransportationService.UserId = user.Id;

            long transportationServiceId = transportationServiceRepository.Add(message.TransportationService);

            if (message.TransportationService.InvoiceDocuments.Any()) {
                message.TransportationService.InvoiceDocuments.ToList().ForEach(d => {
                    d.TransportationServiceId = transportationServiceId;
                });

                _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection).Add(message.TransportationService.InvoiceDocuments);
            }

            SupplyPaymentTask newSupplyPaymentTask = supplyPaymentTaskRepository.GetById(newPaymentTaskId);

            Sender.Tell(new Tuple<SupplyOrder, SupplyPaymentTask>(_supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetByNetId(message.NetId),
                newSupplyPaymentTask));
        });

        Receive<UpdateTransportationServiceMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.TransportationService.InvoiceDocuments.Any()) {
                message.TransportationService.InvoiceDocuments.ToList().ForEach(d => {
                    d.TransportationServiceId = message.TransportationService.Id;
                });

                _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection).Update(message.TransportationService.InvoiceDocuments);
            }

            if (message.TransportationService.ActProvidingServiceDocument != null && !message.TransportationService.ActProvidingServiceDocument.Id.Equals(0))
                _supplyRepositoriesFactory
                    .NewActProvidingServiceDocumentRepository(connection)
                    .Update(message.TransportationService.ActProvidingServiceDocument);

            if (message.TransportationService.SupplyServiceAccountDocument != null && !message.TransportationService.SupplyServiceAccountDocument.Id.Equals(0))
                _supplyRepositoriesFactory
                    .NewSupplyServiceAccountDocumentRepository(connection)
                    .Update(message.TransportationService.SupplyServiceAccountDocument);

            Sender.Tell(_supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetByNetId(message.NetId));
        });

        Receive<GetAllDetailItemsByServiceNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_supplyRepositoriesFactory
                .NewServiceDetailItemRepository(connection)
                .GetAllByNetIdAndType(message.NetId, SupplyServiceType.Transportation)
            );
        });
    }
}