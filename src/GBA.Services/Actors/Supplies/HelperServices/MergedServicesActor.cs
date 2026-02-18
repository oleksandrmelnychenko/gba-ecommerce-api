using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using Akka.Util.Internal;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.ActProvidingServices;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.Supplies.PackingLists;
using GBA.Domain.Messages.Supplies.HelperServices;
using GBA.Domain.Messages.Supplies.HelperServices.Mergeds;
using GBA.Domain.Messages.Supplies.Invoices;
using GBA.Domain.Repositories.Supplies.ActProvidingServices.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.DeliveryProductProtocols.Contracts;
using GBA.Domain.Repositories.Supplies.Documents.Contracts;
using GBA.Domain.Repositories.Supplies.HelperServices.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Supplies.HelperServices;

public sealed class MergedServicesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;

    private readonly string _defaultComment = "��� ������� � 1�.";
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public MergedServicesActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;

        Receive<GetAllDetailItemsByServiceNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _supplyRepositoriesFactory
                    .NewServiceDetailItemRepository(connection)
                    .GetAllByNetIdAndType(
                        message.NetId,
                        SupplyServiceType.Merged
                    )
            );
        });

        Receive<AddOrUpdateMergedServiceMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
            IMergedServiceRepository mergedServiceRepository = _supplyRepositoriesFactory.NewMergedServiceRepository(connection);
            IInvoiceDocumentRepository invoiceDocumentRepository = _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection);
            ISupplyPaymentTaskRepository supplyPaymentTaskRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);
            IServiceDetailItemRepository serviceDetailItemRepository = _supplyRepositoriesFactory.NewServiceDetailItemRepository(connection);
            IServiceDetailItemKeyRepository serviceDetailItemKeyRepository = _supplyRepositoriesFactory.NewServiceDetailItemKeyRepository(connection);
            ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);
            ISupplyServiceNumberRepository supplyServiceNumberRepository = _supplyRepositoriesFactory.NewSupplyServiceNumberRepository(connection);
            ISupplyInformationTaskRepository supplyInformationTaskRepository =
                _supplyRepositoriesFactory.NewSupplyInformationTaskRepository(connection);
            IActProvidingServiceDocumentRepository actProvidingServiceDocumentRepository =
                _supplyRepositoriesFactory.NewActProvidingServiceDocumentRepository(connection);
            ISupplyServiceAccountDocumentRepository supplyServiceAccountDocumentRepository =
                _supplyRepositoriesFactory.NewSupplyServiceAccountDocumentRepository(connection);

            User updatedBy = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

            SupplyOrder supplyOrder = supplyOrderRepository.GetByNetIdIfExist(message.SupplyOrderNetId);

            if (supplyOrder.IsCompleted && !updatedBy.UserRole.UserRoleType.Equals(UserRoleType.GBA)) {
                Sender.Tell(new Exception(MergedServiceResourceNames.MANAGE_ERROR));
                return;
            }

            if (supplyOrder != null) {
                message.MergedService.UserId = updatedBy.Id;

                message.MergedService.NetPrice =
                    decimal.Round(message.MergedService.GrossPrice * 100 / Convert.ToDecimal(100 + message.MergedService.VatPercent), 2, MidpointRounding.AwayFromZero);
                message.MergedService.Vat =
                    decimal.Round(message.MergedService.GrossPrice - message.MergedService.NetPrice, 2, MidpointRounding.AwayFromZero);

                message.MergedService.AccountingNetPrice =
                    decimal.Round(message.MergedService.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + message.MergedService.AccountingVatPercent), 2,
                        MidpointRounding.AwayFromZero);
                message.MergedService.AccountingVat =
                    decimal.Round(message.MergedService.AccountingGrossPrice - message.MergedService.AccountingNetPrice, 2, MidpointRounding.AwayFromZero);

                if (message.MergedService.Id.Equals(0)) {
                    SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                    if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                        message.MergedService.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                    else
                        message.MergedService.ServiceNumber = string.Format("P{0:D10}", 1);

                    supplyServiceNumberRepository.Add(message.MergedService.ServiceNumber);
                }

                if (message.MergedService.SupplyPaymentTask != null && message.MergedService.SupplyPaymentTask.IsNew()) {
                    message.MergedService.SupplyPaymentTask.UserId = message.MergedService.SupplyPaymentTask.User?.Id ?? updatedBy.Id;

                    message.MergedService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.MergedService;
                    message.MergedService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;

                    message.MergedService.SupplyPaymentTask.PayToDate =
                        !message.MergedService.SupplyPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.MergedService.SupplyPaymentTask.PayToDate.Value);

                    message.MergedService.SupplyPaymentTask.NetPrice = message.MergedService.NetPrice;
                    message.MergedService.SupplyPaymentTask.GrossPrice = message.MergedService.GrossPrice;

                    message.MergedService.SupplyPaymentTaskId =
                        supplyPaymentTaskRepository
                            .Add(message.MergedService.SupplyPaymentTask);
                }

                if (message.MergedService.AccountingPaymentTask != null && message.MergedService.AccountingPaymentTask.IsNew()) {
                    message.MergedService.AccountingPaymentTask.UserId = message.MergedService.AccountingPaymentTask.User?.Id ?? updatedBy.Id;

                    message.MergedService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.MergedService;
                    message.MergedService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.MergedService.AccountingPaymentTask.IsAccounting = true;

                    message.MergedService.AccountingPaymentTask.PayToDate =
                        !message.MergedService.AccountingPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.MergedService.AccountingPaymentTask.PayToDate.Value);

                    message.MergedService.AccountingPaymentTask.NetPrice = message.MergedService.AccountingNetPrice;
                    message.MergedService.AccountingPaymentTask.GrossPrice = message.MergedService.AccountingGrossPrice;

                    message.MergedService.AccountingPaymentTaskId =
                        supplyPaymentTaskRepository
                            .Add(message.MergedService.AccountingPaymentTask);
                }

                if (message.MergedService.SupplyInformationTask != null) {
                    if (message.MergedService.SupplyInformationTask.IsNew()) {
                        message.MergedService.SupplyInformationTask.UserId = updatedBy.Id;
                        message.MergedService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                        message.MergedService.AccountingSupplyCostsWithinCountry =
                            message.MergedService.SupplyInformationTask.GrossPrice;

                        message.MergedService.SupplyInformationTaskId =
                            supplyInformationTaskRepository.Add(message.MergedService.SupplyInformationTask);
                    } else {
                        if (message.MergedService.SupplyInformationTask.Deleted) {
                            message.MergedService.SupplyInformationTask.DeletedById = updatedBy.Id;

                            supplyInformationTaskRepository.Remove(message.MergedService.SupplyInformationTask);

                            message.MergedService.SupplyInformationTaskId = null;
                        } else {
                            message.MergedService.SupplyInformationTask.UpdatedById = updatedBy.Id;
                            message.MergedService.SupplyInformationTask.UserId = updatedBy.Id;

                            message.MergedService.AccountingSupplyCostsWithinCountry =
                                message.MergedService.SupplyInformationTask.GrossPrice;

                            supplyInformationTaskRepository.Update(message.MergedService.SupplyInformationTask);
                        }
                    }
                }

                if (message.MergedService.ActProvidingServiceDocument != null) {
                    if (message.MergedService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.MergedService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.MergedService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.MergedService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.MergedService.ActProvidingServiceDocument);
                    } else if (message.MergedService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.MergedService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.MergedService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.MergedService.SupplyServiceAccountDocument != null) {
                    if (message.MergedService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.MergedService.SupplyServiceAccountDocument.Number = string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.MergedService.SupplyServiceAccountDocument.Number = string.Format("{0:D10}", 1);

                        message.MergedService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.MergedService.SupplyServiceAccountDocument);
                    } else if (message.MergedService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        supplyServiceAccountDocumentRepository.RemoveById(message.MergedService.SupplyServiceAccountDocument.Id);
                        message.MergedService.SupplyServiceAccountDocumentId = null;
                    }
                }

                if (message.MergedService.SupplyOrganizationAgreement != null && !message.MergedService.SupplyOrganizationAgreement.IsNew()) {
                    message.MergedService.SupplyOrganizationAgreement =
                        supplyOrganizationAgreementRepository.GetById(message.MergedService.SupplyOrganizationAgreement.Id);

                    message.MergedService.SupplyOrganizationAgreement.CurrentAmount =
                        Math.Round(message.MergedService.SupplyOrganizationAgreement.CurrentAmount - message.MergedService.GrossPrice, 2);

                    message.MergedService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                        Math.Round(message.MergedService.SupplyOrganizationAgreement.AccountingCurrentAmount - message.MergedService.AccountingGrossPrice, 2);

                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.MergedService.SupplyOrganizationAgreement);
                }

                if (message.MergedService.FromDate.HasValue) message.MergedService.FromDate = TimeZoneInfo.ConvertTimeToUtc(message.MergedService.FromDate.Value);

                if (message.MergedService.IsNew()) {
                    message.MergedService.SupplyOrganizationId = message.MergedService.SupplyOrganization.Id;
                    message.MergedService.SupplyOrganizationAgreementId = message.MergedService.SupplyOrganizationAgreement.Id;
                    message.MergedService.SupplyOrderId = supplyOrder.Id;

                    message.MergedService.Id = mergedServiceRepository.Add(message.MergedService);
                } else {
                    mergedServiceRepository.Update(message.MergedService);
                }

                if (message.MergedService.InvoiceDocuments.Any())
                    invoiceDocumentRepository
                        .RemoveAllByMergedServiceIdExceptProvided(
                            message.MergedService.Id,
                            message.MergedService.InvoiceDocuments.Where(d => !d.IsNew() && !d.Deleted).Select(d => d.Id)
                        );
                else
                    invoiceDocumentRepository.RemoveAllByMergedServiceId(message.MergedService.Id);

                invoiceDocumentRepository.Add(
                    message.MergedService.InvoiceDocuments
                        .Where(d => d.IsNew())
                        .Select(d => {
                            d.MergedServiceId = message.MergedService.Id;

                            return d;
                        })
                );

                invoiceDocumentRepository.Update(
                    message.MergedService.InvoiceDocuments
                        .Where(d => !d.IsNew())
                        .Select(d => {
                            d.MergedServiceId = message.MergedService.Id;

                            return d;
                        })
                );

                if (message.MergedService.ServiceDetailItems.Any()) {
                    serviceDetailItemRepository.RemoveAllByMergedServiceIdExceptProvided(
                        message.MergedService.Id,
                        message.MergedService.ServiceDetailItems.Where(i => !i.IsNew()).Select(i => i.Id)
                    );

                    foreach (ServiceDetailItem item in message.MergedService.ServiceDetailItems) {
                        if (item.ServiceDetailItemKey != null)
                            item.ServiceDetailItemKeyId =
                                item.ServiceDetailItemKey.IsNew()
                                    ? serviceDetailItemKeyRepository.Add(item.ServiceDetailItemKey)
                                    : item.ServiceDetailItemKey.Id;

                        item.NetPrice = Math.Round(item.GrossPrice * 100 / Convert.ToDecimal(100 + item.VatPercent), 2);
                        item.Vat = Math.Round(item.GrossPrice - item.NetPrice, 2);
                    }

                    if (message.MergedService.ServiceDetailItems.Any(i => i.IsNew()))
                        serviceDetailItemRepository.Add(message.MergedService.ServiceDetailItems.Where(i => i.IsNew()));
                    if (message.MergedService.ServiceDetailItems.Any(i => !i.IsNew()))
                        serviceDetailItemRepository.Update(message.MergedService.ServiceDetailItems.Where(i => !i.IsNew()));
                } else {
                    serviceDetailItemRepository.RemoveAllByMergedServiceId(message.MergedService.Id);
                }

                Sender.Tell(
                    new Tuple<SupplyOrder, SupplyPaymentTask, SupplyPaymentTask>(
                        supplyOrderRepository.GetByNetId(message.SupplyOrderNetId),
                        message.MergedService.SupplyPaymentTaskId.HasValue
                            ? supplyPaymentTaskRepository.GetById(message.MergedService.SupplyPaymentTaskId.Value)
                            : null,
                        message.MergedService.AccountingPaymentTaskId.HasValue
                            ? supplyPaymentTaskRepository.GetById(message.MergedService.AccountingPaymentTaskId.Value)
                            : null
                    )
                );

                ActorReferenceManager.Instance.Get(SupplyActorNames.SUPPLY_INVOICE_ACTOR).Tell(new UpdateSupplyInvoiceItemGrossPriceMessage(
                    _supplyRepositoriesFactory
                        .NewSupplyInvoiceRepository(connection)
                        .GetIdBySupplyOrderId(supplyOrder.Id),
                    updatedBy.NetUid
                ));
            } else {
                Sender.Tell(new Tuple<SupplyOrder, SupplyPaymentTask, SupplyPaymentTask>(null, null, null));
            }
        });

        Receive<AddOrUpdateMergedServiceUkraineMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IMergedServiceRepository mergedServiceRepository = _supplyRepositoriesFactory.NewMergedServiceRepository(connection);
            IInvoiceDocumentRepository invoiceDocumentRepository = _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection);
            ISupplyPaymentTaskRepository supplyPaymentTaskRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);
            IServiceDetailItemRepository serviceDetailItemRepository = _supplyRepositoriesFactory.NewServiceDetailItemRepository(connection);
            ISupplyOrderUkraineRepository supplyOrderUkraineRepository = _supplyUkraineRepositoriesFactory.NewSupplyOrderUkraineRepository(connection);
            IServiceDetailItemKeyRepository serviceDetailItemKeyRepository = _supplyRepositoriesFactory.NewServiceDetailItemKeyRepository(connection);
            ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);
            ISupplyServiceNumberRepository supplyServiceNumberRepository = _supplyRepositoriesFactory.NewSupplyServiceNumberRepository(connection);
            IActProvidingServiceDocumentRepository actProvidingServiceDocumentRepository =
                _supplyRepositoriesFactory.NewActProvidingServiceDocumentRepository(connection);
            ISupplyServiceAccountDocumentRepository supplyServiceAccountDocumentRepository =
                _supplyRepositoriesFactory.NewSupplyServiceAccountDocumentRepository(connection);

            User updatedBy = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            SupplyOrderUkraine supplyOrderUkraine = supplyOrderUkraineRepository.GetByNetId(message.SupplyOrderUkraineNetId);

            if (supplyOrderUkraine != null) {
                message.MergedService.UserId = updatedBy.Id;

                if (message.MergedService.Id.Equals(0)) {
                    SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                    if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                        message.MergedService.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                    else
                        message.MergedService.ServiceNumber = string.Format("P{0:D10}", 1);

                    supplyServiceNumberRepository.Add(message.MergedService.ServiceNumber);
                }

                message.MergedService.NetPrice =
                    decimal.Round(message.MergedService.GrossPrice * 100 / Convert.ToDecimal(100 + message.MergedService.VatPercent), 2, MidpointRounding.AwayFromZero);
                message.MergedService.Vat =
                    decimal.Round(message.MergedService.GrossPrice - message.MergedService.NetPrice, 2, MidpointRounding.AwayFromZero);

                message.MergedService.AccountingNetPrice =
                    decimal.Round(message.MergedService.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + message.MergedService.AccountingVatPercent), 2,
                        MidpointRounding.AwayFromZero);
                message.MergedService.AccountingVat =
                    decimal.Round(message.MergedService.AccountingGrossPrice - message.MergedService.AccountingNetPrice, 2, MidpointRounding.AwayFromZero);

                if (message.MergedService.SupplyPaymentTask != null && message.MergedService.SupplyPaymentTask.IsNew()) {
                    message.MergedService.SupplyPaymentTask.UserId = message.MergedService.SupplyPaymentTask.User?.Id ?? updatedBy.Id;

                    message.MergedService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.MergedService;
                    message.MergedService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;

                    message.MergedService.SupplyPaymentTask.PayToDate =
                        !message.MergedService.SupplyPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.MergedService.SupplyPaymentTask.PayToDate.Value);

                    supplyPaymentTaskRepository.RemoveById(message.MergedService.SupplyPaymentTaskId ?? 0, updatedBy.Id);

                    message.MergedService.SupplyPaymentTask.NetPrice = message.MergedService.NetPrice;
                    message.MergedService.SupplyPaymentTask.GrossPrice = message.MergedService.GrossPrice;

                    message.MergedService.SupplyPaymentTaskId =
                        supplyPaymentTaskRepository
                            .Add(message.MergedService.SupplyPaymentTask);
                }

                if (message.MergedService.AccountingPaymentTask != null && message.MergedService.AccountingPaymentTask.IsNew()) {
                    message.MergedService.AccountingPaymentTask.UserId = message.MergedService.AccountingPaymentTask.User?.Id ?? updatedBy.Id;

                    message.MergedService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.MergedService;
                    message.MergedService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.MergedService.AccountingPaymentTask.IsAccounting = true;

                    message.MergedService.AccountingPaymentTask.PayToDate =
                        !message.MergedService.AccountingPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.MergedService.AccountingPaymentTask.PayToDate.Value);

                    supplyPaymentTaskRepository.RemoveById(message.MergedService.SupplyPaymentTaskId ?? 0, updatedBy.Id);

                    message.MergedService.AccountingPaymentTask.NetPrice = message.MergedService.AccountingNetPrice;
                    message.MergedService.AccountingPaymentTask.GrossPrice = message.MergedService.AccountingGrossPrice;

                    message.MergedService.AccountingPaymentTaskId =
                        supplyPaymentTaskRepository
                            .Add(message.MergedService.AccountingPaymentTask);
                }

                if (message.MergedService.SupplyOrganizationAgreement != null && !message.MergedService.SupplyOrganizationAgreement.IsNew()) {
                    message.MergedService.SupplyOrganizationAgreement =
                        supplyOrganizationAgreementRepository.GetById(message.MergedService.SupplyOrganizationAgreement.Id);

                    message.MergedService.SupplyOrganizationAgreement.CurrentAmount =
                        Math.Round(
                            message.MergedService.SupplyOrganizationAgreement.CurrentAmount - message.MergedService.GrossPrice,
                            2);

                    message.MergedService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                        Math.Round(
                            message.MergedService.SupplyOrganizationAgreement.AccountingCurrentAmount - message.MergedService.AccountingGrossPrice,
                            2);

                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.MergedService.SupplyOrganizationAgreement);
                }

                if (message.MergedService.ActProvidingServiceDocument != null) {
                    if (message.MergedService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.MergedService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.MergedService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.MergedService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.MergedService.ActProvidingServiceDocument);
                    } else if (message.MergedService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.MergedService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.MergedService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.MergedService.SupplyServiceAccountDocument != null) {
                    if (message.MergedService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.MergedService.SupplyServiceAccountDocument.Number = string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.MergedService.SupplyServiceAccountDocument.Number = string.Format("{0:D10}", 1);

                        message.MergedService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.MergedService.SupplyServiceAccountDocument);
                    } else if (message.MergedService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        supplyServiceAccountDocumentRepository.RemoveById(message.MergedService.SupplyServiceAccountDocument.Id);
                        message.MergedService.SupplyServiceAccountDocumentId = null;
                    }
                }

                if (message.MergedService.FromDate.HasValue) message.MergedService.FromDate = TimeZoneInfo.ConvertTimeToUtc(message.MergedService.FromDate.Value);

                if (message.MergedService.IsNew()) {
                    message.MergedService.SupplyOrganizationId = message.MergedService.SupplyOrganization.Id;
                    message.MergedService.SupplyOrganizationAgreementId = message.MergedService.SupplyOrganizationAgreement.Id;
                    message.MergedService.SupplyOrderUkraineId = supplyOrderUkraine.Id;

                    message.MergedService.Id = mergedServiceRepository.Add(message.MergedService);
                } else {
                    mergedServiceRepository.Update(message.MergedService);
                }

                if (message.MergedService.InvoiceDocuments.Any())
                    invoiceDocumentRepository
                        .RemoveAllByMergedServiceIdExceptProvided(
                            message.MergedService.Id,
                            message.MergedService.InvoiceDocuments.Where(d => !d.IsNew() && !d.Deleted).Select(d => d.Id)
                        );
                else
                    invoiceDocumentRepository.RemoveAllByMergedServiceId(message.MergedService.Id);

                invoiceDocumentRepository.Add(
                    message.MergedService.InvoiceDocuments
                        .Where(d => d.IsNew())
                        .Select(d => {
                            d.MergedServiceId = message.MergedService.Id;

                            return d;
                        })
                );

                invoiceDocumentRepository.Update(
                    message.MergedService.InvoiceDocuments
                        .Where(d => !d.IsNew())
                        .Select(d => {
                            d.MergedServiceId = message.MergedService.Id;

                            return d;
                        })
                );

                if (message.MergedService.ServiceDetailItems.Any()) {
                    serviceDetailItemRepository.RemoveAllByMergedServiceIdExceptProvided(
                        message.MergedService.Id,
                        message.MergedService.ServiceDetailItems.Where(i => !i.IsNew()).Select(i => i.Id)
                    );

                    foreach (ServiceDetailItem item in message.MergedService.ServiceDetailItems) {
                        if (item.ServiceDetailItemKey != null)
                            item.ServiceDetailItemKeyId = item.ServiceDetailItemKey.IsNew()
                                ? serviceDetailItemKeyRepository.Add(item.ServiceDetailItemKey)
                                : item.ServiceDetailItemKey.Id;

                        item.NetPrice = Math.Round(item.GrossPrice * 100 / Convert.ToDecimal(100 + item.VatPercent), 2);
                        item.Vat = Math.Round(item.GrossPrice - item.NetPrice, 2);
                    }

                    if (message.MergedService.ServiceDetailItems.Any(i => i.IsNew()))
                        serviceDetailItemRepository.Add(message.MergedService.ServiceDetailItems.Where(i => i.IsNew()));
                    if (message.MergedService.ServiceDetailItems.Any(i => !i.IsNew()))
                        serviceDetailItemRepository.Update(message.MergedService.ServiceDetailItems.Where(i => !i.IsNew()));
                } else {
                    serviceDetailItemRepository.RemoveAllByMergedServiceId(message.MergedService.Id);
                }

                Sender.Tell(
                    new Tuple<SupplyOrderUkraine, SupplyPaymentTask, SupplyPaymentTask>(
                        supplyOrderUkraineRepository.GetByNetId(message.SupplyOrderUkraineNetId),
                        message.MergedService.SupplyPaymentTaskId.HasValue
                            ? supplyPaymentTaskRepository.GetById(message.MergedService.SupplyPaymentTaskId.Value)
                            : null,
                        message.MergedService.AccountingPaymentTaskId.HasValue
                            ? supplyPaymentTaskRepository.GetById(message.MergedService.AccountingPaymentTaskId.Value)
                            : null
                    )
                );
            } else {
                Sender.Tell(new Tuple<SupplyOrderUkraine, SupplyPaymentTask, SupplyPaymentTask>(null, null, null));
            }
        });

        Receive<ManageMergedServiceMessage>(ProcessManageMergedService);

        Receive<RemoveMergedServiceBeforeCalculatedGrossPriceMessage>(ProcessRemoveMergedServiceBeforeCalculatedGrossPrice);

        Receive<UpdateMergedServiceExtraChargeMessage>(ProcessUpdateMergedServiceExtraCharge);

        Receive<AddSupplyInvoicesToMergedServiceMessage>(ProcessAddSupplyInvoicesToMergedService);

        Receive<ResetValueMergedServiceMessage>(ResetValueMergedService);
    }

    private void ProcessManageMergedService(ManageMergedServiceMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyPaymentTaskRepository supplyPaymentTaskRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);
            IInvoiceDocumentRepository invoiceDocumentRepository = _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection);
            IDeliveryProductProtocolRepository deliveryProductProtocolRepository = _supplyRepositoriesFactory.NewDeliveryProductProtocolRepository(connection);
            ISupplyServiceNumberRepository supplyServiceNumberRepository = _supplyRepositoriesFactory.NewSupplyServiceNumberRepository(connection);
            ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);
            IMergedServiceRepository mergedServiceRepository = _supplyRepositoriesFactory.NewMergedServiceRepository(connection);
            ISupplyPaymentTaskDocumentRepository supplyPaymentTaskDocumentRepository =
                _supplyRepositoriesFactory.NewSupplyPaymentTaskDocumentRepository(connection);
            ISupplyInformationTaskRepository supplyInformationTaskRepository =
                _supplyRepositoriesFactory.NewSupplyInformationTaskRepository(connection);
            IActProvidingServiceDocumentRepository actProvidingServiceDocumentRepository =
                _supplyRepositoriesFactory.NewActProvidingServiceDocumentRepository(connection);
            ISupplyServiceAccountDocumentRepository supplyServiceAccountDocumentRepository =
                _supplyRepositoriesFactory.NewSupplyServiceAccountDocumentRepository(connection);
            IActProvidingServiceRepository actProvidingServiceRepository =
                _supplyRepositoriesFactory.NewActProvidingServiceRepository(connection);

            DeliveryProductProtocol protocol = deliveryProductProtocolRepository
                .GetByNetId(message.NetId);

            User updatedBy = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

            if (!protocol.IsNew() && protocol.IsCompleted && !updatedBy.UserRole.UserRoleType.Equals(UserRoleType.GBA)) {
                Sender.Tell(new Exception(MergedServiceResourceNames.MANAGE_ERROR));
                return;
            }

            message.MergedService.User = updatedBy;
            message.MergedService.UserId = message.MergedService.User.Id;

            if (message.MergedService.Id.Equals(0)) {
                message.MergedService.SupplyOrganizationId = message.MergedService.SupplyOrganization.Id;
                message.MergedService.NetPrice = Math.Round(message.MergedService.GrossPrice * 100 / Convert.ToDecimal(100 + message.MergedService.VatPercent), 2);
                message.MergedService.AccountingNetPrice =
                    Math.Round(message.MergedService.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + message.MergedService.AccountingVatPercent), 2);
                message.MergedService.Vat = Math.Round(message.MergedService.GrossPrice - message.MergedService.NetPrice, 2);
                message.MergedService.AccountingVat = Math.Round(message.MergedService.AccountingGrossPrice - message.MergedService.AccountingNetPrice, 2);

                message.MergedService.ConsumableProductId = message.MergedService.ConsumableProduct.Id;

                if (message.MergedService.SupplyPaymentTask != null) {
                    if (message.MergedService.SupplyPaymentTask.User != null)
                        message.MergedService.SupplyPaymentTask.UserId = message.MergedService.SupplyPaymentTask.User.Id;

                    message.MergedService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.MergedService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.MergedService;

                    message.MergedService.SupplyPaymentTask.PayToDate =
                        !message.MergedService.SupplyPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.MergedService.SupplyPaymentTask.PayToDate.Value);

                    message.MergedService.SupplyPaymentTask.NetPrice = message.MergedService.NetPrice;
                    message.MergedService.SupplyPaymentTask.GrossPrice = message.MergedService.GrossPrice;

                    message.MergedService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(message.MergedService.SupplyPaymentTask);

                    if (message.MergedService.SupplyPaymentTask.SupplyPaymentTaskDocuments.Any())
                        foreach (SupplyPaymentTaskDocument document in message.MergedService.SupplyPaymentTask.SupplyPaymentTaskDocuments) {
                            document.SupplyPaymentTaskId = message.MergedService.SupplyPaymentTaskId.Value;

                            supplyPaymentTaskDocumentRepository.Add(document);
                        }
                }

                if (message.MergedService.AccountingPaymentTask != null) {
                    if (message.MergedService.AccountingPaymentTask.User != null)
                        message.MergedService.AccountingPaymentTask.UserId = message.MergedService.AccountingPaymentTask.User.Id;
                    message.MergedService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.MergedService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.MergedService;
                    message.MergedService.AccountingPaymentTask.IsAccounting = true;

                    message.MergedService.AccountingPaymentTask.PayToDate =
                        !message.MergedService.AccountingPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.MergedService.AccountingPaymentTask.PayToDate.Value);

                    message.MergedService.AccountingPaymentTask.NetPrice = message.MergedService.AccountingNetPrice;
                    message.MergedService.AccountingPaymentTask.GrossPrice = message.MergedService.AccountingGrossPrice;

                    message.MergedService.AccountingPaymentTaskId =
                        supplyPaymentTaskRepository.Add(message.MergedService.AccountingPaymentTask);

                    if (message.MergedService.AccountingPaymentTask.SupplyPaymentTaskDocuments.Any())
                        foreach (SupplyPaymentTaskDocument document in message.MergedService.AccountingPaymentTask.SupplyPaymentTaskDocuments) {
                            document.SupplyPaymentTaskId = message.MergedService.AccountingPaymentTaskId.Value;

                            supplyPaymentTaskDocumentRepository.Add(document);
                        }
                }

                if (message.MergedService.ActProvidingService != null) {
                    if (message.MergedService.User != null)
                        message.MergedService.ActProvidingService.UserId = message.MergedService.User.Id;

                    message.MergedService.ActProvidingService.IsAccounting = false;

                    message.MergedService.ActProvidingService.Price =
                        message.MergedService.GrossPrice;

                    message.MergedService.ActProvidingService.FromDate = DateTime.UtcNow;

                    ActProvidingService lastAct = actProvidingServiceRepository.GetLastRecord(_defaultComment);

                    if (lastAct != null && lastAct.Created.Year.Equals(DateTime.Now.Year) && !string.IsNullOrEmpty(lastAct.Number))
                        message.MergedService.ActProvidingService.Number = string.Format("P{0:D10}", int.Parse(lastAct.Number.Substring(1)) + 1);
                    else
                        message.MergedService.ActProvidingService.Number = string.Format("P{0:D10}", 1);

                    message.MergedService.ActProvidingServiceId =
                        actProvidingServiceRepository.New(message.MergedService.ActProvidingService);

                    if (!message.MergedService.SupplyOrganizationAgreement.IsNew()) {
                        message.MergedService.SupplyOrganizationAgreement =
                            supplyOrganizationAgreementRepository.GetById(message.MergedService.SupplyOrganizationAgreement.Id);

                        message.MergedService.SupplyOrganizationAgreement.CurrentAmount =
                            Math.Round(
                                message.MergedService.SupplyOrganizationAgreement.CurrentAmount - message.MergedService.GrossPrice, 2);

                        supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.MergedService.SupplyOrganizationAgreement);
                    }
                }

                if (message.MergedService.AccountingActProvidingService != null) {
                    if (message.MergedService.User != null)
                        message.MergedService.AccountingActProvidingService.UserId = message.MergedService.User.Id;

                    message.MergedService.AccountingActProvidingService.IsAccounting = true;

                    message.MergedService.AccountingActProvidingService.Price =
                        message.MergedService.AccountingGrossPrice;

                    message.MergedService.AccountingActProvidingService.FromDate = DateTime.UtcNow;

                    ActProvidingService lastAct = actProvidingServiceRepository.GetLastRecord(_defaultComment);

                    if (lastAct != null && lastAct.Created.Year.Equals(DateTime.Now.Year) && !string.IsNullOrEmpty(lastAct.Number))
                        message.MergedService.AccountingActProvidingService.Number = string.Format("P{0:D10}", int.Parse(lastAct.Number.Substring(1)) + 1);
                    else
                        message.MergedService.AccountingActProvidingService.Number = string.Format("P{0:D10}", 1);

                    message.MergedService.AccountingActProvidingServiceId =
                        actProvidingServiceRepository.New(message.MergedService.AccountingActProvidingService);

                    if (!message.MergedService.SupplyOrganizationAgreement.IsNew()) {
                        message.MergedService.SupplyOrganizationAgreement =
                            supplyOrganizationAgreementRepository.GetById(message.MergedService.SupplyOrganizationAgreement.Id);

                        message.MergedService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                            Math.Round(
                                message.MergedService.SupplyOrganizationAgreement.AccountingCurrentAmount -
                                message.MergedService.AccountingGrossPrice, 2);

                        supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.MergedService.SupplyOrganizationAgreement);
                    }
                }

                if (message.MergedService.SupplyInformationTask != null) {
                    if (message.MergedService.SupplyInformationTask.User == null)
                        message.MergedService.SupplyInformationTask.UserId = message.MergedService.User.Id;
                    else
                        message.MergedService.SupplyInformationTask.UserId = message.MergedService.SupplyInformationTask.User.Id;
                    message.MergedService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                    message.MergedService.AccountingSupplyCostsWithinCountry =
                        message.MergedService.SupplyInformationTask.GrossPrice;

                    message.MergedService.SupplyInformationTaskId =
                        supplyInformationTaskRepository.Add(message.MergedService.SupplyInformationTask);
                }

                if (message.MergedService.ActProvidingServiceDocument != null) {
                    if (message.MergedService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.MergedService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.MergedService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.MergedService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.MergedService.ActProvidingServiceDocument);
                    } else if (message.MergedService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.MergedService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.MergedService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.MergedService.SupplyServiceAccountDocument != null) {
                    if (message.MergedService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.MergedService.SupplyServiceAccountDocument.Number = string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.MergedService.SupplyServiceAccountDocument.Number = string.Format("{0:D10}", 1);

                        message.MergedService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.MergedService.SupplyServiceAccountDocument);
                    } else if (message.MergedService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        supplyServiceAccountDocumentRepository.RemoveById(message.MergedService.SupplyServiceAccountDocument.Id);
                        message.MergedService.SupplyServiceAccountDocumentId = null;
                    }
                }

                SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                    message.MergedService.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                else
                    message.MergedService.ServiceNumber = string.Format("P{0:D10}", 1);

                supplyServiceNumberRepository.Add(message.MergedService.ServiceNumber);

                if (message.MergedService.SupplyOrganizationAgreement != null)
                    message.MergedService.SupplyOrganizationAgreementId = message.MergedService.SupplyOrganizationAgreement.Id;

                if (message.MergedService.FromDate.HasValue)
                    message.MergedService.FromDate = TimeZoneInfo.ConvertTimeToUtc(message.MergedService.FromDate.Value);

                message.MergedService.DeliveryProductProtocolId = protocol.Id;

                message.MergedService.Id = mergedServiceRepository.Add(message.MergedService);

                if (message.MergedService.InvoiceDocuments.Any())
                    message.MergedService.InvoiceDocuments.ForEach(document => {
                        document.MergedServiceId = message.MergedService.Id;

                        invoiceDocumentRepository.Add(document);
                    });
            } else {
                MergedService existMergedService = mergedServiceRepository.GetByIdWithoutIncludes(message.MergedService.Id);

                bool isWithoutResetGrossPriceValue = existMergedService.Equals(message.MergedService);

                message.MergedService.NetPrice = Math.Round(message.MergedService.GrossPrice * 100 / Convert.ToDecimal(100 + message.MergedService.VatPercent), 2);
                message.MergedService.AccountingNetPrice =
                    Math.Round(message.MergedService.AccountingGrossPrice * 100 / Convert.ToDecimal(100 + message.MergedService.AccountingVatPercent), 2);
                message.MergedService.Vat = Math.Round(message.MergedService.GrossPrice - message.MergedService.NetPrice, 2);
                message.MergedService.AccountingVat = Math.Round(message.MergedService.AccountingGrossPrice - message.MergedService.AccountingNetPrice, 2);

                message.MergedService.ConsumableProductId = message.MergedService.ConsumableProduct.Id;

                UpdateSupplyOrganizationAndAgreement(
                    supplyOrganizationAgreementRepository,
                    message.MergedService.SupplyOrganizationAgreementId,
                    existMergedService.GrossPrice,
                    existMergedService.AccountingGrossPrice,
                    message.MergedService.SupplyOrganizationAgreement.Id,
                    message.MergedService.GrossPrice,
                    message.MergedService.AccountingGrossPrice);

                message.MergedService.UserId = message.MergedService.User.Id;

                message.MergedService.SupplyOrganizationId =
                    message.MergedService.SupplyOrganization.Id;
                message.MergedService.SupplyOrganizationAgreementId =
                    message.MergedService.SupplyOrganizationAgreement.Id;

                if (message.MergedService.SupplyPaymentTask != null) {
                    if (message.MergedService.SupplyPaymentTask.IsNew()) {
                        message.MergedService.SupplyPaymentTask.UserId = message.MergedService.SupplyPaymentTask.User.Id;
                        message.MergedService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.MergedService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.MergedService;

                        message.MergedService.SupplyPaymentTask.PayToDate =
                            !message.MergedService.SupplyPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.MergedService.SupplyPaymentTask.PayToDate.Value);

                        message.MergedService.SupplyPaymentTask.NetPrice = message.MergedService.NetPrice;
                        message.MergedService.SupplyPaymentTask.GrossPrice = message.MergedService.GrossPrice;

                        message.MergedService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(message.MergedService.SupplyPaymentTask);

                        if (message.MergedService.SupplyPaymentTask.SupplyPaymentTaskDocuments.Any())
                            foreach (SupplyPaymentTaskDocument document in message.MergedService.SupplyPaymentTask.SupplyPaymentTaskDocuments) {
                                document.SupplyPaymentTaskId = message.MergedService.SupplyPaymentTaskId.Value;

                                supplyPaymentTaskDocumentRepository.Add(document);
                            }
                    } else {
                        if (message.MergedService.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.MergedService.SupplyPaymentTask.IsAvailableForPayment) {
                            if (message.MergedService.SupplyPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.MergedService.SupplyPaymentTask.Id, updatedBy.Id);

                                supplyPaymentTaskDocumentRepository.RemoveBySupplyPaymentTaskId(message.MergedService.SupplyPaymentTask.Id);

                                message.MergedService.SupplyPaymentTaskId = null;
                            } else {
                                message.MergedService.SupplyPaymentTask.PayToDate =
                                    !message.MergedService.SupplyPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.MergedService.SupplyPaymentTask.PayToDate.Value);

                                message.MergedService.SupplyPaymentTask.UpdatedById = updatedBy.Id;

                                message.MergedService.SupplyPaymentTask.NetPrice = message.MergedService.NetPrice;
                                message.MergedService.SupplyPaymentTask.GrossPrice = message.MergedService.GrossPrice;

                                message.MergedService.SupplyPaymentTask.UserId = message.MergedService.SupplyPaymentTask.User.Id;

                                if (message.MergedService.SupplyPaymentTask.SupplyPaymentTaskDocuments.Any()) {
                                    supplyPaymentTaskDocumentRepository.Remove(
                                        message
                                            .MergedService
                                            .SupplyPaymentTask
                                            .SupplyPaymentTaskDocuments
                                            .Where(x => x.Deleted.Equals(true))
                                    );

                                    message.MergedService.SupplyPaymentTask.SupplyPaymentTaskDocuments
                                        .Where(x => x.IsNew())
                                        .ForEach(document => {
                                            document.SupplyPaymentTaskId = message.MergedService.SupplyPaymentTask.Id;

                                            supplyPaymentTaskDocumentRepository.Add(document);
                                        });
                                }

                                supplyPaymentTaskRepository.Update(message.MergedService.SupplyPaymentTask);
                            }
                        }
                    }
                }

                if (message.MergedService.AccountingPaymentTask != null) {
                    if (message.MergedService.AccountingPaymentTask.IsNew()) {
                        message.MergedService.AccountingPaymentTask.UserId = message.MergedService.AccountingPaymentTask.User.Id;
                        message.MergedService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.MergedService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.MergedService;
                        message.MergedService.AccountingPaymentTask.IsAccounting = true;

                        message.MergedService.AccountingPaymentTask.PayToDate =
                            !message.MergedService.AccountingPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.MergedService.AccountingPaymentTask.PayToDate.Value);

                        message.MergedService.AccountingPaymentTask.NetPrice = message.MergedService.AccountingNetPrice;
                        message.MergedService.AccountingPaymentTask.GrossPrice = message.MergedService.AccountingGrossPrice;

                        message.MergedService.AccountingPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.MergedService.AccountingPaymentTask);

                        if (message.MergedService.AccountingPaymentTask.SupplyPaymentTaskDocuments.Any())
                            foreach (SupplyPaymentTaskDocument document in message.MergedService.AccountingPaymentTask.SupplyPaymentTaskDocuments) {
                                document.SupplyPaymentTaskId = message.MergedService.AccountingPaymentTaskId.Value;

                                supplyPaymentTaskDocumentRepository.RemoveBySupplyPaymentTaskId(message.MergedService.AccountingPaymentTask.Id);

                                supplyPaymentTaskDocumentRepository.Add(document);
                            }
                    } else {
                        if (message.MergedService.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.MergedService.AccountingPaymentTask.IsAvailableForPayment) {
                            if (message.MergedService.AccountingPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.MergedService.AccountingPaymentTask.Id, updatedBy.Id);

                                message.MergedService.AccountingPaymentTaskId = null;
                            } else {
                                message.MergedService.AccountingPaymentTask.PayToDate =
                                    !message.MergedService.AccountingPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.MergedService.AccountingPaymentTask.PayToDate.Value);

                                message.MergedService.AccountingPaymentTask.UserId = message.MergedService.AccountingPaymentTask.User.Id;

                                message.MergedService.AccountingPaymentTask.NetPrice = message.MergedService.AccountingNetPrice;
                                message.MergedService.AccountingPaymentTask.GrossPrice = message.MergedService.AccountingGrossPrice;

                                message.MergedService.AccountingPaymentTask.UpdatedById = updatedBy.Id;

                                if (message.MergedService.AccountingPaymentTask.SupplyPaymentTaskDocuments.Any()) {
                                    supplyPaymentTaskDocumentRepository.Remove(message
                                        .MergedService
                                        .AccountingPaymentTask
                                        .SupplyPaymentTaskDocuments
                                        .Where(x => x.Deleted.Equals(true)));

                                    message.MergedService.AccountingPaymentTask.SupplyPaymentTaskDocuments
                                        .Where(x => x.IsNew())
                                        .ForEach(document => {
                                            document.SupplyPaymentTaskId = message.MergedService.AccountingPaymentTask.Id;

                                            supplyPaymentTaskDocumentRepository.Add(document);
                                        });
                                }

                                supplyPaymentTaskRepository.Update(message.MergedService.AccountingPaymentTask);
                            }
                        }
                    }
                }

                if (message.MergedService.ActProvidingService != null) {
                    if (message.MergedService.ActProvidingService.IsNew()) {
                        message.MergedService.ActProvidingService.UserId = message.MergedService.User.Id;

                        message.MergedService.ActProvidingService.IsAccounting = false;

                        message.MergedService.ActProvidingService.Price =
                            message.MergedService.GrossPrice;

                        message.MergedService.ActProvidingService.FromDate = DateTime.UtcNow;

                        ActProvidingService lastAct = actProvidingServiceRepository.GetLastRecord(_defaultComment);

                        if (lastAct != null && lastAct.Created.Year.Equals(DateTime.Now.Year) && !string.IsNullOrEmpty(lastAct.Number))
                            message.MergedService.ActProvidingService.Number = string.Format("P{0:D10}", int.Parse(lastAct.Number.Substring(1)) + 1);
                        else
                            message.MergedService.ActProvidingService.Number = string.Format("P{0:D10}", 1);

                        message.MergedService.ActProvidingServiceId =
                            actProvidingServiceRepository.New(message.MergedService.ActProvidingService);

                        if (!message.MergedService.SupplyOrganizationAgreement.IsNew()) {
                            message.MergedService.SupplyOrganizationAgreement =
                                supplyOrganizationAgreementRepository.GetById(message.MergedService.SupplyOrganizationAgreement.Id);

                            message.MergedService.SupplyOrganizationAgreement.CurrentAmount =
                                Math.Round(
                                    message.MergedService.SupplyOrganizationAgreement.CurrentAmount - message.MergedService.GrossPrice, 2);

                            supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.MergedService.SupplyOrganizationAgreement);
                        }
                    } else if (message.MergedService.ActProvidingService.Deleted.Equals(true)) {
                        message.MergedService.ActProvidingServiceId = null;
                        actProvidingServiceRepository.Remove(message.MergedService.ActProvidingService.Id);

                        if (!message.MergedService.SupplyOrganizationAgreement.IsNew()) {
                            message.MergedService.SupplyOrganizationAgreement =
                                supplyOrganizationAgreementRepository.GetById(message.MergedService.SupplyOrganizationAgreement.Id);

                            message.MergedService.SupplyOrganizationAgreement.CurrentAmount =
                                Math.Round(
                                    message.MergedService.SupplyOrganizationAgreement.CurrentAmount + existMergedService.GrossPrice, 2);

                            supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.MergedService.SupplyOrganizationAgreement);
                        }
                    } else {
                        message.MergedService.ActProvidingService.Price =
                            message.MergedService.GrossPrice;
                        actProvidingServiceRepository.Update(message.MergedService.ActProvidingService);

                        if (!message.MergedService.SupplyOrganizationAgreement.IsNew()) {
                            message.MergedService.SupplyOrganizationAgreement =
                                supplyOrganizationAgreementRepository.GetById(message.MergedService.SupplyOrganizationAgreement.Id);

                            message.MergedService.SupplyOrganizationAgreement.CurrentAmount =
                                Math.Round(
                                    message.MergedService.SupplyOrganizationAgreement.CurrentAmount + existMergedService.GrossPrice - message.MergedService.GrossPrice, 2);

                            supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.MergedService.SupplyOrganizationAgreement);
                        }
                    }
                }

                if (message.MergedService.AccountingActProvidingService != null) {
                    if (message.MergedService.AccountingActProvidingService.IsNew()) {
                        message.MergedService.AccountingActProvidingService.UserId = message.MergedService.User.Id;

                        message.MergedService.AccountingActProvidingService.IsAccounting = true;

                        message.MergedService.AccountingActProvidingService.Price =
                            message.MergedService.AccountingGrossPrice;

                        message.MergedService.AccountingActProvidingService.FromDate = DateTime.UtcNow;

                        ActProvidingService lastAct = actProvidingServiceRepository.GetLastRecord(_defaultComment);

                        if (lastAct != null && lastAct.Created.Year.Equals(DateTime.Now.Year) && !string.IsNullOrEmpty(lastAct.Number))
                            message.MergedService.AccountingActProvidingService.Number = string.Format("P{0:D10}", int.Parse(lastAct.Number.Substring(1)) + 1);
                        else
                            message.MergedService.AccountingActProvidingService.Number = string.Format("P{0:D10}", 1);

                        message.MergedService.AccountingActProvidingServiceId =
                            actProvidingServiceRepository.New(message.MergedService.AccountingActProvidingService);

                        if (!message.MergedService.SupplyOrganizationAgreement.IsNew()) {
                            message.MergedService.SupplyOrganizationAgreement =
                                supplyOrganizationAgreementRepository.GetById(message.MergedService.SupplyOrganizationAgreement.Id);

                            message.MergedService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                                Math.Round(
                                    message.MergedService.SupplyOrganizationAgreement.AccountingCurrentAmount - message.MergedService.AccountingGrossPrice, 2);

                            supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.MergedService.SupplyOrganizationAgreement);
                        }
                    } else if (message.MergedService.AccountingActProvidingService.Deleted.Equals(true)) {
                        message.MergedService.AccountingActProvidingServiceId = null;
                        actProvidingServiceRepository.Remove(message.MergedService.AccountingActProvidingService.Id);

                        if (!message.MergedService.SupplyOrganizationAgreement.IsNew()) {
                            message.MergedService.SupplyOrganizationAgreement =
                                supplyOrganizationAgreementRepository.GetById(message.MergedService.SupplyOrganizationAgreement.Id);

                            message.MergedService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                                Math.Round(
                                    message.MergedService.SupplyOrganizationAgreement.AccountingCurrentAmount + existMergedService.AccountingGrossPrice, 2);

                            supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.MergedService.SupplyOrganizationAgreement);
                        }
                    } else {
                        message.MergedService.AccountingActProvidingService.Price =
                            message.MergedService.AccountingGrossPrice;
                        actProvidingServiceRepository.Update(message.MergedService.AccountingActProvidingService);

                        if (!message.MergedService.SupplyOrganizationAgreement.IsNew()) {
                            message.MergedService.SupplyOrganizationAgreement =
                                supplyOrganizationAgreementRepository.GetById(message.MergedService.SupplyOrganizationAgreement.Id);

                            message.MergedService.SupplyOrganizationAgreement.CurrentAmount =
                                Math.Round(
                                    message.MergedService.SupplyOrganizationAgreement.CurrentAmount + existMergedService.AccountingGrossPrice -
                                    message.MergedService.AccountingGrossPrice, 2);

                            supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.MergedService.SupplyOrganizationAgreement);
                        }
                    }
                }

                if (message.MergedService.SupplyInformationTask != null) {
                    if (message.MergedService.SupplyInformationTask.IsNew()) {
                        message.MergedService.SupplyInformationTask.UserId = message.MergedService.SupplyInformationTask.User.Id;
                        message.MergedService.SupplyInformationTask.UpdatedById = updatedBy.Id;

                        message.MergedService.AccountingSupplyCostsWithinCountry =
                            message.MergedService.SupplyInformationTask.GrossPrice;

                        message.MergedService.SupplyInformationTaskId =
                            supplyInformationTaskRepository.Add(message.MergedService.SupplyInformationTask);
                    } else {
                        if (message.MergedService.SupplyInformationTask.Deleted) {
                            message.MergedService.SupplyInformationTask.DeletedById = updatedBy.Id;

                            supplyInformationTaskRepository.Remove(message.MergedService.SupplyInformationTask);

                            message.MergedService.SupplyInformationTaskId = null;
                        } else {
                            message.MergedService.SupplyInformationTask.UpdatedById = updatedBy.Id;
                            message.MergedService.SupplyInformationTask.UserId = message.MergedService.SupplyInformationTask.User.Id;

                            message.MergedService.AccountingSupplyCostsWithinCountry =
                                message.MergedService.SupplyInformationTask.GrossPrice;

                            supplyInformationTaskRepository.Update(message.MergedService.SupplyInformationTask);
                        }
                    }
                }

                if (message.MergedService.ActProvidingServiceDocument != null) {
                    if (message.MergedService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.MergedService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.MergedService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.MergedService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.MergedService.ActProvidingServiceDocument);
                    } else if (message.MergedService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.MergedService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.MergedService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.MergedService.SupplyServiceAccountDocument != null) {
                    if (message.MergedService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.MergedService.SupplyServiceAccountDocument.Number = string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.MergedService.SupplyServiceAccountDocument.Number = string.Format("{0:D10}", 1);

                        message.MergedService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                            .New(message.MergedService.SupplyServiceAccountDocument);
                    } else if (message.MergedService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        supplyServiceAccountDocumentRepository.RemoveById(message.MergedService.SupplyServiceAccountDocument.Id);
                        message.MergedService.SupplyServiceAccountDocumentId = null;
                    }
                }

                if (message.MergedService.FromDate.HasValue) message.MergedService.FromDate = TimeZoneInfo.ConvertTimeToUtc(message.MergedService.FromDate.Value);

                if (message.MergedService.InvoiceDocuments.Any()) {
                    invoiceDocumentRepository.Remove(
                        message.MergedService.InvoiceDocuments
                            .Where(x => x.Deleted.Equals(true)));

                    message.MergedService.InvoiceDocuments
                        .Where(document => document.IsNew())
                        .ForEach(document => {
                            document.MergedServiceId = message.MergedService.Id;

                            invoiceDocumentRepository.Add(document);
                        });
                }

                mergedServiceRepository.Update(message.MergedService);

                if (!isWithoutResetGrossPriceValue)
                    ResetValueMergedService(
                        new ResetValueMergedServiceMessage(
                            message.MergedService.Id,
                            message.UserNetId
                        )
                    );
            }

            Sender.Tell(deliveryProductProtocolRepository.GetByNetId(message.NetId));
        } catch {
            Sender.Tell(new Exception(MergedServiceResourceNames.MANAGE_ERROR));
        }
    }

    private void ProcessRemoveMergedServiceBeforeCalculatedGrossPrice(RemoveMergedServiceBeforeCalculatedGrossPriceMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IMergedServiceRepository mergedServiceRepository =
                _supplyRepositoriesFactory.NewMergedServiceRepository(connection);
            ISupplyPaymentTaskRepository supplyPaymentTaskRepository =
                _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);
            IActProvidingServiceRepository actProvidingServiceRepository =
                _supplyRepositoriesFactory.NewActProvidingServiceRepository(connection);

            DeliveryProductProtocol protocol = mergedServiceRepository
                .GetDeliveryProductProtocolByNetId(message.NetId);

            User updatedBy = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

            if (protocol.IsCompleted && !updatedBy.UserRole.UserRoleType.Equals(UserRoleType.GBA)) {
                Sender.Tell(new Exception(MergedServiceResourceNames.MANAGE_ERROR));
                return;
            }

            MergedService service = mergedServiceRepository.GetWithoutIncludesByNetId(message.NetId);

            if (service.SupplyPaymentTaskId.HasValue)
                supplyPaymentTaskRepository.RemoveById(service.SupplyPaymentTaskId.Value, updatedBy.Id);

            if (service.AccountingPaymentTaskId.HasValue)
                supplyPaymentTaskRepository.RemoveById(service.AccountingPaymentTaskId.Value, updatedBy.Id);

            if (service.ActProvidingServiceId.HasValue)
                actProvidingServiceRepository.Remove(service.ActProvidingServiceId.Value);

            if (service.AccountingActProvidingServiceId.HasValue)
                actProvidingServiceRepository.Remove(service.AccountingActProvidingServiceId.Value);

            service.SupplyOrganizationAgreement.CurrentAmount =
                Math.Round(service.SupplyOrganizationAgreement.CurrentAmount + service.GrossPrice,
                    2);

            service.SupplyOrganizationAgreement.AccountingCurrentAmount =
                Math.Round(service.SupplyOrganizationAgreement.AccountingCurrentAmount + service.AccountingGrossPrice,
                    2);

            _supplyRepositoriesFactory
                .NewSupplyOrganizationAgreementRepository(connection)
                .UpdateCurrentAmount(service.SupplyOrganizationAgreement);

            ResetValueMergedService(
                new ResetValueMergedServiceMessage(
                    service.Id,
                    message.UserNetId
                )
            );

            _supplyRepositoriesFactory
                .NewSupplyInvoiceMergedServiceRepository(connection)
                .RemoveByMergedServiceId(service.Id);

            mergedServiceRepository.RemoveById(service.Id);

            Sender.Tell(_supplyRepositoriesFactory
                .NewDeliveryProductProtocolRepository(connection)
                .GetByNetId(protocol.NetUid));

            ResetValueMergedService(
                new ResetValueMergedServiceMessage(
                    service.Id,
                    message.UserNetId
                )
            );
        } catch {
            Sender.Tell(new Exception(MergedServiceResourceNames.ERROR_REMOVING_SERVICE));
        }
    }

    private void ProcessUpdateMergedServiceExtraCharge(UpdateMergedServiceExtraChargeMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IMergedServiceRepository mergedServiceRepository =
                _supplyRepositoriesFactory.NewMergedServiceRepository(connection);
            ISupplyInvoiceMergedServiceRepository supplyInvoiceMergedServiceRepository =
                _supplyRepositoriesFactory.NewSupplyInvoiceMergedServiceRepository(connection);

            MergedService service = mergedServiceRepository.GetWithoutIncludesByNetId(message.ServiceNetId);

            DeliveryProductProtocol protocol = mergedServiceRepository
                .GetDeliveryProductProtocolByNetId(service.NetUid);

            User updatedBy = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

            if (protocol.IsCompleted && !updatedBy.UserRole.UserRoleType.Equals(UserRoleType.GBA)) {
                Sender.Tell(new Exception(MergedServiceResourceNames.MANAGE_ERROR));
                return;
            }

            List<SupplyInvoiceMergedService> invoices = supplyInvoiceMergedServiceRepository.GetByMergedServiceId(service.Id);

            if (!message.Invoices.Any() && !message.IsAuto) {
                Sender.Tell(new Exception(MergedServiceResourceNames.SELECT_SUPPLY_INVOICE_FOR_CHANGE));
                return;
            }

            if (service.IsNew() || !invoices.Any()) {
                Sender.Tell(new Exception(MergedServiceResourceNames.ADD_INVOICE_TO_SERVICE));
                return;
            }

            if (message.IsAuto) {
                switch (message.TypeExtraCharge) {
                    case SupplyExtraChargeType.Price:
                        decimal totalPrice = invoices.Select(x => x.SupplyInvoice).Sum(p => p.TotalNetPrice);
                        invoices.ForEach(list => {
                            double percent =
                                totalPrice.Equals(0)
                                    ? 100d / invoices.Count
                                    : Convert.ToDouble(list.SupplyInvoice.TotalNetPrice * 100 / totalPrice);

                            list.Value = decimal.Round(Convert.ToDecimal(percent) * service.GrossPrice / 100, 2, MidpointRounding.AwayFromZero);
                            list.AccountingValue = decimal.Round(Convert.ToDecimal(percent) * service.AccountingGrossPrice / 100, 2, MidpointRounding.AwayFromZero);
                        });
                        break;
                    case SupplyExtraChargeType.Weight:
                        double totalWeight = invoices.Select(x => x.SupplyInvoice).Sum(p => p.TotalNetWeight);
                        invoices.ForEach(list => {
                            double percent =
                                totalWeight.Equals(0)
                                    ? 100d / invoices.Count
                                    : list.SupplyInvoice.TotalNetWeight * 100 / totalWeight;

                            list.Value = decimal.Round(Convert.ToDecimal(percent) * service.GrossPrice / 100, 2, MidpointRounding.AwayFromZero);
                            list.AccountingValue = decimal.Round(Convert.ToDecimal(percent) * service.AccountingGrossPrice / 100, 2, MidpointRounding.AwayFromZero);
                        });
                        break;
                    case SupplyExtraChargeType.Volume:
                        double totalCBM = invoices.Select(x => x.SupplyInvoice).Sum(p => p.TotalCBM);
                        invoices.ForEach(list => {
                            double percent =
                                totalCBM.Equals(0)
                                    ? 100d / invoices.Count
                                    : list.SupplyInvoice.TotalCBM * 100 / totalCBM;

                            list.Value = decimal.Round(Convert.ToDecimal(percent) * service.GrossPrice / 100, 2, MidpointRounding.AwayFromZero);
                            list.AccountingValue = decimal.Round(Convert.ToDecimal(percent) * service.AccountingGrossPrice / 100, 2, MidpointRounding.AwayFromZero);
                        });
                        break;
                }

                mergedServiceRepository.UpdateSupplyExtraChargeTypeById(service.Id, message.TypeExtraCharge);

                supplyInvoiceMergedServiceRepository.UpdateExtraValue(invoices);
            } else {
                decimal totalEnteredValue = message.Invoices.Sum(x => x.Value);

                decimal totalEnteredAccountingValue = message.Invoices.Sum(x => x.AccountingValue);

                if (!service.GrossPrice.Equals(totalEnteredValue)) {
                    Sender.Tell(new Exception(MergedServiceResourceNames.ENTERED_MANAGEMENT_PRICE_NOT_VALID));
                    return;
                }

                if (!service.AccountingGrossPrice.Equals(totalEnteredAccountingValue)) {
                    Sender.Tell(new Exception(MergedServiceResourceNames.ENTERED_ACCOUNTING_PRICE_NOT_VALID));
                    return;
                }

                supplyInvoiceMergedServiceRepository.UpdateExtraValue(message.Invoices);

                supplyInvoiceMergedServiceRepository.ResetExtraValue(message.Invoices.Select(x => x.Id), service.Id);

                supplyInvoiceMergedServiceRepository.UpdateExtraValue(message.Invoices);

                supplyInvoiceMergedServiceRepository.ResetExtraValue(message.Invoices.Select(x => x.Id), service.Id);
            }

            mergedServiceRepository.UpdateIsCalculatedValueById(service.Id, message.IsAuto);

            Sender.Tell(
                _supplyRepositoriesFactory
                    .NewDeliveryProductProtocolRepository(connection)
                    .GetByNetId(protocol.NetUid));

            ActorReferenceManager.Instance.Get(SupplyActorNames.SUPPLY_INVOICE_ACTOR).Tell(new UpdateSupplyInvoiceItemGrossPriceMessage(
                supplyInvoiceMergedServiceRepository.GetSupplyInvoiceIdByMergedServiceId(service.Id),
                updatedBy.NetUid
            ));
        } catch {
            Sender.Tell(new Exception(MergedServiceResourceNames.MANAGE_ERROR));
        }
    }

    private void ProcessAddSupplyInvoicesToMergedService(AddSupplyInvoicesToMergedServiceMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyInvoiceMergedServiceRepository supplyInvoiceMergedServiceRepository =
                _supplyRepositoriesFactory.NewSupplyInvoiceMergedServiceRepository(connection);

            DeliveryProductProtocol protocol = _supplyRepositoriesFactory
                .NewMergedServiceRepository(connection)
                .GetDeliveryProductProtocolByNetId(message.Service.NetUid);

            User updatedBy = _userRepositoriesFactory.NewUserRepository(connection).GetByNetId(message.UserNetId);

            if (protocol.IsCompleted && !updatedBy.UserRole.UserRoleType.Equals(UserRoleType.GBA)) {
                Sender.Tell(new Exception(MergedServiceResourceNames.MANAGE_ERROR));
                return;
            }

            List<SupplyInvoiceMergedService> existSupplyInvoice = supplyInvoiceMergedServiceRepository.GetByMergedServiceId(message.Service.Id);

            if (message.Service.SupplyInvoiceMergedServices.Any()) {
                IEnumerable<long> ids = message.Service.SupplyInvoiceMergedServices
                    .Where(s => !s.SupplyInvoice.IsNew())
                    .Select(s => s.SupplyInvoice.Id);

                supplyInvoiceMergedServiceRepository.UnassignAllMergedServiceIdExceptProvided(message.Service.Id, ids);

                ids.ForEach(id => {
                    SupplyInvoiceMergedService existEntity =
                        supplyInvoiceMergedServiceRepository.GetById(message.Service.Id, id);

                    if (existEntity != null) {
                        if (existEntity.Deleted.Equals(true))
                            supplyInvoiceMergedServiceRepository.UpdateAssign(message.Service.Id, id);
                    } else {
                        supplyInvoiceMergedServiceRepository.Add(
                            new SupplyInvoiceMergedService {
                                MergedServiceId = message.Service.Id,
                                SupplyInvoiceId = id
                            });
                    }
                });
            } else {
                supplyInvoiceMergedServiceRepository.RemoveByMergedServiceId(message.Service.Id);
            }

            ResetValueMergedService(
                new ResetValueMergedServiceMessage(
                    message.Service.Id,
                    message.UserNetId,
                    existSupplyInvoice.Select(x => x.SupplyInvoiceId)
                )
            );

            Sender.Tell(
                _supplyRepositoriesFactory
                    .NewDeliveryProductProtocolRepository(connection)
                    .GetByNetId(protocol.NetUid));
        } catch {
            Sender.Tell(new Exception(MergedServiceResourceNames.ERROR_ADDED_INVOICES));
        }
    }

    private void ResetValueMergedService(
        ResetValueMergedServiceMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyInvoiceMergedServiceRepository supplyInvoiceMergedServiceRepository =
            _supplyRepositoriesFactory.NewSupplyInvoiceMergedServiceRepository(connection);
        IMergedServiceRepository mergedServiceRepository =
            _supplyRepositoriesFactory.NewMergedServiceRepository(connection);
        IPackingListPackageOrderItemSupplyServiceRepository packingListPackageOrderItemSupplyServiceRepository =
            _supplyRepositoriesFactory.NewPackingListPackageOrderItemSupplyServiceRepository(connection);

        if (message.ServiceIds.Any())
            foreach (long serviceId in message.ServiceIds)
                ResetValueInvoiceMergedServiceHandler(serviceId, message.UserNetId, supplyInvoiceMergedServiceRepository, mergedServiceRepository, message.InvoiceIds,
                    packingListPackageOrderItemSupplyServiceRepository);
        else
            ResetValueInvoiceMergedServiceHandler(message.ServiceId, message.UserNetId, supplyInvoiceMergedServiceRepository, mergedServiceRepository, message.InvoiceIds,
                packingListPackageOrderItemSupplyServiceRepository);
    }

    private static void ResetValueInvoiceMergedServiceHandler(
        long serviceId,
        Guid userNetId,
        ISupplyInvoiceMergedServiceRepository supplyInvoiceMergedServiceRepository,
        IMergedServiceRepository mergedServiceRepository,
        IEnumerable<long> ids,
        IPackingListPackageOrderItemSupplyServiceRepository packingListPackageOrderItemSupplyServiceRepository) {
        List<SupplyInvoiceMergedService> invoices;

        if (ids.Any())
            invoices = supplyInvoiceMergedServiceRepository.GetBySupplyInvoiceIds(ids);
        else
            invoices = supplyInvoiceMergedServiceRepository.GetByMergedServiceId(serviceId);

        invoices.ForEach(list => {
            list.Value = 0;
            list.AccountingValue = 0;
        });

        supplyInvoiceMergedServiceRepository.UpdateExtraValue(invoices);

        mergedServiceRepository.ResetIsCalculatedValueById(serviceId);

        packingListPackageOrderItemSupplyServiceRepository.RemoveByServiceId(serviceId, TypeService.MergedService);

        ActorReferenceManager.Instance.Get(SupplyActorNames.SUPPLY_INVOICE_ACTOR).Tell(new UpdateSupplyInvoiceItemGrossPriceMessage(
            invoices.Select(x => x.SupplyInvoiceId),
            userNetId
        ));
    }


    private static void UpdateSupplyOrganizationAndAgreement(
        ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository,
        long supplyOrganizationAgreementId,
        decimal oldPrice,
        decimal accountingOldPrice,
        long newSupplyOrganizationAgreementId,
        decimal newPrice,
        decimal accountingNewPrice) {
        if (supplyOrganizationAgreementId.Equals(newSupplyOrganizationAgreementId) &&
            oldPrice.Equals(newPrice))
            return;

        SupplyOrganizationAgreement supplyOrganizationAgreement =
            supplyOrganizationAgreementRepository.GetById(supplyOrganizationAgreementId);

        supplyOrganizationAgreement.CurrentAmount =
            Math.Round(supplyOrganizationAgreement.CurrentAmount + oldPrice,
                2);

        supplyOrganizationAgreement.AccountingCurrentAmount =
            Math.Round(supplyOrganizationAgreement.AccountingCurrentAmount + accountingOldPrice,
                2);

        supplyOrganizationAgreementRepository.UpdateCurrentAmount(supplyOrganizationAgreement);

        SupplyOrganizationAgreement newSupplyOrganizationAgreement =
            supplyOrganizationAgreementRepository.GetById(newSupplyOrganizationAgreementId);

        newSupplyOrganizationAgreement.CurrentAmount =
            Math.Round(newSupplyOrganizationAgreement.CurrentAmount - newPrice,
                2);

        newSupplyOrganizationAgreement.AccountingCurrentAmount =
            Math.Round(newSupplyOrganizationAgreement.AccountingCurrentAmount - accountingNewPrice,
                2);

        supplyOrganizationAgreementRepository.UpdateCurrentAmount(newSupplyOrganizationAgreement);
    }
}