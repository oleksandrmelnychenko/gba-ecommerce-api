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
using GBA.Domain.Messages.Supplies.HelperServices.PlaneDeliveries;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Supplies.HelperServices;

public sealed class PlaneDeliveryServicesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public PlaneDeliveryServicesActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;

        Receive<AddOrUpdatePlaneDeliveryServiceMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IPlaneDeliveryServiceRepository planeDeliveryServiceRepository = _supplyRepositoriesFactory.NewPlaneDeliveryServiceRepository(connection);
            IInvoiceDocumentRepository invoiceDocumentRepository = _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection);
            ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
            ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);

            SupplyOrder supplyOrder = supplyOrderRepository.GetByNetIdIfExist(message.NetId);

            if (supplyOrder != null) {
                User headPolishLogistic = _userRepositoriesFactory.NewUserRepository(connection).GetHeadPolishLogistic();
                long planeDeliveryServiceId = 0;

                if (message.PlaneDeliveryService.PlaneDeliveryOrganization != null)
                    message.PlaneDeliveryService.PlaneDeliveryOrganizationId = message.PlaneDeliveryService.PlaneDeliveryOrganization.Id;

                if (message.PlaneDeliveryService.User != null)
                    message.PlaneDeliveryService.UserId = message.PlaneDeliveryService.User.Id;
                else
                    message.PlaneDeliveryService.UserId = headPolishLogistic.Id;

                if (message.PlaneDeliveryService.SupplyPaymentTask != null) {
                    if (message.PlaneDeliveryService.SupplyPaymentTask.User != null)
                        message.PlaneDeliveryService.SupplyPaymentTask.UserId = message.PlaneDeliveryService.SupplyPaymentTask.User.Id;
                    else
                        message.PlaneDeliveryService.SupplyPaymentTask.UserId = headPolishLogistic.Id;

                    message.PlaneDeliveryService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.PlaneDeliveryService;

                    if (message.PlaneDeliveryService.SupplyPaymentTask.IsNew()) {
                        message.PlaneDeliveryService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;

                        message.PlaneDeliveryService.SupplyPaymentTask.PayToDate = !message.PlaneDeliveryService.SupplyPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow.Date
                            : message.PlaneDeliveryService.SupplyPaymentTask.PayToDate.Value.Date;

                        if (message.PlaneDeliveryService.SupplyOrganizationAgreement != null && !message.PlaneDeliveryService.SupplyOrganizationAgreement.IsNew()) {
                            message.PlaneDeliveryService.SupplyOrganizationAgreement =
                                supplyOrganizationAgreementRepository.GetById(message.PlaneDeliveryService.SupplyOrganizationAgreement.Id);

                            message.PlaneDeliveryService.SupplyOrganizationAgreement.CurrentAmount =
                                Math.Round(message.PlaneDeliveryService.SupplyOrganizationAgreement.CurrentAmount - message.PlaneDeliveryService.GrossPrice, 2);

                            supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.PlaneDeliveryService.SupplyOrganizationAgreement);
                        }

                        message.PlaneDeliveryService.SupplyPaymentTask.NetPrice = message.PlaneDeliveryService.NetPrice;
                        message.PlaneDeliveryService.SupplyPaymentTask.GrossPrice = message.PlaneDeliveryService.GrossPrice;

                        message.PlaneDeliveryService.SupplyPaymentTaskId = _supplyRepositoriesFactory
                            .NewSupplyPaymentTaskRepository(connection)
                            .Add(message.PlaneDeliveryService.SupplyPaymentTask);
                    } else {
                        _supplyRepositoriesFactory
                            .NewSupplyPaymentTaskRepository(connection)
                            .Update(message.PlaneDeliveryService.SupplyPaymentTask);
                    }
                }

                if (message.PlaneDeliveryService.AccountingPaymentTask != null) {
                    if (message.PlaneDeliveryService.AccountingPaymentTask.User != null)
                        message.PlaneDeliveryService.AccountingPaymentTask.UserId = message.PlaneDeliveryService.AccountingPaymentTask.User.Id;
                    else
                        message.PlaneDeliveryService.AccountingPaymentTask.UserId = headPolishLogistic.Id;

                    message.PlaneDeliveryService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.PlaneDeliveryService;

                    if (message.PlaneDeliveryService.AccountingPaymentTask.IsNew()) {
                        message.PlaneDeliveryService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.PlaneDeliveryService.AccountingPaymentTask.IsAccounting = true;

                        message.PlaneDeliveryService.AccountingPaymentTask.PayToDate = !message.PlaneDeliveryService.AccountingPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow.Date
                            : message.PlaneDeliveryService.AccountingPaymentTask.PayToDate.Value.Date;

                        if (message.PlaneDeliveryService.SupplyOrganizationAgreement != null && !message.PlaneDeliveryService.SupplyOrganizationAgreement.IsNew()) {
                            message.PlaneDeliveryService.SupplyOrganizationAgreement =
                                supplyOrganizationAgreementRepository.GetById(message.PlaneDeliveryService.SupplyOrganizationAgreement.Id);

                            message.PlaneDeliveryService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                                Math.Round(
                                    message.PlaneDeliveryService.SupplyOrganizationAgreement.AccountingCurrentAmount - message.PlaneDeliveryService.AccountingGrossPrice,
                                    2);

                            supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.PlaneDeliveryService.SupplyOrganizationAgreement);
                        }

                        message.PlaneDeliveryService.AccountingPaymentTask.NetPrice = message.PlaneDeliveryService.AccountingNetPrice;
                        message.PlaneDeliveryService.AccountingPaymentTask.GrossPrice = message.PlaneDeliveryService.AccountingGrossPrice;

                        message.PlaneDeliveryService.AccountingPaymentTaskId = _supplyRepositoriesFactory
                            .NewSupplyPaymentTaskRepository(connection)
                            .Add(message.PlaneDeliveryService.AccountingPaymentTask);
                    } else {
                        _supplyRepositoriesFactory
                            .NewSupplyPaymentTaskRepository(connection)
                            .Update(message.PlaneDeliveryService.AccountingPaymentTask);
                    }
                }

                if (message.PlaneDeliveryService.IsNew()) {
                    planeDeliveryServiceId = planeDeliveryServiceRepository.Add(message.PlaneDeliveryService);

                    supplyOrder.PlaneDeliveryServiceId = planeDeliveryServiceId;
                    supplyOrderRepository.Update(supplyOrder);

                    supplyOrder.PlaneDeliveryServiceId = planeDeliveryServiceId;
                } else {
                    planeDeliveryServiceId = message.PlaneDeliveryService.Id;
                    planeDeliveryServiceRepository.Update(message.PlaneDeliveryService);
                }

                if (message.PlaneDeliveryService.InvoiceDocuments.Any()) {
                    List<InvoiceDocument> invoiceDocumentsToUpdate = new();
                    List<InvoiceDocument> invoiceDocumentsToAdd = new();

                    foreach (InvoiceDocument invoiceDocument in message.PlaneDeliveryService.InvoiceDocuments)
                        if (invoiceDocument.IsNew()) {
                            invoiceDocument.PlaneDeliveryServiceId = planeDeliveryServiceId;
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

        Receive<UpdatePlaneDeliveryServiceMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.PlaneDeliveryService.InvoiceDocuments.Any()) {
                List<InvoiceDocument> invoiceDocumentsToUpdate = message.PlaneDeliveryService.InvoiceDocuments.Where(d => !d.IsNew()).ToList();

                _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection).Update(invoiceDocumentsToUpdate);
            }

            if (message.PlaneDeliveryService.ActProvidingServiceDocument != null && !message.PlaneDeliveryService.ActProvidingServiceDocument.Id.Equals(0))
                _supplyRepositoriesFactory
                    .NewActProvidingServiceDocumentRepository(connection)
                    .Update(message.PlaneDeliveryService.ActProvidingServiceDocument);

            if (message.PlaneDeliveryService.SupplyServiceAccountDocument != null && !message.PlaneDeliveryService.SupplyServiceAccountDocument.Id.Equals(0))
                _supplyRepositoriesFactory
                    .NewSupplyServiceAccountDocumentRepository(connection)
                    .Update(message.PlaneDeliveryService.SupplyServiceAccountDocument);

            Sender.Tell(_supplyRepositoriesFactory
                .NewSupplyOrderRepository(connection)
                .GetByNetId(message.NetId)
            );
        });

        Receive<GetAllDetailItemsByServiceNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_supplyRepositoriesFactory
                .NewServiceDetailItemRepository(connection)
                .GetAllByNetIdAndType(message.NetId, SupplyServiceType.PlaneDelivery)
            );
        });
    }
}