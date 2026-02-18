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
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Messages.Supplies.HelperServices;
using GBA.Domain.Messages.Supplies.HelperServices.VehicleDeliveries;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.Documents.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Supplies.HelperServices;

public sealed class VehicleDeliveryServicesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public VehicleDeliveryServicesActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;

        Receive<InsertOrUpdateVehicleDeliveryServiceMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(connection);
            ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);
            IInvoiceDocumentRepository invoiceDocumentRepository = _supplyRepositoriesFactory.NewInvoiceDocumentRepository(connection);
            ISupplyPaymentTaskRepository supplyPaymentTaskRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);
            IServiceDetailItemRepository serviceDetailItemRepository = _supplyRepositoriesFactory.NewServiceDetailItemRepository(connection);
            ISupplyServiceNumberRepository supplyServiceNumberRepository = _supplyRepositoriesFactory.NewSupplyServiceNumberRepository(connection);
            IServiceDetailItemKeyRepository serviceDetailItemKeyRepository = _supplyRepositoriesFactory.NewServiceDetailItemKeyRepository(connection);
            ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);
            IActProvidingServiceDocumentRepository actProvidingServiceDocumentRepository =
                _supplyRepositoriesFactory.NewActProvidingServiceDocumentRepository(connection);
            ISupplyServiceAccountDocumentRepository supplyServiceAccountDocumentRepository =
                _supplyRepositoriesFactory.NewSupplyServiceAccountDocumentRepository(connection);

            SupplyOrder supplyOrder = supplyOrderRepository.GetByNetIdIfExist(message.NetId);

            if (supplyOrder != null) {
                User headPolishLogistic = userRepository.GetHeadPolishLogistic();
                User updatedBy = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

                message.VehicleDeliveryService.NetPrice = Math.Round(
                    message.VehicleDeliveryService.GrossPrice * 100 / Convert.ToDecimal(100 + message.VehicleDeliveryService.VatPercent),
                    2
                );
                message.VehicleDeliveryService.Vat = Math.Round(
                    message.VehicleDeliveryService.GrossPrice - message.VehicleDeliveryService.NetPrice,
                    2
                );
                if (message.VehicleDeliveryService.FromDate.HasValue)
                    message.VehicleDeliveryService.FromDate = TimeZoneInfo.ConvertTimeToUtc(message.VehicleDeliveryService.FromDate.Value);
                if (message.VehicleDeliveryService.IsNew()) {
                    if (message.VehicleDeliveryService.SupplyPaymentTask != null) {
                        message.VehicleDeliveryService.SupplyPaymentTask.UserId = message.VehicleDeliveryService.SupplyPaymentTask.User.Id;
                        message.VehicleDeliveryService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.VehicleDeliveryService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.VehicleDeliveryService;

                        message.VehicleDeliveryService.SupplyPaymentTask.PayToDate =
                            !message.VehicleDeliveryService.SupplyPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.VehicleDeliveryService.SupplyPaymentTask.PayToDate.Value);

                        message.VehicleDeliveryService.SupplyPaymentTask.NetPrice = message.VehicleDeliveryService.NetPrice;
                        message.VehicleDeliveryService.SupplyPaymentTask.GrossPrice = message.VehicleDeliveryService.GrossPrice;

                        message.VehicleDeliveryService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(message.VehicleDeliveryService.SupplyPaymentTask);
                    }

                    if (message.VehicleDeliveryService.AccountingPaymentTask != null) {
                        message.VehicleDeliveryService.AccountingPaymentTask.UserId = message.VehicleDeliveryService.AccountingPaymentTask.User.Id;
                        message.VehicleDeliveryService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.VehicleDeliveryService.AccountingPaymentTask.TaskAssignedTo = TaskAssignedTo.VehicleDeliveryService;
                        message.VehicleDeliveryService.AccountingPaymentTask.IsAccounting = true;

                        message.VehicleDeliveryService.AccountingPaymentTask.PayToDate =
                            !message.VehicleDeliveryService.AccountingPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.VehicleDeliveryService.AccountingPaymentTask.PayToDate.Value);

                        message.VehicleDeliveryService.AccountingPaymentTask.NetPrice = message.VehicleDeliveryService.AccountingNetPrice;
                        message.VehicleDeliveryService.AccountingPaymentTask.GrossPrice = message.VehicleDeliveryService.AccountingGrossPrice;

                        message.VehicleDeliveryService.AccountingPaymentTaskId = supplyPaymentTaskRepository.Add(message.VehicleDeliveryService.AccountingPaymentTask);
                    }

                    if (message.VehicleDeliveryService.ActProvidingServiceDocument != null) {
                        if (message.VehicleDeliveryService.ActProvidingServiceDocument.IsNew()) {
                            ActProvidingServiceDocument lastRecord =
                                actProvidingServiceDocumentRepository.GetLastRecord();

                            if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                !string.IsNullOrEmpty(lastRecord.Number))
                                message.VehicleDeliveryService.ActProvidingServiceDocument.Number =
                                    string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                            else
                                message.VehicleDeliveryService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                            message.VehicleDeliveryService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                                .New(message.VehicleDeliveryService.ActProvidingServiceDocument);
                        } else if (message.VehicleDeliveryService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                            message.VehicleDeliveryService.ActProvidingServiceDocumentId = null;
                            actProvidingServiceDocumentRepository.RemoveById(message.VehicleDeliveryService.ActProvidingServiceDocument.Id);
                        }
                    }

                    if (message.VehicleDeliveryService.SupplyServiceAccountDocument != null) {
                        if (message.VehicleDeliveryService.SupplyServiceAccountDocument.IsNew()) {
                            SupplyServiceAccountDocument lastRecord =
                                supplyServiceAccountDocumentRepository.GetLastRecord();

                            if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                !string.IsNullOrEmpty(lastRecord.Number))
                                message.VehicleDeliveryService.SupplyServiceAccountDocument.Number =
                                    string.Format("P{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                            else
                                message.VehicleDeliveryService.SupplyServiceAccountDocument.Number = string.Format("P{0:D10}", 1);

                            message.VehicleDeliveryService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                                .New(message.VehicleDeliveryService.SupplyServiceAccountDocument);
                        } else if (message.VehicleDeliveryService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                            message.VehicleDeliveryService.SupplyServiceAccountDocumentId = null;
                            supplyServiceAccountDocumentRepository.RemoveById(message.VehicleDeliveryService.SupplyServiceAccountDocument.Id);
                        }
                    }

                    SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                    if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                        message.VehicleDeliveryService.ServiceNumber = string.Format("P{0:D10}", int.Parse(number.Number.Substring(1)) + 1);
                    else
                        message.VehicleDeliveryService.ServiceNumber = string.Format("P{0:D10}", 1);

                    supplyServiceNumberRepository.Add(message.VehicleDeliveryService.ServiceNumber);

                    message.VehicleDeliveryService.VehicleDeliveryOrganizationId = message.VehicleDeliveryService.VehicleDeliveryOrganization.Id;
                    message.VehicleDeliveryService.UserId = headPolishLogistic.Id;
                    message.VehicleDeliveryService.SupplyOrganizationAgreementId = message.VehicleDeliveryService.SupplyOrganizationAgreement.Id;
                    message.VehicleDeliveryService.Id = _supplyRepositoriesFactory.NewVehicleDeliveryServiceRepository(connection).Add(message.VehicleDeliveryService);

                    if (message.VehicleDeliveryService.SupplyOrganizationAgreement != null && !message.VehicleDeliveryService.SupplyOrganizationAgreement.IsNew()) {
                        message.VehicleDeliveryService.SupplyOrganizationAgreement =
                            supplyOrganizationAgreementRepository.GetById(message.VehicleDeliveryService.SupplyOrganizationAgreement.Id);

                        message.VehicleDeliveryService.SupplyOrganizationAgreement.CurrentAmount =
                            Math.Round(message.VehicleDeliveryService.SupplyOrganizationAgreement.CurrentAmount - message.VehicleDeliveryService.GrossPrice, 2);

                        message.VehicleDeliveryService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                            Math.Round(message.VehicleDeliveryService.SupplyOrganizationAgreement.AccountingCurrentAmount -
                                       message.VehicleDeliveryService.AccountingGrossPrice, 2);

                        supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.VehicleDeliveryService.SupplyOrganizationAgreement);
                    }

                    if (message.VehicleDeliveryService.InvoiceDocuments.Any(d => d.IsNew()))
                        invoiceDocumentRepository.Add(message.VehicleDeliveryService.InvoiceDocuments
                            .Where(d => d.IsNew())
                            .Select(d => {
                                d.VehicleDeliveryServiceId = message.VehicleDeliveryService.Id;

                                return d;
                            })
                        );
                    if (message.VehicleDeliveryService.ServiceDetailItems.Any())
                        InsertOrUpdateServiceDetailItems(
                            serviceDetailItemRepository,
                            serviceDetailItemKeyRepository,
                            message.VehicleDeliveryService.ServiceDetailItems
                                .Select(i => {
                                    i.VehicleDeliveryServiceId = message.VehicleDeliveryService.Id;

                                    return i;
                                })
                        );
                } else {
                    if (message.VehicleDeliveryService.InvoiceDocuments.Any()) {
                        invoiceDocumentRepository.RemoveAllByVehicleDeliveryServiceIdExceptProvided(
                            message.VehicleDeliveryService.Id,
                            message.VehicleDeliveryService.InvoiceDocuments.Where(d => !d.IsNew() && !d.Deleted).Select(d => d.Id)
                        );

                        if (message.VehicleDeliveryService.InvoiceDocuments.Any(d => d.IsNew()))
                            invoiceDocumentRepository.Add(message.VehicleDeliveryService.InvoiceDocuments
                                .Where(d => d.IsNew())
                                .Select(d => {
                                    d.VehicleDeliveryServiceId = message.VehicleDeliveryService.Id;

                                    return d;
                                })
                            );
                    } else {
                        invoiceDocumentRepository.RemoveAllByVehicleDeliveryServiceId(message.VehicleDeliveryService.Id);
                    }

                    if (message.VehicleDeliveryService.ServiceDetailItems.Any()) {
                        serviceDetailItemRepository.RemoveAllByVehicleDeliveryServiceIdExceptProvided(
                            message.VehicleDeliveryService.Id,
                            message.VehicleDeliveryService.ServiceDetailItems.Where(i => !i.IsNew()).Select(i => i.Id)
                        );

                        InsertOrUpdateServiceDetailItems(
                            serviceDetailItemRepository,
                            serviceDetailItemKeyRepository,
                            message.VehicleDeliveryService.ServiceDetailItems
                                .Select(i => {
                                    i.VehicleDeliveryServiceId = message.VehicleDeliveryService.Id;

                                    return i;
                                })
                        );
                    } else {
                        serviceDetailItemRepository.RemoveAllByVehicleDeliveryServiceId(message.VehicleDeliveryService.Id);
                    }

                    if (message.VehicleDeliveryService.SupplyPaymentTask != null) {
                        if (message.VehicleDeliveryService.SupplyPaymentTask.IsNew()) {
                            message.VehicleDeliveryService.SupplyPaymentTask.UserId = message.VehicleDeliveryService.SupplyPaymentTask.User.Id;
                            message.VehicleDeliveryService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                            message.VehicleDeliveryService.SupplyPaymentTask.TaskAssignedTo = TaskAssignedTo.VehicleDeliveryService;

                            message.VehicleDeliveryService.SupplyPaymentTask.PayToDate =
                                !message.VehicleDeliveryService.SupplyPaymentTask.PayToDate.HasValue
                                    ? DateTime.UtcNow
                                    : TimeZoneInfo.ConvertTimeToUtc(message.VehicleDeliveryService.SupplyPaymentTask.PayToDate.Value);

                            message.VehicleDeliveryService.SupplyPaymentTask.NetPrice = message.VehicleDeliveryService.NetPrice;
                            message.VehicleDeliveryService.SupplyPaymentTask.GrossPrice = message.VehicleDeliveryService.GrossPrice;

                            message.VehicleDeliveryService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(message.VehicleDeliveryService.SupplyPaymentTask);
                        } else {
                            if (message.VehicleDeliveryService.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                                && !message.VehicleDeliveryService.SupplyPaymentTask.IsAvailableForPayment) {
                                if (message.VehicleDeliveryService.SupplyPaymentTask.Deleted) {
                                    supplyPaymentTaskRepository.RemoveById(message.VehicleDeliveryService.SupplyPaymentTask.Id, updatedBy.Id);

                                    message.VehicleDeliveryService.SupplyPaymentTaskId = null;
                                } else {
                                    message.VehicleDeliveryService.SupplyPaymentTask.PayToDate =
                                        !message.VehicleDeliveryService.SupplyPaymentTask.PayToDate.HasValue
                                            ? DateTime.UtcNow
                                            : TimeZoneInfo.ConvertTimeToUtc(message.VehicleDeliveryService.SupplyPaymentTask.PayToDate.Value);

                                    message.VehicleDeliveryService.SupplyPaymentTask.NetPrice = message.VehicleDeliveryService.NetPrice;
                                    message.VehicleDeliveryService.SupplyPaymentTask.GrossPrice = message.VehicleDeliveryService.GrossPrice;
                                    message.VehicleDeliveryService.SupplyPaymentTask.UpdatedById = updatedBy.Id;

                                    supplyPaymentTaskRepository.Update(message.VehicleDeliveryService.SupplyPaymentTask);
                                }
                            }
                        }
                    }

                    if (message.VehicleDeliveryService.ActProvidingServiceDocument != null) {
                        if (message.VehicleDeliveryService.ActProvidingServiceDocument.IsNew()) {
                            ActProvidingServiceDocument lastRecord =
                                actProvidingServiceDocumentRepository.GetLastRecord();

                            if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                !string.IsNullOrEmpty(lastRecord.Number))
                                message.VehicleDeliveryService.ActProvidingServiceDocument.Number =
                                    string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                            else
                                message.VehicleDeliveryService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                            message.VehicleDeliveryService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                                .New(message.VehicleDeliveryService.ActProvidingServiceDocument);
                        } else if (message.VehicleDeliveryService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                            message.VehicleDeliveryService.ActProvidingServiceDocumentId = null;
                            actProvidingServiceDocumentRepository.RemoveById(message.VehicleDeliveryService.ActProvidingServiceDocument.Id);
                        }
                    }

                    if (message.VehicleDeliveryService.SupplyServiceAccountDocument != null) {
                        if (message.VehicleDeliveryService.SupplyServiceAccountDocument.IsNew()) {
                            SupplyServiceAccountDocument lastRecord =
                                supplyServiceAccountDocumentRepository.GetLastRecord();

                            if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                                !string.IsNullOrEmpty(lastRecord.Number))
                                message.VehicleDeliveryService.SupplyServiceAccountDocument.Number =
                                    string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                            else
                                message.VehicleDeliveryService.SupplyServiceAccountDocument.Number = string.Format("{0:D10}", 1);

                            message.VehicleDeliveryService.SupplyServiceAccountDocumentId = supplyServiceAccountDocumentRepository
                                .New(message.VehicleDeliveryService.SupplyServiceAccountDocument);
                        } else if (message.VehicleDeliveryService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                            message.VehicleDeliveryService.SupplyServiceAccountDocumentId = null;
                            supplyServiceAccountDocumentRepository.RemoveById(message.VehicleDeliveryService.SupplyServiceAccountDocument.Id);
                        }
                    }

                    _supplyRepositoriesFactory.NewVehicleDeliveryServiceRepository(connection).Update(message.VehicleDeliveryService);
                }

                if (!message.VehicleDeliveryService.SupplyPaymentTaskId.HasValue)
                    Sender.Tell(new Tuple<SupplyOrder, SupplyPaymentTask>(supplyOrderRepository.GetByNetId(message.NetId), null));
                else
                    Sender.Tell(new Tuple<SupplyOrder, SupplyPaymentTask>(supplyOrderRepository.GetByNetId(message.NetId),
                        supplyPaymentTaskRepository.GetById(message.VehicleDeliveryService.SupplyPaymentTaskId.Value)));
            } else {
                Sender.Tell(null);
            }
        });

        Receive<GetAllDetailItemsByServiceNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_supplyRepositoriesFactory
                .NewServiceDetailItemRepository(connection)
                .GetAllByNetIdAndType(message.NetId, SupplyServiceType.VehicleDelivery)
            );
        });
    }

    private static void InsertOrUpdateServiceDetailItems(
        IServiceDetailItemRepository serviceDetailItemRepository,
        IServiceDetailItemKeyRepository serviceDetailItemKeyRepository,
        IEnumerable<ServiceDetailItem> serviceDetailItems) {
        foreach (ServiceDetailItem item in serviceDetailItems) {
            if (item.ServiceDetailItemKey != null)
                item.ServiceDetailItemKeyId = item.ServiceDetailItemKey.IsNew() ? serviceDetailItemKeyRepository.Add(item.ServiceDetailItemKey) : item.ServiceDetailItemKey.Id;

            if (item.UnitPrice > 0 && item.Qty > 0) {
                item.NetPrice = decimal.Round(item.UnitPrice * Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero);

                item.Vat = decimal.Round(item.NetPrice * Convert.ToDecimal(item.VatPercent) / 100m, 2, MidpointRounding.AwayFromZero);

                item.GrossPrice = decimal.Round(item.NetPrice + item.Vat, 2, MidpointRounding.AwayFromZero);
            } else if (item.GrossPrice > 0 && item.Qty > 0) {
                item.Vat = item.VatPercent > 0 ? decimal.Round(item.GrossPrice * 100m / (Convert.ToDecimal(item.VatPercent) + 100m), 2, MidpointRounding.AwayFromZero) : 0m;

                item.NetPrice = decimal.Round(item.GrossPrice - item.Vat, 2, MidpointRounding.AwayFromZero);

                item.UnitPrice = decimal.Round(item.NetPrice / Convert.ToDecimal(item.Qty), 2, MidpointRounding.AwayFromZero);
            }
        }

        if (serviceDetailItems.Any(i => i.IsNew())) serviceDetailItemRepository.Add(serviceDetailItems.Where(i => i.IsNew()));
        if (serviceDetailItems.Any(i => !i.IsNew())) serviceDetailItemRepository.Update(serviceDetailItems.Where(i => !i.IsNew()));
    }
}