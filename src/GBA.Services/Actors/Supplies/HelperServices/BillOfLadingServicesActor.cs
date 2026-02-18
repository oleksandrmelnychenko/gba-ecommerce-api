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
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.EntityHelpers.Supplies.PackingLists;
using GBA.Domain.Messages.Supplies.HelperServices.BillOfLadings;
using GBA.Domain.Messages.Supplies.Invoices;
using GBA.Domain.Repositories.Supplies.ActProvidingServices.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.DeliveryProductProtocols.Contracts;
using GBA.Domain.Repositories.Supplies.Documents.Contracts;
using GBA.Domain.Repositories.Supplies.HelperServices.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Supplies.HelperServices;

public sealed class BillOfLadingServicesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public BillOfLadingServicesActor(
        IDbConnectionFactory connectionFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        IUserRepositoriesFactory userRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _userRepositoriesFactory = userRepositoriesFactory;

        Receive<ManageBillOfLadingServiceMessage>(ProcessManageBillOfLadingService);

        Receive<AddSupplyInvoicesToBillOfLadingServiceMessage>(ProcessAddSupplyInvoicesToBillOfLadingService);

        Receive<UpdateBillOfLadingServiceExtraChargeMessage>(ProcessUpdateBillOfLadingServiceExtraCharge);

        Receive<ResetValueBillOfLadingServiceMessage>(ResetValueBillOfLadingService);
    }

    private void ProcessManageBillOfLadingService(ManageBillOfLadingServiceMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ISupplyPaymentTaskRepository supplyPaymentTaskRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskRepository(connection);
            IUserRepository userRepositories = _userRepositoriesFactory.NewUserRepository(connection);
            IBillOfLadingDocumentRepository billOfLadingDocumentRepository = _supplyRepositoriesFactory.NewBillOfLadingDocumentRepository(connection);
            IDeliveryProductProtocolRepository deliveryProductProtocolRepository = _supplyRepositoriesFactory.NewDeliveryProductProtocolRepository(connection);
            ISupplyServiceNumberRepository supplyServiceNumberRepository = _supplyRepositoriesFactory.NewSupplyServiceNumberRepository(connection);
            ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
                _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);
            IBillOfLadingServiceRepository billOfLadingServiceRepository = _supplyRepositoriesFactory.NewBillOfLadingServiceRepository(connection);
            ISupplyPaymentTaskDocumentRepository supplyPaymentTaskDocumentRepository = _supplyRepositoriesFactory.NewSupplyPaymentTaskDocumentRepository(connection);
            ISupplyInformationTaskRepository supplyInformationTaskRepository =
                _supplyRepositoriesFactory.NewSupplyInformationTaskRepository(connection);
            IActProvidingServiceDocumentRepository actProvidingServiceDocumentRepository =
                _supplyRepositoriesFactory.NewActProvidingServiceDocumentRepository(connection);
            ISupplyServiceAccountDocumentRepository supplyServiceAccountDocumentRepository =
                _supplyRepositoriesFactory.NewSupplyServiceAccountDocumentRepository(connection);
            IActProvidingServiceRepository actProvidingServiceRepository =
                _supplyRepositoriesFactory.NewActProvidingServiceRepository(connection);

            User updateUser = userRepositories.GetByNetId(message.UserNedId);

            DeliveryProductProtocol deliverProductProtocol = deliveryProductProtocolRepository.GetByNetId(message.NetId);

            if (deliverProductProtocol != null && deliverProductProtocol.IsCompleted && !updateUser.UserRole.UserRoleType.Equals(UserRoleType.GBA)) {
                Sender.Tell(new Exception(BillOfLadingServiceResourceNames.ERROR_UPDATE_PROTOCOL));
                return;
            }

            TaskAssignedTo taskAssignedTo =
                message.BillOfLadingService.TypeBillOfLadingService.Equals(TypeBillOfLadingService.Container)
                    ? TaskAssignedTo.ContainerService
                    : TaskAssignedTo.VehicleService;

            if (message.BillOfLadingService.Id.Equals(0)) {
                message.BillOfLadingService.GrossPrice = message.BillOfLadingService.NetPrice;
                message.BillOfLadingService.AccountingGrossPrice = message.BillOfLadingService.AccountingNetPrice;
                if (message.BillOfLadingService.SupplyPaymentTask != null) {
                    message.BillOfLadingService.SupplyPaymentTask.UserId = message.BillOfLadingService.User.Id;
                    message.BillOfLadingService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.BillOfLadingService.SupplyPaymentTask.TaskAssignedTo = taskAssignedTo;

                    message.BillOfLadingService.SupplyPaymentTask.PayToDate =
                        !message.BillOfLadingService.SupplyPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.BillOfLadingService.SupplyPaymentTask.PayToDate.Value);

                    message.BillOfLadingService.GrossPrice = message.BillOfLadingService.NetPrice;
                    message.BillOfLadingService.SupplyPaymentTask.NetPrice = message.BillOfLadingService.NetPrice;
                    message.BillOfLadingService.SupplyPaymentTask.GrossPrice = message.BillOfLadingService.NetPrice;

                    message.BillOfLadingService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(message.BillOfLadingService.SupplyPaymentTask);

                    if (message.BillOfLadingService.SupplyPaymentTask.SupplyPaymentTaskDocuments.Any())
                        foreach (SupplyPaymentTaskDocument document in message.BillOfLadingService.SupplyPaymentTask.SupplyPaymentTaskDocuments) {
                            document.SupplyPaymentTaskId = message.BillOfLadingService.SupplyPaymentTaskId.Value;

                            supplyPaymentTaskDocumentRepository.Add(document);
                        }
                }

                if (message.BillOfLadingService.AccountingPaymentTask != null) {
                    message.BillOfLadingService.AccountingPaymentTask.UserId = message.BillOfLadingService.User.Id;
                    message.BillOfLadingService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                    message.BillOfLadingService.AccountingPaymentTask.TaskAssignedTo = taskAssignedTo;
                    message.BillOfLadingService.AccountingPaymentTask.IsAccounting = true;

                    message.BillOfLadingService.AccountingPaymentTask.PayToDate =
                        !message.BillOfLadingService.AccountingPaymentTask.PayToDate.HasValue
                            ? DateTime.UtcNow
                            : TimeZoneInfo.ConvertTimeToUtc(message.BillOfLadingService.AccountingPaymentTask.PayToDate.Value);

                    message.BillOfLadingService.AccountingGrossPrice = message.BillOfLadingService.AccountingNetPrice;
                    message.BillOfLadingService.AccountingPaymentTask.NetPrice = message.BillOfLadingService.AccountingNetPrice;
                    message.BillOfLadingService.AccountingPaymentTask.GrossPrice = message.BillOfLadingService.AccountingNetPrice;

                    message.BillOfLadingService.AccountingPaymentTaskId =
                        supplyPaymentTaskRepository.Add(message.BillOfLadingService.AccountingPaymentTask);

                    if (message.BillOfLadingService.AccountingPaymentTask.SupplyPaymentTaskDocuments.Any())
                        foreach (SupplyPaymentTaskDocument document in message.BillOfLadingService.AccountingPaymentTask.SupplyPaymentTaskDocuments) {
                            document.SupplyPaymentTaskId = message.BillOfLadingService.AccountingPaymentTaskId.Value;

                            supplyPaymentTaskDocumentRepository.Add(document);
                        }
                }

                if (message.BillOfLadingService.SupplyInformationTask != null) {
                    message.BillOfLadingService.SupplyInformationTask.UserId = message.BillOfLadingService.User.Id;
                    message.BillOfLadingService.SupplyInformationTask.UpdatedById = updateUser.Id;

                    message.BillOfLadingService.AccountingSupplyCostsWithinCountry =
                        message.BillOfLadingService.SupplyInformationTask.GrossPrice;

                    message.BillOfLadingService.SupplyInformationTaskId =
                        supplyInformationTaskRepository.Add(message.BillOfLadingService.SupplyInformationTask);
                }

                if (message.BillOfLadingService.ActProvidingService != null) {
                    message.BillOfLadingService.ActProvidingService.UserId = message.BillOfLadingService.User.Id;

                    message.BillOfLadingService.ActProvidingService.IsAccounting = false;

                    message.BillOfLadingService.ActProvidingService.Price =
                        message.BillOfLadingService.NetPrice;

                    message.BillOfLadingService.ActProvidingService.FromDate = DateTime.Now;

                    message.BillOfLadingService.ActProvidingServiceId =
                        actProvidingServiceRepository.New(message.BillOfLadingService.ActProvidingService);
                }

                if (message.BillOfLadingService.AccountingActProvidingService != null) {
                    message.BillOfLadingService.AccountingActProvidingService.UserId = message.BillOfLadingService.User.Id;

                    message.BillOfLadingService.AccountingActProvidingService.IsAccounting = true;

                    message.BillOfLadingService.AccountingActProvidingService.Price =
                        message.BillOfLadingService.AccountingNetPrice;

                    message.BillOfLadingService.AccountingActProvidingService.FromDate = DateTime.Now;

                    message.BillOfLadingService.AccountingActProvidingServiceId =
                        actProvidingServiceRepository.New(message.BillOfLadingService.AccountingActProvidingService);
                }

                if (message.BillOfLadingService.ActProvidingServiceDocument != null) {
                    if (message.BillOfLadingService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.BillOfLadingService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.BillOfLadingService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.BillOfLadingService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.BillOfLadingService.ActProvidingServiceDocument);
                    } else if (message.BillOfLadingService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.BillOfLadingService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.BillOfLadingService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.BillOfLadingService.SupplyServiceAccountDocument != null) {
                    if (message.BillOfLadingService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.BillOfLadingService.SupplyServiceAccountDocument.Number = string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.BillOfLadingService.SupplyServiceAccountDocument.Number = string.Format("{0:D10}", 1);

                        message.BillOfLadingService.SupplyServiceAccountDocumentId =
                            supplyServiceAccountDocumentRepository
                                .New(message.BillOfLadingService.SupplyServiceAccountDocument);
                    } else if (message.BillOfLadingService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.BillOfLadingService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.BillOfLadingService.SupplyServiceAccountDocument.Id);
                    }
                }

                if (!message.BillOfLadingService.SupplyOrganizationAgreement.IsNew()) {
                    message.BillOfLadingService.SupplyOrganizationAgreement =
                        supplyOrganizationAgreementRepository.GetById(message.BillOfLadingService.SupplyOrganizationAgreement.Id);

                    message.BillOfLadingService.SupplyOrganizationAgreement.CurrentAmount =
                        Math.Round(
                            message.BillOfLadingService.SupplyOrganizationAgreement.CurrentAmount - message.BillOfLadingService.NetPrice, 2);

                    message.BillOfLadingService.SupplyOrganizationAgreement.AccountingCurrentAmount =
                        Math.Round(
                            message.BillOfLadingService.SupplyOrganizationAgreement.AccountingCurrentAmount -
                            message.BillOfLadingService.AccountingNetPrice, 2);

                    supplyOrganizationAgreementRepository.UpdateCurrentAmount(message.BillOfLadingService.SupplyOrganizationAgreement);
                }

                SupplyServiceNumber number = supplyServiceNumberRepository.GetLastRecord();

                if (number != null && number.Created.Year.Equals(DateTime.Now.Year))
                    message.BillOfLadingService.ServiceNumber = $"P{int.Parse(number.Number.Substring(1)) + 1:D10}";
                else
                    message.BillOfLadingService.ServiceNumber = "P{1:D10}";

                supplyServiceNumberRepository.Add(message.BillOfLadingService.ServiceNumber);

                SupplyTransportationType transportationTypeProtocol =
                    deliveryProductProtocolRepository.GetTransportationTypeById(deliverProductProtocol.Id);

                switch (transportationTypeProtocol) {
                    case SupplyTransportationType.Vehicle:
                        message.BillOfLadingService.TypeBillOfLadingService = TypeBillOfLadingService.Vehicle;
                        break;
                    case SupplyTransportationType.Ship:
                        message.BillOfLadingService.TypeBillOfLadingService = TypeBillOfLadingService.Container;
                        break;
                    case SupplyTransportationType.Plane:
                        message.BillOfLadingService.TypeBillOfLadingService = TypeBillOfLadingService.Plane;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                message.BillOfLadingService.SupplyOrganizationId = message.BillOfLadingService.SupplyOrganization.Id;
                message.BillOfLadingService.UserId = message.BillOfLadingService.User.Id;
                if (message.BillOfLadingService.SupplyOrganizationAgreement != null)
                    message.BillOfLadingService.SupplyOrganizationAgreementId = message.BillOfLadingService.SupplyOrganizationAgreement.Id;

                if (message.BillOfLadingService.LoadDate.HasValue)
                    message.BillOfLadingService.LoadDate = TimeZoneInfo.ConvertTimeToUtc(message.BillOfLadingService.LoadDate.Value);

                if (message.BillOfLadingService.FromDate.HasValue)
                    message.BillOfLadingService.FromDate = TimeZoneInfo.ConvertTimeToUtc(message.BillOfLadingService.FromDate.Value);

                message.BillOfLadingService.DeliveryProductProtocolId = deliverProductProtocol.Id;

                message.BillOfLadingService.Id = billOfLadingServiceRepository.Add(message.BillOfLadingService);

                if (message.BillOfLadingService.BillOfLadingDocuments.Any())
                    message.BillOfLadingService.BillOfLadingDocuments.ForEach(document => {
                        document.BillOfLadingServiceId = message.BillOfLadingService.Id;
                        document.Number = message.BillOfLadingService.ServiceNumber;
                        document.Date = message.BillOfLadingService.FromDate ?? DateTime.Now;

                        billOfLadingDocumentRepository.Add(document);
                    });
            } else {
                BillOfLadingService billOfLadingService = billOfLadingServiceRepository.GetByIdWithoutIncludes(message.BillOfLadingService.Id);

                bool isWithoutResetGrossPriceValue = billOfLadingService.Equals(message.BillOfLadingService);

                message.BillOfLadingService.GrossPrice = message.BillOfLadingService.NetPrice;
                message.BillOfLadingService.AccountingGrossPrice = message.BillOfLadingService.AccountingNetPrice;

                UpdateSupplyOrganizationAndAgreement(
                    supplyOrganizationAgreementRepository,
                    billOfLadingService.SupplyOrganizationAgreementId,
                    billOfLadingService.NetPrice,
                    billOfLadingService.AccountingNetPrice,
                    message.BillOfLadingService.SupplyOrganizationAgreement.Id,
                    message.BillOfLadingService.NetPrice,
                    message.BillOfLadingService.AccountingNetPrice);

                message.BillOfLadingService.SupplyOrganizationId =
                    message.BillOfLadingService.SupplyOrganization.Id;
                message.BillOfLadingService.SupplyOrganizationAgreementId =
                    message.BillOfLadingService.SupplyOrganizationAgreement.Id;

                if (message.BillOfLadingService.SupplyPaymentTask != null) {
                    if (message.BillOfLadingService.SupplyPaymentTask.IsNew()) {
                        message.BillOfLadingService.SupplyPaymentTask.UserId = message.BillOfLadingService.SupplyPaymentTask.User.Id;
                        message.BillOfLadingService.SupplyPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.BillOfLadingService.SupplyPaymentTask.TaskAssignedTo = taskAssignedTo;

                        message.BillOfLadingService.SupplyPaymentTask.PayToDate =
                            !message.BillOfLadingService.SupplyPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.BillOfLadingService.SupplyPaymentTask.PayToDate.Value);

                        message.BillOfLadingService.GrossPrice = message.BillOfLadingService.NetPrice;
                        message.BillOfLadingService.SupplyPaymentTask.NetPrice = message.BillOfLadingService.NetPrice;
                        message.BillOfLadingService.SupplyPaymentTask.GrossPrice = message.BillOfLadingService.NetPrice;

                        message.BillOfLadingService.SupplyPaymentTaskId = supplyPaymentTaskRepository.Add(message.BillOfLadingService.SupplyPaymentTask);

                        if (message.BillOfLadingService.SupplyPaymentTask.SupplyPaymentTaskDocuments.Any())
                            foreach (SupplyPaymentTaskDocument document in message.BillOfLadingService.SupplyPaymentTask.SupplyPaymentTaskDocuments) {
                                document.SupplyPaymentTaskId = message.BillOfLadingService.SupplyPaymentTaskId.Value;

                                supplyPaymentTaskDocumentRepository.Add(document);
                            }
                    } else {
                        if (message.BillOfLadingService.SupplyPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.BillOfLadingService.SupplyPaymentTask.IsAvailableForPayment) {
                            if (message.BillOfLadingService.SupplyPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.BillOfLadingService.SupplyPaymentTask.Id, updateUser.Id);

                                supplyPaymentTaskDocumentRepository.RemoveBySupplyPaymentTaskId(message.BillOfLadingService.SupplyPaymentTask.Id);

                                message.BillOfLadingService.SupplyPaymentTaskId = null;
                            } else {
                                message.BillOfLadingService.SupplyPaymentTask.PayToDate =
                                    !message.BillOfLadingService.SupplyPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.BillOfLadingService.SupplyPaymentTask.PayToDate.Value);

                                message.BillOfLadingService.SupplyPaymentTask.UpdatedById = updateUser.Id;

                                message.BillOfLadingService.GrossPrice = message.BillOfLadingService.NetPrice;
                                message.BillOfLadingService.SupplyPaymentTask.NetPrice = message.BillOfLadingService.NetPrice;
                                message.BillOfLadingService.SupplyPaymentTask.GrossPrice = message.BillOfLadingService.NetPrice;

                                if (message.BillOfLadingService.SupplyPaymentTask.SupplyPaymentTaskDocuments.Any()) {
                                    supplyPaymentTaskDocumentRepository.Remove(
                                        message
                                            .BillOfLadingService
                                            .SupplyPaymentTask
                                            .SupplyPaymentTaskDocuments
                                            .Where(x => x.Deleted.Equals(true))
                                    );

                                    message.BillOfLadingService.SupplyPaymentTask.SupplyPaymentTaskDocuments
                                        .Where(x => x.IsNew())
                                        .ForEach(document => {
                                            document.SupplyPaymentTaskId = message.BillOfLadingService.SupplyPaymentTask.Id;

                                            supplyPaymentTaskDocumentRepository.Add(document);
                                        });
                                }

                                supplyPaymentTaskRepository.Update(message.BillOfLadingService.SupplyPaymentTask);
                            }
                        }
                    }
                }

                if (message.BillOfLadingService.AccountingPaymentTask != null) {
                    if (message.BillOfLadingService.AccountingPaymentTask.IsNew()) {
                        message.BillOfLadingService.AccountingPaymentTask.UserId = message.BillOfLadingService.AccountingPaymentTask.User.Id;
                        message.BillOfLadingService.AccountingPaymentTask.TaskStatus = TaskStatus.NotDone;
                        message.BillOfLadingService.AccountingPaymentTask.TaskAssignedTo = taskAssignedTo;
                        message.BillOfLadingService.AccountingPaymentTask.IsAccounting = true;

                        message.BillOfLadingService.AccountingPaymentTask.PayToDate =
                            !message.BillOfLadingService.AccountingPaymentTask.PayToDate.HasValue
                                ? DateTime.UtcNow
                                : TimeZoneInfo.ConvertTimeToUtc(message.BillOfLadingService.AccountingPaymentTask.PayToDate.Value);

                        message.BillOfLadingService.AccountingGrossPrice = message.BillOfLadingService.AccountingNetPrice;
                        message.BillOfLadingService.AccountingPaymentTask.NetPrice = message.BillOfLadingService.AccountingNetPrice;
                        message.BillOfLadingService.AccountingPaymentTask.GrossPrice = message.BillOfLadingService.AccountingNetPrice;

                        message.BillOfLadingService.AccountingPaymentTaskId =
                            supplyPaymentTaskRepository.Add(message.BillOfLadingService.AccountingPaymentTask);

                        if (message.BillOfLadingService.AccountingPaymentTask.SupplyPaymentTaskDocuments.Any())
                            foreach (SupplyPaymentTaskDocument document in message.BillOfLadingService.AccountingPaymentTask.SupplyPaymentTaskDocuments) {
                                document.SupplyPaymentTaskId = message.BillOfLadingService.AccountingPaymentTaskId.Value;

                                supplyPaymentTaskDocumentRepository.Add(document);
                            }
                    } else {
                        if (message.BillOfLadingService.AccountingPaymentTask.TaskStatus.Equals(TaskStatus.NotDone)
                            && !message.BillOfLadingService.AccountingPaymentTask.IsAvailableForPayment) {
                            if (message.BillOfLadingService.AccountingPaymentTask.Deleted) {
                                supplyPaymentTaskRepository.RemoveById(message.BillOfLadingService.AccountingPaymentTask.Id, updateUser.Id);

                                supplyPaymentTaskDocumentRepository.RemoveBySupplyPaymentTaskId(message.BillOfLadingService.AccountingPaymentTask.Id);

                                message.BillOfLadingService.AccountingPaymentTaskId = null;
                            } else {
                                message.BillOfLadingService.AccountingPaymentTask.PayToDate =
                                    !message.BillOfLadingService.AccountingPaymentTask.PayToDate.HasValue
                                        ? DateTime.UtcNow
                                        : TimeZoneInfo.ConvertTimeToUtc(message.BillOfLadingService.AccountingPaymentTask.PayToDate.Value);

                                message.BillOfLadingService.AccountingPaymentTask.UpdatedById = updateUser.Id;

                                message.BillOfLadingService.AccountingGrossPrice = message.BillOfLadingService.AccountingNetPrice;
                                message.BillOfLadingService.AccountingPaymentTask.NetPrice = message.BillOfLadingService.AccountingNetPrice;
                                message.BillOfLadingService.AccountingPaymentTask.GrossPrice = message.BillOfLadingService.AccountingNetPrice;

                                if (message.BillOfLadingService.AccountingPaymentTask.SupplyPaymentTaskDocuments.Any()) {
                                    supplyPaymentTaskDocumentRepository.Remove(message
                                        .BillOfLadingService
                                        .AccountingPaymentTask
                                        .SupplyPaymentTaskDocuments
                                        .Where(x => x.Deleted.Equals(true)));

                                    message.BillOfLadingService.AccountingPaymentTask.SupplyPaymentTaskDocuments
                                        .Where(x => x.IsNew())
                                        .ForEach(document => {
                                            document.SupplyPaymentTaskId = message.BillOfLadingService.AccountingPaymentTask.Id;

                                            supplyPaymentTaskDocumentRepository.Add(document);
                                        });
                                }

                                supplyPaymentTaskRepository.Update(message.BillOfLadingService.AccountingPaymentTask);
                            }
                        }
                    }
                }

                if (message.BillOfLadingService.ActProvidingService != null) {
                    if (message.BillOfLadingService.ActProvidingService.IsNew()) {
                        message.BillOfLadingService.ActProvidingService.UserId = message.BillOfLadingService.User.Id;

                        message.BillOfLadingService.ActProvidingService.IsAccounting = false;

                        message.BillOfLadingService.ActProvidingService.Price =
                            message.BillOfLadingService.NetPrice;

                        message.BillOfLadingService.ActProvidingService.FromDate = message.BillOfLadingService.Created;
                        message.BillOfLadingService.ActProvidingServiceId =
                            actProvidingServiceRepository.New(message.BillOfLadingService.ActProvidingService);
                    } else if (message.BillOfLadingService.ActProvidingService.Deleted.Equals(true)) {
                        message.BillOfLadingService.ActProvidingServiceId = null;
                        actProvidingServiceRepository.Remove(message.BillOfLadingService.ActProvidingService.Id);
                    } else {
                        message.BillOfLadingService.ActProvidingService.Price =
                            message.BillOfLadingService.NetPrice;
                        actProvidingServiceRepository.Update(message.BillOfLadingService.ActProvidingService);
                    }
                }

                if (message.BillOfLadingService.AccountingActProvidingService != null) {
                    if (message.BillOfLadingService.AccountingActProvidingService.IsNew()) {
                        message.BillOfLadingService.AccountingActProvidingService.UserId = message.BillOfLadingService.User.Id;

                        message.BillOfLadingService.AccountingActProvidingService.IsAccounting = true;

                        message.BillOfLadingService.AccountingActProvidingService.Price =
                            message.BillOfLadingService.AccountingNetPrice;

                        message.BillOfLadingService.AccountingActProvidingService.FromDate = message.BillOfLadingService.ActProvidingServiceDocument.IsNew()
                            ? DateTime.Now
                            : message.BillOfLadingService.ActProvidingServiceDocument.Created;

                        message.BillOfLadingService.AccountingActProvidingServiceId =
                            actProvidingServiceRepository.New(message.BillOfLadingService.AccountingActProvidingService);
                    } else if (message.BillOfLadingService.AccountingActProvidingService.Deleted.Equals(true)) {
                        message.BillOfLadingService.AccountingActProvidingServiceId = null;
                        actProvidingServiceRepository.Remove(message.BillOfLadingService.AccountingActProvidingService.Id);
                    } else {
                        message.BillOfLadingService.AccountingActProvidingService.Price =
                            message.BillOfLadingService.AccountingNetPrice;
                        actProvidingServiceRepository.Update(message.BillOfLadingService.AccountingActProvidingService);
                    }
                }

                if (message.BillOfLadingService.SupplyInformationTask != null) {
                    if (message.BillOfLadingService.SupplyInformationTask.IsNew()) {
                        message.BillOfLadingService.SupplyInformationTask.UserId = message.BillOfLadingService.SupplyInformationTask.User.Id;
                        message.BillOfLadingService.SupplyInformationTask.UpdatedById = updateUser.Id;

                        message.BillOfLadingService.AccountingSupplyCostsWithinCountry =
                            message.BillOfLadingService.SupplyInformationTask.GrossPrice;

                        message.BillOfLadingService.SupplyInformationTaskId =
                            supplyInformationTaskRepository.Add(message.BillOfLadingService.SupplyInformationTask);
                    } else {
                        if (message.BillOfLadingService.SupplyInformationTask.Deleted) {
                            message.BillOfLadingService.SupplyInformationTask.DeletedById = updateUser.Id;

                            supplyInformationTaskRepository.Remove(message.BillOfLadingService.SupplyInformationTask);

                            message.BillOfLadingService.SupplyInformationTaskId = null;
                        } else {
                            message.BillOfLadingService.SupplyInformationTask.UpdatedById = updateUser.Id;
                            message.BillOfLadingService.SupplyInformationTask.UserId = message.BillOfLadingService.SupplyInformationTask.User.Id;

                            message.BillOfLadingService.AccountingSupplyCostsWithinCountry =
                                message.BillOfLadingService.SupplyInformationTask.GrossPrice;

                            supplyInformationTaskRepository.Update(message.BillOfLadingService.SupplyInformationTask);
                        }
                    }
                }

                if (message.BillOfLadingService.ActProvidingServiceDocument != null) {
                    if (message.BillOfLadingService.ActProvidingServiceDocument.IsNew()) {
                        ActProvidingServiceDocument lastRecord =
                            actProvidingServiceDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.BillOfLadingService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.BillOfLadingService.ActProvidingServiceDocument.Number = string.Format("{0:D10}", 1);

                        message.BillOfLadingService.ActProvidingServiceDocumentId = actProvidingServiceDocumentRepository
                            .New(message.BillOfLadingService.ActProvidingServiceDocument);
                    } else if (message.BillOfLadingService.ActProvidingServiceDocument.Deleted.Equals(true)) {
                        message.BillOfLadingService.ActProvidingServiceDocumentId = null;
                        actProvidingServiceDocumentRepository.RemoveById(message.BillOfLadingService.ActProvidingServiceDocument.Id);
                    }
                }

                if (message.BillOfLadingService.SupplyServiceAccountDocument != null) {
                    if (message.BillOfLadingService.SupplyServiceAccountDocument.IsNew()) {
                        SupplyServiceAccountDocument lastRecord =
                            supplyServiceAccountDocumentRepository.GetLastRecord();

                        if (lastRecord != null && lastRecord.Created.Year.Equals(DateTime.Now.Year) &&
                            !string.IsNullOrEmpty(lastRecord.Number))
                            message.BillOfLadingService.SupplyServiceAccountDocument.Number = string.Format("{0:D10}", int.Parse(lastRecord.Number.Substring(1)) + 1);
                        else
                            message.BillOfLadingService.SupplyServiceAccountDocument.Number = string.Format("{0:D10}", 1);

                        message.BillOfLadingService.SupplyServiceAccountDocumentId =
                            supplyServiceAccountDocumentRepository
                                .New(message.BillOfLadingService.SupplyServiceAccountDocument);
                    } else if (message.BillOfLadingService.SupplyServiceAccountDocument.Deleted.Equals(true)) {
                        message.BillOfLadingService.SupplyServiceAccountDocumentId = null;
                        supplyServiceAccountDocumentRepository.RemoveById(message.BillOfLadingService.SupplyServiceAccountDocument.Id);
                    }
                }

                if (message.BillOfLadingService.LoadDate.HasValue)
                    message.BillOfLadingService.LoadDate = TimeZoneInfo.ConvertTimeToUtc(message.BillOfLadingService.LoadDate.Value);

                if (message.BillOfLadingService.FromDate.HasValue) message.BillOfLadingService.FromDate = TimeZoneInfo.ConvertTimeToUtc(message.BillOfLadingService.FromDate.Value);

                message.BillOfLadingService.UserId = message.BillOfLadingService.User.Id;

                if (message.BillOfLadingService.BillOfLadingDocuments.Any()) {
                    billOfLadingDocumentRepository.Remove(
                        message.BillOfLadingService.BillOfLadingDocuments
                            .Where(x => x.Deleted.Equals(true)));

                    message.BillOfLadingService.BillOfLadingDocuments
                        .Where(document => document.IsNew())
                        .ForEach(document => {
                            document.BillOfLadingServiceId = message.BillOfLadingService.Id;
                            document.Number = message.BillOfLadingService.ServiceNumber;
                            document.Date = message.BillOfLadingService.FromDate ?? DateTime.Now;

                            billOfLadingDocumentRepository.Add(document);
                        });
                }

                billOfLadingServiceRepository.Update(message.BillOfLadingService);

                if (!isWithoutResetGrossPriceValue)
                    ResetValueBillOfLadingService(
                        new ResetValueBillOfLadingServiceMessage(
                            message.BillOfLadingService.Id,
                            updateUser.NetUid
                        )
                    );
            }

            Sender.Tell(deliveryProductProtocolRepository.GetByNetId(message.NetId));
        } catch {
            Sender.Tell(new Exception(BillOfLadingServiceResourceNames.ERROR_UPDATE_PROTOCOL));
        }
    }

    private void ProcessAddSupplyInvoicesToBillOfLadingService(AddSupplyInvoicesToBillOfLadingServiceMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IBillOfLadingServiceRepository billOfLadingServiceRepository =
                _supplyRepositoriesFactory.NewBillOfLadingServiceRepository(connection);
            ISupplyInvoiceBillOfLadingServiceRepository supplyInvoiceBillOfLadingServiceRepository =
                _supplyRepositoriesFactory.NewSupplyInvoiceBillOfLadingServiceRepository(connection);
            IDeliveryProductProtocolRepository deliveryProductProtocolRepository =
                _supplyRepositoriesFactory
                    .NewDeliveryProductProtocolRepository(connection);

            DeliveryProductProtocol protocol = billOfLadingServiceRepository
                .GetDeliveryProductProtocolByNetId(message.Service.NetUid);

            User user = _userRepositoriesFactory
                .NewUserRepository(connection)
                .GetByNetId(message.UserNetId);

            if (protocol.IsCompleted && !user.UserRole.UserRoleType.Equals(UserRoleType.GBA)) {
                Sender.Tell(new Exception(BillOfLadingServiceResourceNames.ERROR_UPDATE_PROTOCOL));
                return;
            }

            List<SupplyInvoiceBillOfLadingService> existSupplyInvoice = supplyInvoiceBillOfLadingServiceRepository.GetByBillOfLadingServiceId(message.Service.Id);

            if (message.Service.SupplyInvoiceBillOfLadingServices.Any()) {
                IEnumerable<long> ids = message.Service.SupplyInvoiceBillOfLadingServices
                    .Where(s => !s.SupplyInvoice.IsNew())
                    .Select(s => s.SupplyInvoice.Id);

                supplyInvoiceBillOfLadingServiceRepository.UnassignAllBillOfLadingServiceIdExceptProvided(message.Service.Id, ids);
                ids.ForEach(id => {
                    SupplyInvoiceBillOfLadingService existEntity =
                        supplyInvoiceBillOfLadingServiceRepository.GetById(message.Service.Id, id);
                    if (existEntity != null) {
                        if (existEntity.Deleted.Equals(true))
                            supplyInvoiceBillOfLadingServiceRepository.UpdateAssign(message.Service.Id, id);
                    } else {
                        supplyInvoiceBillOfLadingServiceRepository.Add(
                            new SupplyInvoiceBillOfLadingService {
                                BillOfLadingServiceId = message.Service.Id,
                                SupplyInvoiceId = id
                            });
                    }
                });
            } else {
                supplyInvoiceBillOfLadingServiceRepository.RemoveByBillOfLadingId(message.Service.Id);
            }

            ResetValueBillOfLadingService(
                new ResetValueBillOfLadingServiceMessage(
                    message.Service.Id,
                    user.NetUid,
                    existSupplyInvoice.Select(x => x.SupplyInvoiceId)
                )
            );

            Sender.Tell(
                deliveryProductProtocolRepository
                    .GetByNetId(protocol.NetUid));
        } catch {
            Sender.Tell(new Exception(BillOfLadingServiceResourceNames.ERROR_ADD_INVOICES_TO_SERVICE));
        }
    }

    private void ProcessUpdateBillOfLadingServiceExtraCharge(UpdateBillOfLadingServiceExtraChargeMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IBillOfLadingServiceRepository billOfLadingServiceRepository =
                _supplyRepositoriesFactory.NewBillOfLadingServiceRepository(connection);
            ISupplyInvoiceBillOfLadingServiceRepository supplyInvoiceBillOfLadingServiceRepository =
                _supplyRepositoriesFactory.NewSupplyInvoiceBillOfLadingServiceRepository(connection);

            BillOfLadingService service = billOfLadingServiceRepository.GetWithoutIncludesByNetId(message.ServiceNetId);

            DeliveryProductProtocol protocol = billOfLadingServiceRepository
                .GetDeliveryProductProtocolByNetId(message.ServiceNetId);

            User user = _userRepositoriesFactory
                .NewUserRepository(connection)
                .GetByNetId(message.UserNetId);

            if (protocol.IsCompleted && !user.UserRole.UserRoleType.Equals(UserRoleType.GBA)) {
                Sender.Tell(new Exception(BillOfLadingServiceResourceNames.ERROR_UPDATE_PROTOCOL));
                return;
            }

            List<SupplyInvoiceBillOfLadingService> invoices = supplyInvoiceBillOfLadingServiceRepository.GetByBillOfLadingServiceId(service.Id);

            if (service.IsNew() || !invoices.Any()) {
                Sender.Tell(new DeliveryProductProtocol());
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

                billOfLadingServiceRepository.UpdateSupplyExtraChargeTypeById(service.Id, message.TypeExtraCharge);

                supplyInvoiceBillOfLadingServiceRepository.UpdateExtraValue(invoices);
            } else {
                decimal totalEnteredValue = message.Invoices.Sum(x => x.Value);

                decimal totalEnteredAccountingValue = message.Invoices.Sum(x => x.AccountingValue);

                if (!service.GrossPrice.Equals(totalEnteredValue)) {
                    Sender.Tell(new Exception(BillOfLadingServiceResourceNames.ENTERED_MANAGEMENT_PRICE_NOT_VALID));
                    return;
                }

                if (!service.AccountingGrossPrice.Equals(totalEnteredAccountingValue)) {
                    Sender.Tell(new Exception(BillOfLadingServiceResourceNames.ENTERED_ACCOUNTING_PRICE_NOT_VALID));
                    return;
                }

                supplyInvoiceBillOfLadingServiceRepository.UpdateExtraValue(message.Invoices);

                supplyInvoiceBillOfLadingServiceRepository.ResetExtraValue(message.Invoices.Select(x => x.Id), service.Id);
            }

            billOfLadingServiceRepository.UpdateIsCalculatedValueById(service.Id, message.IsAuto);

            Sender.Tell(
                _supplyRepositoriesFactory
                    .NewDeliveryProductProtocolRepository(connection)
                    .GetByNetId(protocol.NetUid));

            ActorReferenceManager.Instance.Get(SupplyActorNames.SUPPLY_INVOICE_ACTOR).Tell(new UpdateSupplyInvoiceItemGrossPriceMessage(
                supplyInvoiceBillOfLadingServiceRepository.GetSupplyInvoiceIdByBillOfLadingServiceId(service.Id),
                user.NetUid
            ));
        } catch {
            Sender.Tell(new Exception(BillOfLadingServiceResourceNames.ERROR_UPDATE_PROTOCOL));
        }
    }

    private void ResetValueBillOfLadingService(
        ResetValueBillOfLadingServiceMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyInvoiceBillOfLadingServiceRepository supplyInvoiceBillOfLadingServiceRepository =
            _supplyRepositoriesFactory.NewSupplyInvoiceBillOfLadingServiceRepository(connection);
        IBillOfLadingServiceRepository billOfLadingServiceRepository =
            _supplyRepositoriesFactory.NewBillOfLadingServiceRepository(connection);
        IPackingListPackageOrderItemSupplyServiceRepository packingListPackageOrderItemSupplyServiceRepository =
            _supplyRepositoriesFactory.NewPackingListPackageOrderItemSupplyServiceRepository(connection);

        List<SupplyInvoiceBillOfLadingService> invoices;

        if (message.InvoiceIds.Any())
            invoices = supplyInvoiceBillOfLadingServiceRepository.GetBySupplyInvoiceIds(message.InvoiceIds);
        else
            invoices = supplyInvoiceBillOfLadingServiceRepository.GetByBillOfLadingServiceId(message.ServiceId);

        invoices.ForEach(list => {
            list.Value = 0;
            list.AccountingValue = 0;
        });

        supplyInvoiceBillOfLadingServiceRepository.UpdateExtraValue(invoices);

        billOfLadingServiceRepository.ResetIsCalculatedValueById(message.ServiceId);

        packingListPackageOrderItemSupplyServiceRepository.RemoveByServiceId(message.ServiceId, TypeService.BillOfLadingService);

        ActorReferenceManager.Instance.Get(SupplyActorNames.SUPPLY_INVOICE_ACTOR).Tell(new UpdateSupplyInvoiceItemGrossPriceMessage(
            invoices.Select(x => x.SupplyInvoiceId),
            message.UserNetId
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