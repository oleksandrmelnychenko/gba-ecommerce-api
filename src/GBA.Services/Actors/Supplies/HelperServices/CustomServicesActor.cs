using System;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Messages.Supplies.HelperServices;
using GBA.Domain.Messages.Supplies.HelperServices.Customs;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Supplies.HelperServices;

public sealed class CustomServicesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public CustomServicesActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;

        Receive<AddCustomServiceMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ICustomServiceRepository customServiceRepository = _supplyRepositoriesFactory.NewCustomServiceRepository(connection);
            ISupplyPaymentTaskRepository supplyPaymentTaskRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);
            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
            ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);

            SupplyOrder supplyOrder = supplyOrderRepository.GetByNetIdIfExist(message.SupplyOrderNetId);

            if (supplyOrder != null) {
                User headPolishLogistic = userRepository.GetHeadPolishLogistic();

                message.CustomService.UserId = message.CustomService.User?.Id ?? headPolishLogistic.Id;

                if (message.CustomService.SupplyPaymentTask != null) {
                    message.CustomService.SupplyPaymentTask.UserId = message.CustomService.SupplyPaymentTask.User?.Id ?? headPolishLogistic.Id;

                    message.CustomService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomService;
                    message.CustomService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;

                    message.CustomService.SupplyPaymentTask.PayToDate = message.CustomService.SupplyPaymentTask.PayToDate?.Date ?? DateTime.UtcNow.Date;

                    message.CustomService.SupplyPaymentTask.NetPrice = message.CustomService.NetPrice;
                    message.CustomService.SupplyPaymentTask.GrossPrice = message.CustomService.GrossPrice;

                    long paymentTaskId = supplyPaymentTaskRepository
                        .Add(message.CustomService.SupplyPaymentTask);

                    message.CustomService.SupplyPaymentTaskId = paymentTaskId;
                }

                if (message.CustomService.AccountingPaymentTask != null) {
                    message.CustomService.AccountingPaymentTask.UserId = message.CustomService.AccountingPaymentTask.User?.Id ?? headPolishLogistic.Id;

                    message.CustomService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomService;
                    message.CustomService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.CustomService.AccountingPaymentTask.IsAccounting = true;

                    message.CustomService.AccountingPaymentTask.PayToDate = message.CustomService.AccountingPaymentTask.PayToDate?.Date ?? DateTime.UtcNow.Date;

                    message.CustomService.AccountingPaymentTask.NetPrice = message.CustomService.AccountingNetPrice;
                    message.CustomService.AccountingPaymentTask.GrossPrice = message.CustomService.AccountingGrossPrice;

                    long paymentTaskId = supplyPaymentTaskRepository
                        .Add(message.CustomService.AccountingPaymentTask);

                    message.CustomService.AccountingPaymentTaskId = paymentTaskId;
                }

                if (message.CustomService.SupplyOrganizationAgreement != null && !message.CustomService.SupplyOrganizationAgreement.IsNew()) {
                    message.CustomService.SupplyOrganizationAgreement =
                        supplyOrganizationAgreementRepository.GetById(message.CustomService.SupplyOrganizationAgreement.Id);

                    message.CustomService.SupplyOrganizationAgreement.CurrentAmount =
                        Math.Round(
                            message.CustomService.SupplyOrganizationAgreement.CurrentAmount - message.CustomService.GrossPrice,
                            2);

                    message.CustomService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                        Math.Round(
                            message.CustomService.SupplyOrganizationAgreement.AccountingCurrentAmount - message.CustomService.AccountingGrossPrice,
                            2);

                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.CustomService.SupplyOrganizationAgreement);
                }

                if (message.CustomService.ExciseDutyOrganization != null) message.CustomService.ExciseDutyOrganizationId = message.CustomService.ExciseDutyOrganization.Id;

                if (message.CustomService.CustomOrganization != null) message.CustomService.CustomOrganizationId = message.CustomService.CustomOrganization.Id;

                message.CustomService.SupplyOrderId = supplyOrder.Id;

                long customServiceId = customServiceRepository.Add(message.CustomService);

                if (message.CustomService.InvoiceDocuments.Any()) {
                    message.CustomService.InvoiceDocuments.ToList().ForEach(d => {
                        d.CustomServiceId = customServiceId;
                    });

                    _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection).Add(message.CustomService.InvoiceDocuments);
                }

                Sender.Tell(supplyOrderRepository.GetByNetId(message.SupplyOrderNetId));
            } else {
                Sender.Tell(null);
            }
        });

        Receive<GetAllDetailItemsByServiceNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_supplyRepositoriesFactory
                .NewServiceDetailItemRepository(connection)
                .GetAllByNetIdAndType(message.NetId, SupplyServiceType.Custom)
            );
        });

        Receive<UpdateCustomServiceMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.CustomService.InvoiceDocuments.Any())
                _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection).Update(message.CustomService.InvoiceDocuments);

            if (message.CustomService.ActProvidingServiceDocument != null && !message.CustomService.ActProvidingServiceDocument.Id.Equals(0))
                _supplyRepositoriesFactory
                    .NewActProvidingServiceDocumentRepository(connection)
                    .Update(message.CustomService.ActProvidingServiceDocument);

            if (message.CustomService.SupplyServiceAccountDocument != null && !message.CustomService.SupplyServiceAccountDocument.Id.Equals(0))
                _supplyRepositoriesFactory
                    .NewSupplyServiceAccountDocumentRepository(connection)
                    .Update(message.CustomService.SupplyServiceAccountDocument);

            Sender.Tell(_supplyRepositoriesFactory.NewSupplyOrderRepository(connection).GetByNetId(message.NetId));
        });
    }
}