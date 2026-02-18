using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Messages.Supplies.HelperServices;
using GBA.Domain.Messages.Supplies.HelperServices.CustomAgencies;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Supplies.HelperServices;

public sealed class CustomAgencyServicesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public CustomAgencyServicesActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;

        Receive<AddOrUpdateCustomAgencyServiceMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ICustomAgencyServiceRepository customAgencyServiceRepository = _supplyRepositoriesFactory.NewCustomAgencyServiceRepository(connection);
            IInvoiceDocumentRepository invoiceDocumentRepository = _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection);
            ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
            ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);

            SupplyOrder supplyOrder = supplyOrderRepository.GetByNetIdIfExist(message.NetId);

            if (supplyOrder != null) {
                User headPolishLogistic = _userRepositoriesFactory.NewUserRepository(connection).GetHeadPolishLogistic();
                long customAgencyServiceId;

                if (message.CustomAgencyService.CustomAgencyOrganization != null)
                    message.CustomAgencyService.CustomAgencyOrganizationId = message.CustomAgencyService.CustomAgencyOrganization.Id;

                message.CustomAgencyService.UserId = message.CustomAgencyService.User?.Id ?? headPolishLogistic.Id;

                if (message.CustomAgencyService.SupplyPaymentTask != null) {
                    message.CustomAgencyService.SupplyPaymentTask.UserId = message.CustomAgencyService.SupplyPaymentTask.User?.Id ?? headPolishLogistic.Id;

                    message.CustomAgencyService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomAgencyService;

                    if (message.CustomAgencyService.SupplyPaymentTask.IsNew()) {
                        message.CustomAgencyService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;

                        message.CustomAgencyService.SupplyPaymentTask.PayToDate = message.CustomAgencyService.SupplyPaymentTask.PayToDate?.Date ?? DateTime.UtcNow.Date;

                        if (message.CustomAgencyService.SupplyOrganizationAgreement != null && !message.CustomAgencyService.SupplyOrganizationAgreement.IsNew()) {
                            message.CustomAgencyService.SupplyOrganizationAgreement =
                                supplyOrganizationAgreementRepository.GetById(message.CustomAgencyService.SupplyOrganizationAgreement.Id);

                            message.CustomAgencyService.SupplyOrganizationAgreement.CurrentAmount =
                                Math.Round(message.CustomAgencyService.SupplyOrganizationAgreement.CurrentAmount - message.CustomAgencyService.GrossPrice, 2);

                            supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.CustomAgencyService.SupplyOrganizationAgreement);
                        }

                        message.CustomAgencyService.SupplyPaymentTask.NetPrice = message.CustomAgencyService.NetPrice;
                        message.CustomAgencyService.SupplyPaymentTask.GrossPrice = message.CustomAgencyService.GrossPrice;

                        message.CustomAgencyService.SupplyPaymentTaskId = _supplyRepositoriesFactory
                            .NewSupplyPaymentTaskRepository(connection)
                            .Add(message.CustomAgencyService.SupplyPaymentTask);
                    } else {
                        //TODO: if task status changed - updated store dateTime

                        _supplyRepositoriesFactory
                            .NewSupplyPaymentTaskRepository(connection)
                            .Update(message.CustomAgencyService.SupplyPaymentTask);
                    }
                }

                if (message.CustomAgencyService.AccountingPaymentTask != null) {
                    message.CustomAgencyService.AccountingPaymentTask.UserId = message.CustomAgencyService.AccountingPaymentTask.User?.Id ?? headPolishLogistic.Id;

                    message.CustomAgencyService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.CustomAgencyService;

                    if (message.CustomAgencyService.AccountingPaymentTask.IsNew()) {
                        message.CustomAgencyService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.CustomAgencyService.AccountingPaymentTask.IsAccounting = true;

                        message.CustomAgencyService.AccountingPaymentTask.PayToDate =
                            message.CustomAgencyService.AccountingPaymentTask.PayToDate?.Date ?? DateTime.UtcNow.Date;

                        message.CustomAgencyService.AccountingPaymentTask.NetPrice = message.CustomAgencyService.AccountingNetPrice;
                        message.CustomAgencyService.AccountingPaymentTask.GrossPrice = message.CustomAgencyService.AccountingGrossPrice;

                        if (message.CustomAgencyService.SupplyOrganizationAgreement != null && !message.CustomAgencyService.SupplyOrganizationAgreement.IsNew()) {
                            message.CustomAgencyService.SupplyOrganizationAgreement =
                                supplyOrganizationAgreementRepository.GetById(message.CustomAgencyService.SupplyOrganizationAgreement.Id);

                            message.CustomAgencyService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                                Math.Round(
                                    message.CustomAgencyService.SupplyOrganizationAgreement.AccountingCurrentAmount - message.CustomAgencyService.AccountingGrossPrice, 2);

                            supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.CustomAgencyService.SupplyOrganizationAgreement);
                        }

                        message.CustomAgencyService.AccountingPaymentTaskId = _supplyRepositoriesFactory
                            .NewSupplyPaymentTaskRepository(connection)
                            .Add(message.CustomAgencyService.AccountingPaymentTask);
                    } else {
                        //TODO: if task status changed - updated store dateTime

                        _supplyRepositoriesFactory
                            .NewSupplyPaymentTaskRepository(connection)
                            .Update(message.CustomAgencyService.AccountingPaymentTask);
                    }
                }

                if (message.CustomAgencyService.IsNew()) {
                    customAgencyServiceId = customAgencyServiceRepository.Add(message.CustomAgencyService);

                    supplyOrder.CustomAgencyServiceId = customAgencyServiceId;
                    supplyOrderRepository.Update(supplyOrder);

                    supplyOrder.CustomAgencyServiceId = customAgencyServiceId;
                } else {
                    customAgencyServiceId = message.CustomAgencyService.Id;
                    customAgencyServiceRepository.Update(message.CustomAgencyService);
                }

                if (message.CustomAgencyService.InvoiceDocuments.Any()) {
                    List<InvoiceDocument> invoiceDocumentsToUpdate = new();
                    List<InvoiceDocument> invoiceDocumentsToAdd = new();

                    foreach (InvoiceDocument invoiceDocument in message.CustomAgencyService.InvoiceDocuments)
                        if (invoiceDocument.IsNew()) {
                            invoiceDocument.CustomAgencyServiceId = customAgencyServiceId;
                            invoiceDocumentsToAdd.Add(invoiceDocument);
                        } else {
                            invoiceDocumentsToUpdate.Add(invoiceDocument);
                        }

                    if (invoiceDocumentsToUpdate.Any()) invoiceDocumentRepository.Update(invoiceDocumentsToUpdate);

                    if (invoiceDocumentsToAdd.Any()) invoiceDocumentRepository.Add(invoiceDocumentsToAdd);
                }

                Sender.Tell(supplyOrderRepository.GetByNetId(message.NetId));
            } else {
                Sender.Tell(null);
            }
        });

        Receive<UpdateCustomAgencyServiceMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IInvoiceDocumentRepository invoiceDocumentRepository = _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection);

            if (message.CustomAgencyService.InvoiceDocuments.Any()) {
                List<InvoiceDocument> invoiceDocumentsToUpdate = message.CustomAgencyService.InvoiceDocuments.Where(d => !d.IsNew()).ToList();

                invoiceDocumentRepository.Update(invoiceDocumentsToUpdate);
                invoiceDocumentRepository.Add(message.CustomAgencyService.InvoiceDocuments
                    .Where(d => d.IsNew())
                    .Select(d => {
                        d.CustomAgencyServiceId = message.CustomAgencyService.Id;

                        return d;
                    })
                );
            }

            if (message.CustomAgencyService.ActProvidingServiceDocument != null &&
                !message.CustomAgencyService.ActProvidingServiceDocument.Id.Equals(0))
                _supplyRepositoriesFactory
                    .NewActProvidingServiceDocumentRepository(connection)
                    .Update(message.CustomAgencyService.ActProvidingServiceDocument);

            if (message.CustomAgencyService.SupplyServiceAccountDocument != null &&
                !message.CustomAgencyService.SupplyServiceAccountDocument.Id.Equals(0))
                _supplyRepositoriesFactory
                    .NewSupplyServiceAccountDocumentRepository(connection)
                    .Update(message.CustomAgencyService.SupplyServiceAccountDocument);

            Sender.Tell(_supplyRepositoriesFactory
                .NewSupplyOrderRepository(connection)
                .GetByNetId(message.NetId)
            );
        });

        Receive<GetAllDetailItemsByServiceNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_supplyRepositoriesFactory
                .NewServiceDetailItemRepository(connection)
                .GetAllByNetIdAndType(message.NetId, SupplyServiceType.CustomAgency)
            );
        });
    }
}