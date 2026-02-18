using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Clients.PackingMarkings;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.Consumables.Orders;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.Accounting;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies;

public sealed class SupplyPaymentTaskRepository : ISupplyPaymentTaskRepository {
    private readonly IDbConnection _connection;

    public SupplyPaymentTaskRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupplyPaymentTask supplyPaymentTask) {
        return _connection.Query<long>(
            "INSERT INTO SupplyPaymentTask(Comment, UserID, PayToDate, TaskAssignedTo, TaskStatus, TaskStatusUpdated, NetPrice, GrossPrice, Updated, IsAccounting) " +
            "VALUES(@Comment, @UserID, @PayToDate, @TaskAssignedTo, @TaskStatus, @TaskStatusUpdated, @NetPrice, @GrossPrice, getutcdate(), @IsAccounting); " +
            "SELECT SCOPE_IDENTITY()",
            supplyPaymentTask
        ).Single();
    }

    public SupplyPaymentTask GetById(long id) {
        return _connection.Query<SupplyPaymentTask, User, SupplyPaymentTask>(
            "SELECT * FROM SupplyPaymentTask " +
            "LEFT OUTER JOIN [User] " +
            "ON [User].ID = SupplyPaymentTask.UserID " +
            "AND [User].Deleted = 0 " +
            "WHERE SupplyPaymentTask.ID = @Id",
            (task, user) => {
                if (user != null) task.User = user;

                return task;
            },
            new { Id = id }
        ).SingleOrDefault();
    }

    public SupplyPaymentTask GetByIdWithCalculatedGrossPrice(long id) {
        return _connection.Query<SupplyPaymentTask, User, SupplyPaymentTask>(
            "SELECT [SupplyPaymentTask].[ID] " +
            ",[SupplyPaymentTask].[Created] " +
            ",[SupplyPaymentTask].[Deleted] " +
            ",[SupplyPaymentTask].[NetUID] " +
            ",[SupplyPaymentTask].[Updated] " +
            ",[SupplyPaymentTask].[Comment] " +
            ",[SupplyPaymentTask].[UserID] " +
            ",[SupplyPaymentTask].[PayToDate] " +
            ",[SupplyPaymentTask].[TaskAssignedTo] " +
            ",[SupplyPaymentTask].[TaskStatus] " +
            ",[SupplyPaymentTask].[TaskStatusUpdated] " +
            ",[SupplyPaymentTask].[IsAccounting] " +
            ",( " +
            "[SupplyPaymentTask].[GrossPrice] " +
            "- " +
            "( " +
            "SELECT ISNULL(SUM([OutcomePaymentOrderSupplyPaymentTask].Amount), 0) " +
            "FROM [OutcomePaymentOrderSupplyPaymentTask] " +
            "WHERE [OutcomePaymentOrderSupplyPaymentTask].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
            ") " +
            ") AS [GrossPrice] " +
            ",[SupplyPaymentTask].[NetPrice] " +
            ",[User].* " +
            "FROM [SupplyPaymentTask] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [SupplyPaymentTask].UserID " +
            "AND [User].Deleted = 0 " +
            "WHERE [SupplyPaymentTask].ID = @Id",
            (task, user) => {
                if (user != null) task.User = user;

                return task;
            },
            new { Id = id }
        ).SingleOrDefault();
    }

    public SupplyPaymentTask GetByNetId(Guid netId) {
        SupplyPaymentTask taskToReturn = null;

        List<JoinService> joinServices = new();

        Type[] types = {
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyOrderPaymentDeliveryProtocol),
            typeof(SupplyOrderPolandPaymentDeliveryProtocol),
            typeof(ContainerService),
            typeof(CustomService),
            typeof(PortWorkService),
            typeof(TransportationService),
            typeof(PortCustomAgencyService),
            typeof(CustomAgencyService),
            typeof(PlaneDeliveryService),
            typeof(VehicleDeliveryService),
            typeof(ConsumablesOrder),
            typeof(SupplyPaymentTaskDocument),
            typeof(OutcomePaymentOrderSupplyPaymentTask),
            typeof(OutcomePaymentOrder),
            typeof(User),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(PaymentRegister),
            typeof(Currency),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(MergedService),
            typeof(SupplyOrderUkrainePaymentDeliveryProtocol)
        };

        Func<object[], SupplyPaymentTask> mapper = objects => {
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[0];
            User user = (User)objects[1];
            SupplyOrderPaymentDeliveryProtocol supplyOrderPaymentDeliveryProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[2];
            SupplyOrderPolandPaymentDeliveryProtocol supplyOrderPolandPaymentDeliveryProtocol = (SupplyOrderPolandPaymentDeliveryProtocol)objects[3];
            ContainerService containerService = (ContainerService)objects[4];
            CustomService customService = (CustomService)objects[5];
            PortWorkService portWorkService = (PortWorkService)objects[6];
            TransportationService transportationService = (TransportationService)objects[7];
            PortCustomAgencyService portCustomAgencyService = (PortCustomAgencyService)objects[8];
            CustomAgencyService customAgencyService = (CustomAgencyService)objects[9];
            PlaneDeliveryService planeDeliveryService = (PlaneDeliveryService)objects[10];
            VehicleDeliveryService vehicleDeliveryService = (VehicleDeliveryService)objects[11];
            ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[12];
            SupplyPaymentTaskDocument document = (SupplyPaymentTaskDocument)objects[13];
            OutcomePaymentOrderSupplyPaymentTask junctionTask = (OutcomePaymentOrderSupplyPaymentTask)objects[14];
            OutcomePaymentOrder outcome = (OutcomePaymentOrder)objects[15];
            User outcomeUser = (User)objects[16];
            Organization organization = (Organization)objects[17];
            PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[18];
            PaymentRegister paymentRegister = (PaymentRegister)objects[19];
            Currency currency = (Currency)objects[20];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[21];
            PaymentMovement paymentMovement = (PaymentMovement)objects[22];
            MergedService mergedService = (MergedService)objects[23];
            SupplyOrderUkrainePaymentDeliveryProtocol protocol = (SupplyOrderUkrainePaymentDeliveryProtocol)objects[24];

            if (taskToReturn == null) {
                if (supplyOrderPaymentDeliveryProtocol != null)
                    joinServices.Add(new JoinService(supplyOrderPaymentDeliveryProtocol.Id, JoinServiceType.SupplyOrderPaymentDeliveryProtocol));

                if (supplyOrderPolandPaymentDeliveryProtocol != null)
                    joinServices.Add(new JoinService(supplyOrderPolandPaymentDeliveryProtocol.Id, JoinServiceType.SupplyOrderPolandPaymentDeliveryProtocol));

                if (containerService != null) joinServices.Add(new JoinService(containerService.Id, JoinServiceType.ContainerService));

                if (customService != null) joinServices.Add(new JoinService(customService.Id, JoinServiceType.CustomService));

                if (portWorkService != null) joinServices.Add(new JoinService(portWorkService.Id, JoinServiceType.PortWorkService));

                if (transportationService != null) joinServices.Add(new JoinService(transportationService.Id, JoinServiceType.TransportationService));

                if (portCustomAgencyService != null) joinServices.Add(new JoinService(portCustomAgencyService.Id, JoinServiceType.PortCustomAgencyService));

                if (customAgencyService != null) joinServices.Add(new JoinService(customAgencyService.Id, JoinServiceType.CustomAgencyService));

                if (planeDeliveryService != null) joinServices.Add(new JoinService(planeDeliveryService.Id, JoinServiceType.PlaneDeliveryService));

                if (vehicleDeliveryService != null) joinServices.Add(new JoinService(vehicleDeliveryService.Id, JoinServiceType.VehicleDeliveryService));

                if (consumablesOrder != null) joinServices.Add(new JoinService(consumablesOrder.Id, JoinServiceType.ConsumablesOrder));

                if (mergedService != null) joinServices.Add(new JoinService(mergedService.Id, JoinServiceType.MergedService));

                if (protocol != null) joinServices.Add(new JoinService(protocol.Id, JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol));

                if (document != null) supplyPaymentTask.SupplyPaymentTaskDocuments.Add(document);

                if (junctionTask != null) {
                    if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                    paymentCurrencyRegister.PaymentRegister = paymentRegister;
                    paymentCurrencyRegister.Currency = currency;

                    outcome.User = outcomeUser;
                    outcome.PaymentCurrencyRegister = paymentCurrencyRegister;
                    outcome.PaymentMovementOperation = paymentMovementOperation;

                    junctionTask.OutcomePaymentOrder = outcome;

                    supplyPaymentTask.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);
                }

                supplyPaymentTask.User = user;

                taskToReturn = supplyPaymentTask;
            } else {
                if (containerService != null) joinServices.Add(new JoinService(containerService.Id, JoinServiceType.ContainerService));

                if (portWorkService != null) joinServices.Add(new JoinService(portWorkService.Id, JoinServiceType.PortWorkService));

                if (document != null && !taskToReturn.SupplyPaymentTaskDocuments.Any(d => d.Id.Equals(document.Id))) taskToReturn.SupplyPaymentTaskDocuments.Add(document);

                if (junctionTask == null || taskToReturn.OutcomePaymentOrderSupplyPaymentTasks.Any(o => o.Id.Equals(junctionTask.Id))) return supplyPaymentTask;

                if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                paymentCurrencyRegister.PaymentRegister = paymentRegister;
                paymentCurrencyRegister.Currency = currency;

                outcome.User = outcomeUser;
                outcome.PaymentCurrencyRegister = paymentCurrencyRegister;
                outcome.PaymentMovementOperation = paymentMovementOperation;
                outcome.Organization = organization;

                junctionTask.OutcomePaymentOrder = outcome;

                taskToReturn.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);
            }

            return supplyPaymentTask;
        };

        string tasksSqlExpression =
            "SELECT [SupplyPaymentTask].[ID] " +
            ",[SupplyPaymentTask].[Created] " +
            ",[SupplyPaymentTask].[Deleted] " +
            ",[SupplyPaymentTask].[NetUID] " +
            ",[SupplyPaymentTask].[Updated] " +
            ",[SupplyPaymentTask].[Comment] " +
            ",[SupplyPaymentTask].[UserID] " +
            ",[SupplyPaymentTask].[PayToDate] " +
            ",[SupplyPaymentTask].[TaskAssignedTo] " +
            ",[SupplyPaymentTask].[IsAvailableForPayment] " +
            ",[SupplyPaymentTask].[TaskStatus] " +
            ",[SupplyPaymentTask].[TaskStatusUpdated] " +
            ",( " +
            "CASE " +
            "WHEN [SupplyPaymentTask].TaskStatus = 1 " +
            "THEN [SupplyPaymentTask].[GrossPrice] " +
            "ELSE " +
            "( " +
            "[SupplyPaymentTask].[GrossPrice] " +
            "- " +
            "( " +
            "SELECT ISNULL(SUM([OutcomePaymentOrderSupplyPaymentTask].Amount), 0) " +
            "FROM [OutcomePaymentOrderSupplyPaymentTask] " +
            "WHERE [OutcomePaymentOrderSupplyPaymentTask].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
            ") " +
            ") " +
            "END " +
            ") AS [GrossPrice] " +
            ",[SupplyPaymentTask].[NetPrice] " +
            ",[User].* " +
            ",[SupplyOrderPaymentDeliveryProtocol].* " +
            ",[SupplyOrderPolandPaymentDeliveryProtocol].* " +
            ",[ContainerService].* " +
            ",[CustomService].* " +
            ",[PortWorkService].* " +
            ",[TransportationService].* " +
            ",[PortCustomAgencyService].* " +
            ",[CustomAgencyService].* " +
            ",[PlaneDeliveryService].* " +
            ",[VehicleDeliveryService].* " +
            ",[ConsumablesOrder].* " +
            ",[SupplyPaymentTaskDocument].* " +
            ",[OutcomePaymentOrderSupplyPaymentTask].* " +
            ",[OutcomePaymentOrder].* " +
            ",[OutcomeUser].* " +
            ",[Organization].* " +
            ",[PaymentCurrencyRegister].* " +
            ",[PaymentRegister].* " +
            ",[Currency].* " +
            ",[PaymentMovementOperation].* " +
            ",[PaymentMovement].* " +
            ",[MergedService].* " +
            ",[SupplyOrderUkrainePaymentDeliveryProtocol].* " +
            "FROM [SupplyPaymentTask] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [SupplyOrderPaymentDeliveryProtocol] " +
            "ON [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
            "LEFT JOIN [SupplyProForm] " +
            "ON [SupplyProForm].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyProFormID " +
            "LEFT JOIN [SupplyOrderPolandPaymentDeliveryProtocol] " +
            "ON [SupplyOrderPolandPaymentDeliveryProtocol].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON ( " +
            "( " +
            "[SupplyOrder].ID IS NOT NULL " +
            "AND " +
            "[SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            ") " +
            "OR " +
            "( " +
            "[SupplyProForm].ID IS NOT NULL " +
            "AND " +
            "[SupplyProForm].ID = [SupplyOrder].SupplyProFormID " +
            ") " +
            "OR " +
            "( " +
            "[SupplyOrderPolandPaymentDeliveryProtocol].ID IS NOT NULL " +
            "AND " +
            "[SupplyOrder].ID = [SupplyOrderPolandPaymentDeliveryProtocol].SupplyOrderID " +
            ") " +
            ") " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "AND [ClientAgreement].Deleted = 0 " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] AS [AgreementOrganization] " +
            "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [ContainerService] " +
            "ON [ContainerService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
            "LEFT JOIN [SupplyOrganization] AS [ContainerOrganization] " +
            "ON [ContainerService].ContainerOrganizationID = [ContainerOrganization].ID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [ContainerOrganizationAgreement] " +
            "ON [ContainerOrganizationAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [ContainerOrganizationOrganization] " +
            "ON [ContainerOrganizationOrganization].ID = [ContainerOrganizationAgreement].OrganizationID " +
            "LEFT JOIN [CustomService] " +
            "ON [CustomService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
            "LEFT JOIN [SupplyOrganization] AS [CustomOrganization] " +
            "ON [CustomService].CustomOrganizationID = [CustomOrganization].ID " +
            "OR [CustomService].ExciseDutyOrganizationID = [CustomOrganization].ID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [CustomOrganizationAgreement] " +
            "ON [CustomOrganizationAgreement].ID = [CustomService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].OrganizationView AS [CustomOrganizationOrganization] " +
            "ON [CustomOrganizationOrganization].ID = [CustomOrganizationAgreement].OrganizationID " +
            "LEFT JOIN [PortWorkService] " +
            "ON [PortWorkService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
            "LEFT JOIN [SupplyOrganization] AS [PortWorkOrganization] " +
            "ON [PortWorkService].PortWorkOrganizationID = [PortWorkOrganization].ID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [PortWorkOrganizationAgreement] " +
            "ON [PortWorkOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [PortWorkOrganizationOrganization] " +
            "ON [PortWorkOrganizationOrganization].ID = [PortWorkOrganizationAgreement].OrganizationID " +
            "LEFT JOIN [TransportationService] " +
            "ON [TransportationService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
            "LEFT JOIN [SupplyOrganization] AS [TransportationOrganization] " +
            "ON [TransportationService].TransportationOrganizationID = [TransportationOrganization].ID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [TransportationOrganizationAgreement] " +
            "ON [TransportationOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [TransportationOrganizationOrganization] " +
            "ON [TransportationOrganizationOrganization].ID = [TransportationOrganizationAgreement].OrganizationID " +
            "LEFT JOIN [PortCustomAgencyService] " +
            "ON [PortCustomAgencyService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
            "LEFT JOIN [SupplyOrganization] AS [PortCustomAgencyOrganization] " +
            "ON [PortCustomAgencyService].PortCustomAgencyOrganizationID = [PortCustomAgencyOrganization].ID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [PortCustomAgencyOrganizationAgreement] " +
            "ON [PortCustomAgencyOrganizationAgreement].ID = [PortCustomAgencyService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [PortCustomAgencyOrganizationOrganization] " +
            "ON [PortCustomAgencyOrganizationOrganization].ID = [PortCustomAgencyOrganizationAgreement].OrganizationID " +
            "LEFT JOIN [CustomAgencyService] " +
            "ON [CustomAgencyService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
            "LEFT JOIN [SupplyOrganization] AS [CustomAgencyOrganization] " +
            "ON [CustomAgencyService].CustomAgencyOrganizationID = [CustomAgencyOrganization].ID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [CustomAgencyOrganizationAgreement] " +
            "ON [CustomAgencyOrganizationAgreement].ID = [CustomAgencyService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [CustomAgencyOrganizationOrganization] " +
            "ON [CustomAgencyOrganizationOrganization].ID = [CustomAgencyOrganizationAgreement].OrganizationID " +
            "LEFT JOIN [PlaneDeliveryService] " +
            "ON [PlaneDeliveryService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
            "LEFT JOIN [SupplyOrganization] AS [PlaneDeliveryOrganization] " +
            "ON [PlaneDeliveryService].PlaneDeliveryOrganizationID = [PlaneDeliveryOrganization].ID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [PlaneDeliveryOrganizationAgreement] " +
            "ON [PlaneDeliveryOrganizationAgreement].ID = [PlaneDeliveryService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [PlaneDeliveryOrganizationOrganization] " +
            "ON [PlaneDeliveryOrganizationOrganization].ID = [PlaneDeliveryOrganizationAgreement].OrganizationID " +
            "LEFT JOIN [VehicleDeliveryService] " +
            "ON [VehicleDeliveryService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
            "LEFT JOIN [SupplyOrganization] AS [VehicleDeliveryOrganization] " +
            "ON [VehicleDeliveryService].VehicleDeliveryOrganizationID = [VehicleDeliveryOrganization].ID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [VehicleDeliveryOrganizationAgreement] " +
            "ON [VehicleDeliveryOrganizationAgreement].ID = [VehicleDeliveryService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [VehicleDeliveryOrganizationOrganization] " +
            "ON [VehicleDeliveryOrganizationOrganization].ID = [VehicleDeliveryOrganizationAgreement].OrganizationID " +
            "LEFT JOIN [ConsumablesOrder] " +
            "ON [ConsumablesOrder].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
            "LEFT JOIN [ConsumablesOrderItem] " +
            "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
            "ON [ConsumablesOrderItem].ConsumableProductOrganizationID = [ConsumableProductOrganization].ID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [ConsumableProductOrganizationAgreement] " +
            "ON [ConsumableProductOrganizationAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [ConsumableProductOrganizationOrganization] " +
            "ON [ConsumableProductOrganizationOrganization].ID = [ConsumableProductOrganizationAgreement].OrganizationID " +
            "LEFT JOIN [SupplyPaymentTaskDocument] " +
            "ON [SupplyPaymentTaskDocument].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
            "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
            "ON [OutcomePaymentOrderSupplyPaymentTask].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [OutcomePaymentOrderSupplyPaymentTask].OutcomePaymentOrderID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [OutcomeUser] " +
            "ON [OutcomeUser].ID = [OutcomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
            "LEFT JOIN ( " +
            "SELECT [PaymentMovement].ID " +
            ", [PaymentMovement].[Created] " +
            ", [PaymentMovement].[Deleted] " +
            ", [PaymentMovement].[NetUID] " +
            ", (CASE WHEN [PaymentMovementTranslation].[Name] IS NOT NULL THEN [PaymentMovementTranslation].[Name] ELSE [PaymentMovement].[OperationName] END) AS [OperationName] " +
            ", [PaymentMovement].[Updated] " +
            "FROM [PaymentMovement] " +
            "LEFT JOIN [PaymentMovementTranslation] " +
            "ON [PaymentMovementTranslation].PaymentMovementID = [PaymentMovement].ID " +
            "AND [PaymentMovementTranslation].CultureCode = @Culture " +
            ") AS [PaymentMovement] " +
            "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [PaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [MergedService] " +
            "ON [MergedService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
            "LEFT JOIN [SupplyOrderUkrainePaymentDeliveryProtocol] " +
            "ON [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
            "WHERE [SupplyPaymentTask].NetUID = @NetId ";

        _connection.Query(
            tasksSqlExpression,
            types,
            mapper,
            new {
                NetId = netId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        if (!joinServices.Any()) return taskToReturn;

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.SupplyOrderPaymentDeliveryProtocol))) {
            Type[] includesTypes = {
                typeof(SupplyOrderPaymentDeliveryProtocol),
                typeof(SupplyOrderPaymentDeliveryProtocolKey),
                typeof(SupplyPaymentTask),
                typeof(User),
                typeof(SupplyInvoice),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization),
                typeof(InvoiceDocument),
                typeof(SupplyInformationDeliveryProtocol),
                typeof(SupplyInformationDeliveryProtocolKey),
                typeof(SupplyProForm),
                typeof(ProFormDocument),
                typeof(SupplyInformationDeliveryProtocol),
                typeof(SupplyInformationDeliveryProtocolKey),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization),
                typeof(SupplyOrderNumber),
                typeof(SupplyOrderNumber)
            };

            Func<object[], SupplyOrderPaymentDeliveryProtocol> includesMapper = objects => {
                SupplyOrderPaymentDeliveryProtocol supplyOrderPaymentDeliveryProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[0];
                SupplyOrderPaymentDeliveryProtocolKey supplyOrderPaymentDeliveryProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[1];
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
                User user = (User)objects[3];
                SupplyInvoice supplyInvoice = (SupplyInvoice)objects[4];
                SupplyOrder invoiceSupplyOrder = (SupplyOrder)objects[5];
                Client invoiceClient = (Client)objects[6];
                Region invoiceClientRegion = (Region)objects[7];
                RegionCode invoiceClientRegionCode = (RegionCode)objects[8];
                Country invoiceClientCountry = (Country)objects[9];
                ClientBankDetails invoiceClientBankDetails = (ClientBankDetails)objects[10];
                ClientBankDetailAccountNumber invoiceClientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[11];
                Currency invoiceClientBankDetailAccountNumberCurrency = (Currency)objects[12];
                ClientBankDetailIbanNo invoiceClientClientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[13];
                Currency invoiceClientClientBankDetailIbanNoCurrency = (Currency)objects[14];
                TermsOfDelivery invoiceClientTermsOfDelivery = (TermsOfDelivery)objects[15];
                PackingMarking invoiceClientPackingMarking = (PackingMarking)objects[16];
                PackingMarkingPayment invoiceClientPackingMarkingPayment = (PackingMarkingPayment)objects[17];
                ClientAgreement invoiceClientClientAgreement = (ClientAgreement)objects[18];
                Agreement invoiceClientAgreement = (Agreement)objects[19];
                ProviderPricing invoiceClientProviderPricing = (ProviderPricing)objects[20];
                Currency invoiceClientProviderPricingCurrency = (Currency)objects[21];
                Pricing invoiceClientPricing = (Pricing)objects[22];
                PriceType invoiceClientPricingPriceType = (PriceType)objects[23];
                Organization invoiceClientAgreementOrganization = (Organization)objects[24];
                Currency invoiceClientAgreementCurrency = (Currency)objects[25];
                Organization invoiceOrganization = (Organization)objects[26];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[27];
                SupplyInformationDeliveryProtocol invoiceInformationProtocol = (SupplyInformationDeliveryProtocol)objects[28];
                SupplyInformationDeliveryProtocolKey invoiceInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[29];
                SupplyProForm supplyProForm = (SupplyProForm)objects[30];
                ProFormDocument proFormDocument = (ProFormDocument)objects[31];
                SupplyInformationDeliveryProtocol proFormInformationProtocol = (SupplyInformationDeliveryProtocol)objects[32];
                SupplyInformationDeliveryProtocolKey proFormInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[33];
                SupplyOrder proFormSupplyOrder = (SupplyOrder)objects[34];
                Client proFormClient = (Client)objects[35];
                Region proFormClientRegion = (Region)objects[36];
                RegionCode proFormClientRegionCode = (RegionCode)objects[37];
                Country proFormClientCountry = (Country)objects[38];
                ClientBankDetails proFormClientBankDetails = (ClientBankDetails)objects[39];
                ClientBankDetailAccountNumber proFormClientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[40];
                Currency proFormClientBankDetailAccountNumberCurrency = (Currency)objects[41];
                ClientBankDetailIbanNo proFormClientClientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[42];
                Currency proFormClientClientBankDetailIbanNoCurrency = (Currency)objects[43];
                TermsOfDelivery proFormClientTermsOfDelivery = (TermsOfDelivery)objects[44];
                PackingMarking proFormClientPackingMarking = (PackingMarking)objects[45];
                PackingMarkingPayment proFormClientPackingMarkingPayment = (PackingMarkingPayment)objects[46];
                ClientAgreement proFormClientClientAgreement = (ClientAgreement)objects[47];
                Agreement proFormClientAgreement = (Agreement)objects[48];
                ProviderPricing proFormClientProviderPricing = (ProviderPricing)objects[49];
                Currency proFormClientProviderPricingCurrency = (Currency)objects[50];
                Pricing proFormClientPricing = (Pricing)objects[51];
                PriceType proFormClientPricingPriceType = (PriceType)objects[52];
                Organization proFormClientAgreementOrganization = (Organization)objects[53];
                Currency proFormClientAgreementCurrency = (Currency)objects[54];
                Organization proFormOrganization = (Organization)objects[55];
                SupplyOrderNumber invoiceSupplyOrderNumber = (SupplyOrderNumber)objects[56];
                SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[57];

                if (taskToReturn.PaymentDeliveryProtocols.Any()) {
                    SupplyOrderPaymentDeliveryProtocol protocolFromList = taskToReturn.PaymentDeliveryProtocols.First();

                    if (supplyInvoice != null) {
                        if (invoiceDocument != null && !protocolFromList.SupplyInvoice.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                            protocolFromList.SupplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                        if (invoiceInformationProtocol != null &&
                            !protocolFromList.SupplyInvoice.InformationDeliveryProtocols.Any(p => p.Id.Equals(invoiceInformationProtocol.Id))) {
                            invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                            protocolFromList.SupplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                        }
                    }

                    if (supplyProForm == null) return supplyOrderPaymentDeliveryProtocol;

                    if (proFormDocument != null && !protocolFromList.SupplyProForm.ProFormDocuments.Any(d => d.Id.Equals(proFormDocument.Id)))
                        protocolFromList.SupplyProForm.ProFormDocuments.Add(proFormDocument);

                    if (proFormInformationProtocol != null &&
                        !protocolFromList.SupplyProForm.InformationDeliveryProtocols.Any(p => p.Id.Equals(proFormInformationProtocol.Id))) {
                        proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;

                        protocolFromList.SupplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                    }

                    if (proFormSupplyOrder == null || protocolFromList.SupplyProForm.SupplyOrders.Any(o => o.Id.Equals(proFormSupplyOrder.Id)))
                        return supplyOrderPaymentDeliveryProtocol;

                    proFormSupplyOrder.Client = proFormClient;
                    proFormSupplyOrder.SupplyOrderNumber = supplyOrderNumber;
                    proFormSupplyOrder.Organization = proFormOrganization;

                    protocolFromList.SupplyProForm.SupplyOrders.Add(proFormSupplyOrder);
                } else {
                    if (supplyInvoice != null) {
                        if (invoiceDocument != null) supplyInvoice.InvoiceDocuments.Add(invoiceDocument);

                        if (invoiceInformationProtocol != null) {
                            invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                            supplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                        }

                        if (invoiceClient != null) {
                            if (invoiceClientBankDetails != null) {
                                if (invoiceClientBankDetailAccountNumber != null) {
                                    invoiceClientBankDetailAccountNumber.Currency = invoiceClientBankDetailAccountNumberCurrency;

                                    invoiceClientBankDetails.AccountNumber = invoiceClientBankDetailAccountNumber;
                                }

                                if (invoiceClientClientBankDetailIbanNo != null) {
                                    invoiceClientClientBankDetailIbanNo.Currency = invoiceClientClientBankDetailIbanNoCurrency;

                                    invoiceClientBankDetails.ClientBankDetailIbanNo = invoiceClientClientBankDetailIbanNo;
                                }
                            }

                            if (invoiceClientClientAgreement != null) {
                                if (invoiceClientProviderPricing != null) {
                                    if (invoiceClientPricing != null) invoiceClientPricing.PriceType = invoiceClientPricingPriceType;

                                    invoiceClientProviderPricing.Currency = invoiceClientProviderPricingCurrency;
                                    invoiceClientProviderPricing.Pricing = invoiceClientPricing;
                                }

                                invoiceClientAgreement.Organization = invoiceClientAgreementOrganization;
                                invoiceClientAgreement.Currency = invoiceClientAgreementCurrency;
                                invoiceClientAgreement.ProviderPricing = invoiceClientProviderPricing;

                                invoiceClientClientAgreement.Agreement = invoiceClientAgreement;

                                invoiceClient.ClientAgreements.Add(invoiceClientClientAgreement);
                            }

                            invoiceClient.Region = invoiceClientRegion;
                            invoiceClient.RegionCode = invoiceClientRegionCode;
                            invoiceClient.Country = invoiceClientCountry;
                            invoiceClient.ClientBankDetails = invoiceClientBankDetails;
                            invoiceClient.TermsOfDelivery = invoiceClientTermsOfDelivery;
                            invoiceClient.PackingMarking = invoiceClientPackingMarking;
                            invoiceClient.PackingMarkingPayment = invoiceClientPackingMarkingPayment;
                        }

                        invoiceSupplyOrder.Client = invoiceClient;
                        invoiceSupplyOrder.SupplyOrderNumber = invoiceSupplyOrderNumber;
                        invoiceSupplyOrder.Organization = invoiceOrganization;

                        supplyInvoice.SupplyOrder = invoiceSupplyOrder;
                    }

                    if (supplyProForm != null) {
                        if (proFormDocument != null) supplyProForm.ProFormDocuments.Add(proFormDocument);

                        if (proFormInformationProtocol != null) {
                            proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;

                            supplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                        }

                        if (proFormSupplyOrder != null) {
                            if (proFormClient != null) {
                                if (proFormClientBankDetails != null) {
                                    if (proFormClientBankDetailAccountNumber != null) {
                                        proFormClientBankDetailAccountNumber.Currency = proFormClientBankDetailAccountNumberCurrency;

                                        proFormClientBankDetails.AccountNumber = proFormClientBankDetailAccountNumber;
                                    }

                                    if (proFormClientClientBankDetailIbanNo != null) {
                                        proFormClientClientBankDetailIbanNo.Currency = proFormClientClientBankDetailIbanNoCurrency;

                                        proFormClientBankDetails.ClientBankDetailIbanNo = proFormClientClientBankDetailIbanNo;
                                    }
                                }

                                if (proFormClientClientAgreement != null) {
                                    if (proFormClientProviderPricing != null) {
                                        if (proFormClientPricing != null) proFormClientPricing.PriceType = proFormClientPricingPriceType;

                                        proFormClientProviderPricing.Currency = proFormClientProviderPricingCurrency;
                                        proFormClientProviderPricing.Pricing = proFormClientPricing;
                                    }

                                    proFormClientAgreement.Organization = proFormClientAgreementOrganization;
                                    proFormClientAgreement.Currency = proFormClientAgreementCurrency;
                                    proFormClientAgreement.ProviderPricing = proFormClientProviderPricing;

                                    proFormClientClientAgreement.Agreement = proFormClientAgreement;

                                    proFormClient.ClientAgreements.Add(proFormClientClientAgreement);
                                }

                                proFormClient.Region = proFormClientRegion;
                                proFormClient.RegionCode = proFormClientRegionCode;
                                proFormClient.Country = proFormClientCountry;
                                proFormClient.ClientBankDetails = proFormClientBankDetails;
                                proFormClient.TermsOfDelivery = proFormClientTermsOfDelivery;
                                proFormClient.PackingMarking = proFormClientPackingMarking;
                                proFormClient.PackingMarkingPayment = proFormClientPackingMarkingPayment;
                            }

                            proFormSupplyOrder.Client = proFormClient;
                            proFormSupplyOrder.SupplyOrderNumber = supplyOrderNumber;
                            proFormSupplyOrder.Organization = proFormOrganization;

                            supplyProForm.SupplyOrders.Add(proFormSupplyOrder);
                        }
                    }

                    supplyOrderPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = supplyOrderPaymentDeliveryProtocolKey;
                    supplyOrderPaymentDeliveryProtocol.User = user;
                    supplyOrderPaymentDeliveryProtocol.SupplyInvoice = supplyInvoice;
                    supplyOrderPaymentDeliveryProtocol.SupplyProForm = supplyProForm;

                    taskToReturn.PaymentDeliveryProtocols.Add(supplyOrderPaymentDeliveryProtocol);
                }

                return supplyOrderPaymentDeliveryProtocol;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [SupplyOrderPaymentDeliveryProtocol] " +
                "LEFT JOIN [SupplyOrderPaymentDeliveryProtocolKey] " +
                "ON [SupplyOrderPaymentDeliveryProtocolKey].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
                "LEFT JOIN [User] AS [SupplyOrderPaymentDeliveryProtocolUser] " +
                "ON [SupplyOrderPaymentDeliveryProtocolUser].ID = [SupplyOrderPaymentDeliveryProtocol].UserID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] AS [InvoiceSupplyOrder] " +
                "ON [InvoiceSupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [Client] AS [InvoiceSupplyOrderClient] " +
                "ON [InvoiceSupplyOrder].ClientID = [InvoiceSupplyOrderClient].ID " +
                "LEFT JOIN [Region] AS [InvoiceSupplyOrderClientRegion] " +
                "ON [InvoiceSupplyOrderClientRegion].ID = [InvoiceSupplyOrderClient].RegionID " +
                "LEFT JOIN [RegionCode] AS [InvoiceSupplyOrderClientRegionCode] " +
                "ON [InvoiceSupplyOrderClientRegionCode].ID = [InvoiceSupplyOrderClient].RegionCodeID " +
                "LEFT JOIN [Country] AS [InvoiceSupplyOrderClientCountry] " +
                "ON [InvoiceSupplyOrderClientCountry].ID = [InvoiceSupplyOrderClient].CountryID " +
                "LEFT JOIN [ClientBankDetails] AS [InvoiceSupplyOrderClientBankDetails] " +
                "ON [InvoiceSupplyOrderClientBankDetails].ID = [InvoiceSupplyOrderClient].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] AS [InvoiceSupplyOrderClientBankDetailsAccountNumber] " +
                "ON [InvoiceSupplyOrderClientBankDetailsAccountNumber].ID = [InvoiceSupplyOrderClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency] " +
                "ON [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency].ID = [InvoiceSupplyOrderClientBankDetailsAccountNumber].CurrencyID " +
                "AND [InvoiceSupplyOrderClientBankDetailsAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] AS [InvoiceSupplyOrderClientBankDetailsIbanNo] " +
                "ON [InvoiceSupplyOrderClientBankDetailsIbanNo].ID = [InvoiceSupplyOrderClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency] " +
                "ON [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency].ID = [InvoiceSupplyOrderClientBankDetailsIbanNo].CurrencyID " +
                "AND [InvoiceSupplyOrderClientBankDetailsIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] AS [InvoiceSupplyOrderClientTermsOfDelivery] " +
                "ON [InvoiceSupplyOrderClientTermsOfDelivery].ID = [InvoiceSupplyOrderClient].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] AS [InvoiceSupplyOrderClientPackingMarking] " +
                "ON [InvoiceSupplyOrderClientPackingMarking].ID = [InvoiceSupplyOrderClient].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] AS [InvoiceSupplyOrderClientPackingMarkingPayment] " +
                "ON [InvoiceSupplyOrderClientPackingMarkingPayment].ID = [InvoiceSupplyOrderClient].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] AS [InvoiceSupplyOrderClientClientAgreement] " +
                "ON [InvoiceSupplyOrderClientClientAgreement].ClientID = [InvoiceSupplyOrderClient].ID " +
                "AND [InvoiceSupplyOrderClientClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] AS [InvoiceSupplyOrderClientAgreement] " +
                "ON [InvoiceSupplyOrderClientAgreement].ID = [InvoiceSupplyOrderClientClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] AS [InvoiceSupplyOrderClientAgreementProviderPricing] " +
                "ON [InvoiceSupplyOrderClientAgreementProviderPricing].ID = [InvoiceSupplyOrderClientAgreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [InvoiceSupplyOrderClientAgreementProviderPricingCurrency].ID = [InvoiceSupplyOrderClientAgreementProviderPricing].CurrencyID " +
                "AND [InvoiceSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] AS [InvoiceSupplyOrderClientAgreementPricing] " +
                "ON [InvoiceSupplyOrderClientAgreementProviderPricing].BasePricingID = [InvoiceSupplyOrderClientAgreementPricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [InvoiceSupplyOrderClientAgreementPricingPriceType] " +
                "ON [InvoiceSupplyOrderClientAgreementPricing].PriceTypeID = [InvoiceSupplyOrderClientAgreementPricingPriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [InvoiceSupplyOrderClientAgreementOrganization] " +
                "ON [InvoiceSupplyOrderClientAgreementOrganization].ID = [InvoiceSupplyOrderClientAgreement].OrganizationID " +
                "AND [InvoiceSupplyOrderClientAgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [InvoiceSupplyOrderClientAgreementCurrency] " +
                "ON [InvoiceSupplyOrderClientAgreementCurrency].ID = [InvoiceSupplyOrderClientAgreement].CurrencyID " +
                "AND [InvoiceSupplyOrderClientAgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [InvoiceSupplyOrderOrganization] " +
                "ON [InvoiceSupplyOrderOrganization].ID = [InvoiceSupplyOrder].OrganizationID " +
                "AND [InvoiceSupplyOrderOrganization].CultureCode = @Culture " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].SupplyInvoiceID = [SupplyInvoice].ID " +
                "AND [InvoiceDocument].Deleted = 0 " +
                "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [InvoiceSupplyInformationDeliveryProtocol] " +
                "ON [InvoiceSupplyInformationDeliveryProtocol].SupplyInvoiceID = [SupplyInvoice].ID " +
                "AND [InvoiceSupplyInformationDeliveryProtocol].Deleted = 0 " +
                "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [InvoiceSupplyInformationDeliveryProtocolKey] " +
                "ON [InvoiceSupplyInformationDeliveryProtocolKey].ID = [InvoiceSupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
                "AND [InvoiceSupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
                "LEFT JOIN [SupplyProForm] " +
                "ON [SupplyProForm].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyProFormID " +
                "LEFT JOIN [ProFormDocument] " +
                "ON [ProFormDocument].SupplyProFormID = [SupplyProForm].ID " +
                "AND [ProFormDocument].Deleted = 0 " +
                "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [ProFormSupplyInformationDeliveryProtocol] " +
                "ON [ProFormSupplyInformationDeliveryProtocol].SupplyProFormID = [SupplyProForm].ID " +
                "AND [ProFormSupplyInformationDeliveryProtocol].Deleted = 0 " +
                "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [ProFormSupplyInformationDeliveryProtocolKey] " +
                "ON [ProFormSupplyInformationDeliveryProtocolKey].ID = [ProFormSupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
                "AND [ProFormSupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrder] AS [ProFormSupplyOrder] " +
                "ON [ProFormSupplyOrder].SupplyProFormID = [SupplyProForm].ID " +
                "LEFT JOIN [Client] AS [ProFormSupplyOrderClient] " +
                "ON [ProFormSupplyOrder].ClientID = [ProFormSupplyOrderClient].ID " +
                "LEFT JOIN [Region] AS [ProFormSupplyOrderClientRegion] " +
                "ON [ProFormSupplyOrderClientRegion].ID = [ProFormSupplyOrderClient].RegionID " +
                "LEFT JOIN [RegionCode] AS [ProFormSupplyOrderClientRegionCode] " +
                "ON [ProFormSupplyOrderClientRegionCode].ID = [ProFormSupplyOrderClient].RegionCodeID " +
                "LEFT JOIN [Country] AS [ProFormSupplyOrderClientCountry] " +
                "ON [ProFormSupplyOrderClientCountry].ID = [ProFormSupplyOrderClient].CountryID " +
                "LEFT JOIN [ClientBankDetails] AS [ProFormSupplyOrderClientBankDetails] " +
                "ON [ProFormSupplyOrderClientBankDetails].ID = [ProFormSupplyOrderClient].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] AS [ProFormSupplyOrderClientBankDetailsAccountNumber] " +
                "ON [ProFormSupplyOrderClientBankDetailsAccountNumber].ID = [ProFormSupplyOrderClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency] " +
                "ON [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency].ID = [ProFormSupplyOrderClientBankDetailsAccountNumber].CurrencyID " +
                "AND [ProFormSupplyOrderClientBankDetailsAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] AS [ProFormSupplyOrderClientBankDetailsIbanNo] " +
                "ON [ProFormSupplyOrderClientBankDetailsIbanNo].ID = [ProFormSupplyOrderClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientBankDetailsIbanNoCurrency] " +
                "ON [ProFormSupplyOrderClientBankDetailsIbanNoCurrency].ID = [ProFormSupplyOrderClientBankDetailsIbanNo].CurrencyID " +
                "AND [ProFormSupplyOrderClientBankDetailsIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] AS [ProFormSupplyOrderClientTermsOfDelivery] " +
                "ON [ProFormSupplyOrderClientTermsOfDelivery].ID = [ProFormSupplyOrderClient].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] AS [ProFormSupplyOrderClientPackingMarking] " +
                "ON [ProFormSupplyOrderClientPackingMarking].ID = [ProFormSupplyOrderClient].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] AS [ProFormSupplyOrderClientPackingMarkingPayment] " +
                "ON [ProFormSupplyOrderClientPackingMarkingPayment].ID = [ProFormSupplyOrderClient].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] AS [ProFormSupplyOrderClientClientAgreement] " +
                "ON [ProFormSupplyOrderClientClientAgreement].ClientID = [ProFormSupplyOrderClient].ID " +
                "AND [ProFormSupplyOrderClientClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] AS [ProFormSupplyOrderClientAgreement] " +
                "ON [ProFormSupplyOrderClientAgreement].ID = [ProFormSupplyOrderClientClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] AS [ProFormSupplyOrderClientAgreementProviderPricing] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricing].ID = [ProFormSupplyOrderClientAgreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProFormSupplyOrderClientAgreementProviderPricing].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] AS [ProFormSupplyOrderClientAgreementPricing] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricing].BasePricingID = [ProFormSupplyOrderClientAgreementPricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [ProFormSupplyOrderClientAgreementPricingPriceType] " +
                "ON [ProFormSupplyOrderClientAgreementPricing].PriceTypeID = [ProFormSupplyOrderClientAgreementPricingPriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [ProFormSupplyOrderClientAgreementOrganization] " +
                "ON [ProFormSupplyOrderClientAgreementOrganization].ID = [ProFormSupplyOrderClientAgreement].OrganizationID " +
                "AND [ProFormSupplyOrderClientAgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementCurrency].ID = [ProFormSupplyOrderClientAgreement].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [ProFormSupplyOrderOrganization] " +
                "ON [ProFormSupplyOrderOrganization].ID = [ProFormSupplyOrder].OrganizationID " +
                "AND [ProFormSupplyOrderOrganization].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrderNumber] AS [InvoiceSupplyOrderNumber] " +
                "ON [InvoiceSupplyOrderNumber].ID = [InvoiceSupplyOrder].SupplyOrderNumberID " +
                "LEFT JOIN [SupplyOrderNumber] " +
                "ON [ProFormSupplyOrder].SupplyOrderNumberID = [SupplyOrderNumber].ID " +
                "WHERE [SupplyOrderPaymentDeliveryProtocol].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.SupplyOrderPaymentDeliveryProtocol)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.SupplyOrderPolandPaymentDeliveryProtocol))) {
            Type[] includesTypes = {
                typeof(SupplyOrderPolandPaymentDeliveryProtocol),
                typeof(SupplyOrderPaymentDeliveryProtocolKey),
                typeof(SupplyPaymentTask),
                typeof(User),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization),
                typeof(InvoiceDocument),
                typeof(SupplyOrderNumber)
            };

            Func<object[], SupplyOrderPolandPaymentDeliveryProtocol> includesMapper = objects => {
                SupplyOrderPolandPaymentDeliveryProtocol supplyOrderPolandPaymentDeliveryProtocol = (SupplyOrderPolandPaymentDeliveryProtocol)objects[0];
                SupplyOrderPaymentDeliveryProtocolKey supplyOrderPaymentDeliveryProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[1];
                //SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
                User user = (User)objects[3];
                SupplyOrder supplyOrder = (SupplyOrder)objects[4];
                Client client = (Client)objects[5];
                Region region = (Region)objects[6];
                RegionCode regionCode = (RegionCode)objects[7];
                Country country = (Country)objects[8];
                ClientBankDetails clientBankDetails = (ClientBankDetails)objects[9];
                ClientBankDetailAccountNumber clientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[10];
                Currency clientBankDetailAccountNumberCurrency = (Currency)objects[11];
                ClientBankDetailIbanNo clientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[12];
                Currency clientBankDetailIbanNoCurrency = (Currency)objects[13];
                TermsOfDelivery termsOfDelivery = (TermsOfDelivery)objects[14];
                PackingMarking packingMarking = (PackingMarking)objects[15];
                PackingMarkingPayment packingMarkingPayment = (PackingMarkingPayment)objects[16];
                ClientAgreement clientAgreement = (ClientAgreement)objects[17];
                Agreement agreement = (Agreement)objects[18];
                ProviderPricing providerPricing = (ProviderPricing)objects[19];
                Currency providerPricingCurrency = (Currency)objects[20];
                Pricing pricing = (Pricing)objects[21];
                PriceType priceType = (PriceType)objects[22];
                Organization agreementOrganization = (Organization)objects[23];
                Currency agreementCurrency = (Currency)objects[24];
                Organization organization = (Organization)objects[25];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[26];
                SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[27];

                if (taskToReturn.SupplyOrderPolandPaymentDeliveryProtocols.Any()) {
                    SupplyOrderPolandPaymentDeliveryProtocol protocolFromList = taskToReturn.SupplyOrderPolandPaymentDeliveryProtocols.First();

                    if (invoiceDocument != null && !protocolFromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        protocolFromList.InvoiceDocuments.Add(invoiceDocument);
                } else {
                    if (invoiceDocument != null) supplyOrderPolandPaymentDeliveryProtocol.InvoiceDocuments.Add(invoiceDocument);

                    if (client != null) {
                        if (clientBankDetails != null) {
                            if (clientBankDetailAccountNumber != null) {
                                clientBankDetailAccountNumber.Currency = clientBankDetailAccountNumberCurrency;

                                clientBankDetails.AccountNumber = clientBankDetailAccountNumber;
                            }

                            if (clientBankDetailIbanNo != null) {
                                clientBankDetailIbanNo.Currency = clientBankDetailIbanNoCurrency;

                                clientBankDetails.ClientBankDetailIbanNo = clientBankDetailIbanNo;
                            }
                        }

                        if (clientAgreement != null) {
                            if (providerPricing != null) {
                                if (pricing != null) pricing.PriceType = priceType;

                                providerPricing.Currency = providerPricingCurrency;
                                providerPricing.Pricing = pricing;
                            }

                            agreement.Organization = agreementOrganization;
                            agreement.Currency = agreementCurrency;
                            agreement.ProviderPricing = providerPricing;

                            clientAgreement.Agreement = agreement;

                            client.ClientAgreements.Add(clientAgreement);
                        }

                        client.Region = region;
                        client.RegionCode = regionCode;
                        client.Country = country;
                        client.ClientBankDetails = clientBankDetails;
                        client.TermsOfDelivery = termsOfDelivery;
                        client.PackingMarking = packingMarking;
                        client.PackingMarkingPayment = packingMarkingPayment;
                    }

                    supplyOrder.Client = client;
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                    supplyOrder.Organization = organization;

                    supplyOrderPolandPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = supplyOrderPaymentDeliveryProtocolKey;
                    supplyOrderPolandPaymentDeliveryProtocol.User = user;
                    supplyOrderPolandPaymentDeliveryProtocol.SupplyOrder = supplyOrder;

                    taskToReturn.SupplyOrderPolandPaymentDeliveryProtocols.Add(supplyOrderPolandPaymentDeliveryProtocol);
                }

                return supplyOrderPolandPaymentDeliveryProtocol;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [SupplyOrderPolandPaymentDeliveryProtocol] " +
                "LEFT JOIN [SupplyOrderPaymentDeliveryProtocolKey] " +
                "ON [SupplyOrderPaymentDeliveryProtocolKey].ID = [SupplyOrderPolandPaymentDeliveryProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [SupplyOrderPolandPaymentDeliveryProtocol].SupplyPaymentTaskID " +
                "LEFT JOIN [User] AS [SupplyOrderPolandPaymentDeliveryProtocolUser] " +
                "ON [SupplyOrderPolandPaymentDeliveryProtocolUser].ID = [SupplyOrderPolandPaymentDeliveryProtocol].UserID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyOrderPolandPaymentDeliveryProtocol].SupplyOrderID " +
                "LEFT JOIN [Client] " +
                "ON [SupplyOrder].ClientID = [Client].ID " +
                "LEFT JOIN [Region] " +
                "ON [Region].ID = [Client].RegionID " +
                "LEFT JOIN [RegionCode] " +
                "ON [RegionCode].ID = [Client].RegionCodeID " +
                "LEFT JOIN [Country] " +
                "ON [Country].ID = [Client].CountryID " +
                "LEFT JOIN [ClientBankDetails] " +
                "ON [ClientBankDetails].ID = [Client].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] " +
                "ON [ClientBankDetailAccountNumber].ID = [ClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailAccountNumberCurrency] " +
                "ON [ClientBankDetailAccountNumberCurrency].ID = [ClientBankDetailAccountNumber].CurrencyID " +
                "AND [ClientBankDetailAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] " +
                "ON [ClientBankDetailIbanNo].ID = [ClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailIbanNoCurrency] " +
                "ON [ClientBankDetailIbanNoCurrency].ID = [ClientBankDetailIbanNo].CurrencyID " +
                "AND [ClientBankDetailIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] " +
                "ON [TermsOfDelivery].ID = [Client].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] " +
                "ON [PackingMarking].ID = [Client].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] " +
                "ON [PackingMarkingPayment].ID = [Client].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ClientID = [Client].ID " +
                "AND [ClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] " +
                "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProviderPricing].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] " +
                "ON [ProviderPricing].BasePricingID = [Pricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [PriceType] " +
                "ON [Pricing].PriceTypeID = [PriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
                "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
                "AND [AgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].SupplyOrderPolandPaymentDeliveryProtocolID = [SupplyOrderPolandPaymentDeliveryProtocol].ID " +
                "AND [InvoiceDocument].Deleted = 0 " +
                "LEFT JOIN [SupplyOrderNumber] " +
                "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
                "WHERE [SupplyOrderPolandPaymentDeliveryProtocol].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.SupplyOrderPolandPaymentDeliveryProtocol)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.ContainerService))) {
            Type[] includesTypes = {
                typeof(ContainerService),
                typeof(SupplyPaymentTask),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency),
                typeof(BillOfLadingDocument),
                typeof(User),
                typeof(InvoiceDocument),
                typeof(SupplyOrderContainerService),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization),
                typeof(Organization),
                typeof(SupplyOrderNumber)
            };

            Func<object[], ContainerService> includesMapper = objects => {
                ContainerService containerService = (ContainerService)objects[0];
                //SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[1];
                SupplyOrganization containerOrganization = (SupplyOrganization)objects[2];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[3];
                Currency currency = (Currency)objects[4];
                BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[5];
                User user = (User)objects[6];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[7];
                SupplyOrderContainerService supplyOrderContainerService = (SupplyOrderContainerService)objects[8];
                SupplyOrder supplyOrder = (SupplyOrder)objects[9];
                Client client = (Client)objects[10];
                Region region = (Region)objects[11];
                RegionCode regionCode = (RegionCode)objects[12];
                Country country = (Country)objects[13];
                ClientBankDetails clientBankDetails = (ClientBankDetails)objects[14];
                ClientBankDetailAccountNumber clientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[15];
                Currency clientBankDetailAccountNumberCurrency = (Currency)objects[16];
                ClientBankDetailIbanNo clientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[17];
                Currency clientBankDetailIbanNoCurrency = (Currency)objects[18];
                TermsOfDelivery termsOfDelivery = (TermsOfDelivery)objects[19];
                PackingMarking packingMarking = (PackingMarking)objects[20];
                PackingMarkingPayment packingMarkingPayment = (PackingMarkingPayment)objects[21];
                ClientAgreement clientAgreement = (ClientAgreement)objects[22];
                Agreement agreement = (Agreement)objects[23];
                ProviderPricing providerPricing = (ProviderPricing)objects[24];
                Currency providerPricingCurrency = (Currency)objects[25];
                Pricing pricing = (Pricing)objects[26];
                PriceType priceType = (PriceType)objects[27];
                Organization agreementOrganization = (Organization)objects[28];
                Currency agreementCurrency = (Currency)objects[29];
                Organization organization = (Organization)objects[30];
                Organization supplyOrganizationOrganization = (Organization)objects[31];
                SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[32];

                if (taskToReturn.ContainerServices.Any()) {
                    if (taskToReturn.ContainerServices.Any(s => s.Id.Equals(containerService.Id))) {
                        ContainerService serviceFromList = taskToReturn.ContainerServices.First(s => s.Id.Equals(containerService.Id));

                        if (invoiceDocument != null && !serviceFromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                            serviceFromList.InvoiceDocuments.Add(invoiceDocument);

                        if (supplyOrderContainerService == null || serviceFromList.SupplyOrderContainerServices.Any(s => s.Id.Equals(supplyOrderContainerService.Id)))
                            return containerService;

                        supplyOrder.Client = client;
                        supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                        supplyOrder.Organization = organization;

                        supplyOrderContainerService.SupplyOrder = supplyOrder;

                        serviceFromList.SupplyOrderContainerServices.Add(supplyOrderContainerService);
                    } else {
                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = supplyOrganizationOrganization;
                        }

                        if (invoiceDocument != null) containerService.InvoiceDocuments.Add(invoiceDocument);

                        if (supplyOrderContainerService != null) {
                            if (client != null) {
                                if (clientBankDetails != null) {
                                    if (clientBankDetailAccountNumber != null) {
                                        clientBankDetailAccountNumber.Currency = clientBankDetailAccountNumberCurrency;

                                        clientBankDetails.AccountNumber = clientBankDetailAccountNumber;
                                    }

                                    if (clientBankDetailIbanNo != null) {
                                        clientBankDetailIbanNo.Currency = clientBankDetailIbanNoCurrency;

                                        clientBankDetails.ClientBankDetailIbanNo = clientBankDetailIbanNo;
                                    }
                                }

                                if (clientAgreement != null) {
                                    if (providerPricing != null) {
                                        if (pricing != null) pricing.PriceType = priceType;

                                        providerPricing.Currency = providerPricingCurrency;
                                        providerPricing.Pricing = pricing;
                                    }

                                    agreement.Organization = agreementOrganization;
                                    agreement.Currency = agreementCurrency;
                                    agreement.ProviderPricing = providerPricing;

                                    clientAgreement.Agreement = agreement;

                                    client.ClientAgreements.Add(clientAgreement);
                                }

                                client.Region = region;
                                client.RegionCode = regionCode;
                                client.Country = country;
                                client.ClientBankDetails = clientBankDetails;
                                client.TermsOfDelivery = termsOfDelivery;
                                client.PackingMarking = packingMarking;
                                client.PackingMarkingPayment = packingMarkingPayment;
                            }

                            supplyOrder.Client = client;
                            supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                            supplyOrder.Organization = organization;

                            supplyOrderContainerService.SupplyOrder = supplyOrder;

                            containerService.SupplyOrderContainerServices.Add(supplyOrderContainerService);
                        }

                        containerService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        containerService.ContainerOrganization = containerOrganization;
                        containerService.BillOfLadingDocument = billOfLadingDocument;
                        containerService.User = user;

                        taskToReturn.ContainerServices.Add(containerService);
                    }
                } else {
                    if (invoiceDocument != null) containerService.InvoiceDocuments.Add(invoiceDocument);

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = supplyOrganizationOrganization;
                    }

                    if (supplyOrderContainerService != null) {
                        if (client != null) {
                            if (clientBankDetails != null) {
                                if (clientBankDetailAccountNumber != null) {
                                    clientBankDetailAccountNumber.Currency = clientBankDetailAccountNumberCurrency;

                                    clientBankDetails.AccountNumber = clientBankDetailAccountNumber;
                                }

                                if (clientBankDetailIbanNo != null) {
                                    clientBankDetailIbanNo.Currency = clientBankDetailIbanNoCurrency;

                                    clientBankDetails.ClientBankDetailIbanNo = clientBankDetailIbanNo;
                                }
                            }

                            if (clientAgreement != null) {
                                if (providerPricing != null) {
                                    if (pricing != null) pricing.PriceType = priceType;

                                    providerPricing.Currency = providerPricingCurrency;
                                    providerPricing.Pricing = pricing;
                                }

                                agreement.Organization = agreementOrganization;
                                agreement.Currency = agreementCurrency;
                                agreement.ProviderPricing = providerPricing;

                                clientAgreement.Agreement = agreement;

                                client.ClientAgreements.Add(clientAgreement);
                            }

                            client.Region = region;
                            client.RegionCode = regionCode;
                            client.Country = country;
                            client.ClientBankDetails = clientBankDetails;
                            client.TermsOfDelivery = termsOfDelivery;
                            client.PackingMarking = packingMarking;
                            client.PackingMarkingPayment = packingMarkingPayment;
                        }

                        supplyOrder.Client = client;
                        supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                        supplyOrder.Organization = organization;

                        supplyOrderContainerService.SupplyOrder = supplyOrder;

                        containerService.SupplyOrderContainerServices.Add(supplyOrderContainerService);
                    }

                    containerService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    containerService.ContainerOrganization = containerOrganization;
                    containerService.BillOfLadingDocument = billOfLadingDocument;
                    containerService.User = user;

                    taskToReturn.ContainerServices.Add(containerService);
                }

                return containerService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [ContainerService] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [ContainerService].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrganization] AS [ContainerOrganization] " +
                "ON [ContainerOrganization].ID = [ContainerService].ContainerOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [BillOfLadingDocument] " +
                "ON [BillOfLadingDocument].ID = [ContainerService].BillOfLadingDocumentID " +
                "LEFT JOIN [User] AS [ContainerServiceUser] " +
                "ON [ContainerServiceUser].ID = [ContainerService].UserID " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].ContainerServiceID = [ContainerService].ID " +
                "LEFT JOIN [SupplyOrderContainerService] " +
                "ON [SupplyOrderContainerService].ContainerServiceID = [ContainerService].ID " +
                "AND [SupplyOrderContainerService].Deleted = 0 " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyOrderContainerService].SupplyOrderID " +
                "LEFT JOIN [Client] " +
                "ON [SupplyOrder].ClientID = [Client].ID " +
                "LEFT JOIN [Region] " +
                "ON [Region].ID = [Client].RegionID " +
                "LEFT JOIN [RegionCode] " +
                "ON [RegionCode].ID = [Client].RegionCodeID " +
                "LEFT JOIN [Country] " +
                "ON [Country].ID = [Client].CountryID " +
                "LEFT JOIN [ClientBankDetails] " +
                "ON [ClientBankDetails].ID = [Client].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] " +
                "ON [ClientBankDetailAccountNumber].ID = [ClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailAccountNumberCurrency] " +
                "ON [ClientBankDetailAccountNumberCurrency].ID = [ClientBankDetailAccountNumber].CurrencyID " +
                "AND [ClientBankDetailAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] " +
                "ON [ClientBankDetailIbanNo].ID = [ClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailIbanNoCurrency] " +
                "ON [ClientBankDetailIbanNoCurrency].ID = [ClientBankDetailIbanNo].CurrencyID " +
                "AND [ClientBankDetailIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] " +
                "ON [TermsOfDelivery].ID = [Client].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] " +
                "ON [PackingMarking].ID = [Client].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] " +
                "ON [PackingMarkingPayment].ID = [Client].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ClientID = [Client].ID " +
                "AND [ClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] " +
                "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProviderPricing].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] " +
                "ON [ProviderPricing].BasePricingID = [Pricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [PriceType] " +
                "ON [Pricing].PriceTypeID = [PriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
                "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
                "AND [AgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [SupplyOrganizationOrganization] " +
                "ON [SupplyOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [SupplyOrganizationOrganization].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrderNumber] " +
                "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
                "WHERE [ContainerService].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.ContainerService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.CustomService))) {
            Type[] includesTypes = {
                typeof(CustomService),
                typeof(User),
                typeof(SupplyPaymentTask),
                typeof(SupplyOrganization),
                typeof(SupplyOrganization),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization),
                typeof(InvoiceDocument),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(SupplyOrderNumber),
                typeof(SupplyOrganizationAgreement),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Organization),
                typeof(Currency)
            };

            Func<object[], CustomService> includesMapper = objects => {
                CustomService customService = (CustomService)objects[0];
                User user = (User)objects[1];
                //SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
                SupplyOrganization customOrganization = (SupplyOrganization)objects[3];
                SupplyOrganization exciseDutyOrganization = (SupplyOrganization)objects[4];
                SupplyOrder supplyOrder = (SupplyOrder)objects[5];
                Client client = (Client)objects[6];
                Region region = (Region)objects[7];
                RegionCode regionCode = (RegionCode)objects[8];
                Country country = (Country)objects[9];
                ClientBankDetails clientBankDetails = (ClientBankDetails)objects[10];
                ClientBankDetailAccountNumber clientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[11];
                Currency clientBankDetailAccountNumberCurrency = (Currency)objects[12];
                ClientBankDetailIbanNo clientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[13];
                Currency clientBankDetailIbanNoCurrency = (Currency)objects[14];
                TermsOfDelivery termsOfDelivery = (TermsOfDelivery)objects[15];
                PackingMarking packingMarking = (PackingMarking)objects[16];
                PackingMarkingPayment packingMarkingPayment = (PackingMarkingPayment)objects[17];
                ClientAgreement clientAgreement = (ClientAgreement)objects[18];
                Agreement agreement = (Agreement)objects[19];
                ProviderPricing providerPricing = (ProviderPricing)objects[20];
                Currency providerPricingCurrency = (Currency)objects[21];
                Pricing pricing = (Pricing)objects[22];
                PriceType priceType = (PriceType)objects[23];
                Organization agreementOrganization = (Organization)objects[24];
                Currency agreementCurrency = (Currency)objects[25];
                Organization organization = (Organization)objects[26];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[27];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[28];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[29];
                SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[30];
                SupplyOrganizationAgreement customOrganizationAgreement = (SupplyOrganizationAgreement)objects[31];
                SupplyOrganizationAgreement exciseDutyOrganizationAgreement = (SupplyOrganizationAgreement)objects[32];
                Organization customSupplyOrganizationOrganization = (Organization)objects[33];
                Organization exciseOrganizationOrganization = (Organization)objects[34];
                Currency currency = (Currency)objects[35];

                if (taskToReturn.BrokerServices.Any()) {
                    CustomService serviceFromList = taskToReturn.BrokerServices.First();

                    if (invoiceDocument != null && !serviceFromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        serviceFromList.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem == null || serviceFromList.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) return customService;

                    serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                    serviceFromList.ServiceDetailItems.Add(serviceDetailItem);
                } else {
                    if (exciseDutyOrganization != null) exciseDutyOrganizationAgreement.Organization = exciseOrganizationOrganization;

                    if (customOrganization != null) customOrganizationAgreement.Organization = customSupplyOrganizationOrganization;

                    if (invoiceDocument != null) customService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        customService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (customOrganizationAgreement != null) customOrganizationAgreement.Currency = currency;

                    if (client != null) {
                        if (clientBankDetails != null) {
                            if (clientBankDetailAccountNumber != null) {
                                clientBankDetailAccountNumber.Currency = clientBankDetailAccountNumberCurrency;

                                clientBankDetails.AccountNumber = clientBankDetailAccountNumber;
                            }

                            if (clientBankDetailIbanNo != null) {
                                clientBankDetailIbanNo.Currency = clientBankDetailIbanNoCurrency;

                                clientBankDetails.ClientBankDetailIbanNo = clientBankDetailIbanNo;
                            }
                        }

                        if (clientAgreement != null) {
                            if (providerPricing != null) {
                                if (pricing != null) pricing.PriceType = priceType;

                                providerPricing.Currency = providerPricingCurrency;
                                providerPricing.Pricing = pricing;
                            }

                            agreement.Organization = agreementOrganization;
                            agreement.Currency = agreementCurrency;
                            agreement.ProviderPricing = providerPricing;

                            clientAgreement.Agreement = agreement;

                            client.ClientAgreements.Add(clientAgreement);
                        }

                        client.Region = region;
                        client.RegionCode = regionCode;
                        client.Country = country;
                        client.ClientBankDetails = clientBankDetails;
                        client.TermsOfDelivery = termsOfDelivery;
                        client.PackingMarking = packingMarking;
                        client.PackingMarkingPayment = packingMarkingPayment;
                    }

                    supplyOrder.Client = client;
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                    supplyOrder.Organization = organization;

                    customService.SupplyOrganizationAgreement = customOrganizationAgreement;
                    customService.SupplyOrder = supplyOrder;
                    customService.User = user;

                    customService.ExciseDutyOrganization = exciseDutyOrganization;

                    if (exciseDutyOrganization != null && exciseDutyOrganizationAgreement != null) {
                        if (!exciseDutyOrganization.SupplyOrganizationAgreements.Any(x => x.Id != exciseDutyOrganizationAgreement.Id))
                            exciseDutyOrganization.SupplyOrganizationAgreements.Add(exciseDutyOrganizationAgreement);
                        else
                            exciseDutyOrganizationAgreement = exciseDutyOrganization.SupplyOrganizationAgreements.First(x => x.Id == exciseDutyOrganizationAgreement.Id);

                        exciseDutyOrganizationAgreement.Organization = exciseOrganizationOrganization;
                    }

                    customService.CustomOrganization = customOrganization;

                    taskToReturn.BrokerServices.Add(customService);
                }

                return customService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [CustomService] " +
                "LEFT JOIN [User] AS [CustomServiceUser] " +
                "ON [CustomServiceUser].ID = [CustomService].UserID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [CustomService].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrganization] AS [CustomOrganization] " +
                "ON [CustomOrganization].ID = [CustomService].CustomOrganizationID " +
                "LEFT JOIN [SupplyOrganization] AS [ExciseDutyOrganization] " +
                "ON [ExciseDutyOrganization].ID = [CustomService].ExciseDutyOrganizationID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [CustomService].SupplyOrderID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [SupplyOrder].ClientID " +
                "LEFT JOIN [Region] " +
                "ON [Region].ID = [Client].RegionID " +
                "LEFT JOIN [RegionCode] " +
                "ON [RegionCode].ID = [Client].RegionCodeID " +
                "LEFT JOIN [Country] " +
                "ON [Country].ID = [Client].CountryID " +
                "LEFT JOIN [ClientBankDetails] " +
                "ON [ClientBankDetails].ID = [Client].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] " +
                "ON [ClientBankDetailAccountNumber].ID = [ClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailAccountNumberCurrency] " +
                "ON [ClientBankDetailAccountNumberCurrency].ID = [ClientBankDetailAccountNumber].CurrencyID " +
                "AND [ClientBankDetailAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] " +
                "ON [ClientBankDetailIbanNo].ID = [ClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailIbanNoCurrency] " +
                "ON [ClientBankDetailIbanNoCurrency].ID = [ClientBankDetailIbanNo].CurrencyID " +
                "AND [ClientBankDetailIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] " +
                "ON [TermsOfDelivery].ID = [Client].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] " +
                "ON [PackingMarking].ID = [Client].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] " +
                "ON [PackingMarkingPayment].ID = [Client].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ClientID = [Client].ID " +
                "AND [ClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] " +
                "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProviderPricing].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] " +
                "ON [ProviderPricing].BasePricingID = [Pricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [PriceType] " +
                "ON [Pricing].PriceTypeID = [PriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
                "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
                "AND [AgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].CustomServiceID = [CustomService].ID " +
                "AND [InvoiceDocument].Deleted = 0 " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].CustomServiceID = [CustomService].ID " +
                "AND [ServiceDetailItem].Deleted = 0 " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [SupplyOrderNumber] " +
                "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [CustomService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [SupplyOrganizationAgreement] AS [ExciseSupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [ExciseDutyOrganization].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [SupplyOrganizationOrganization] " +
                "ON [SupplyOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [SupplyOrganizationOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [ExciseSupplyOrganizationOrganization] " +
                "ON [CustomSupplyOrganizationOrganization].ID = [ExciseSupplyOrganizationAgreement].OrganizationID " +
                "AND [CustomSupplyOrganizationOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "WHERE [CustomService].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.CustomService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.PortWorkService))) {
            Type[] includesTypes = {
                typeof(PortWorkService),
                typeof(User),
                typeof(SupplyPaymentTask),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency),
                typeof(InvoiceDocument),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization),
                typeof(Organization),
                typeof(SupplyOrderNumber)
            };

            Func<object[], PortWorkService> includesMapper = objects => {
                PortWorkService portWorkService = (PortWorkService)objects[0];
                User user = (User)objects[1];
                //SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
                SupplyOrganization portWorkOrganization = (SupplyOrganization)objects[3];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[4];
                Currency currency = (Currency)objects[5];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[7];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[8];
                SupplyOrder supplyOrder = (SupplyOrder)objects[9];
                Client client = (Client)objects[10];
                Region region = (Region)objects[11];
                RegionCode regionCode = (RegionCode)objects[12];
                Country country = (Country)objects[13];
                ClientBankDetails clientBankDetails = (ClientBankDetails)objects[14];
                ClientBankDetailAccountNumber clientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[15];
                Currency clientBankDetailAccountNumberCurrency = (Currency)objects[16];
                ClientBankDetailIbanNo clientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[17];
                Currency clientBankDetailIbanNoCurrency = (Currency)objects[18];
                TermsOfDelivery termsOfDelivery = (TermsOfDelivery)objects[19];
                PackingMarking packingMarking = (PackingMarking)objects[20];
                PackingMarkingPayment packingMarkingPayment = (PackingMarkingPayment)objects[21];
                ClientAgreement clientAgreement = (ClientAgreement)objects[22];
                Agreement agreement = (Agreement)objects[23];
                ProviderPricing providerPricing = (ProviderPricing)objects[24];
                Currency providerPricingCurrency = (Currency)objects[25];
                Pricing pricing = (Pricing)objects[26];
                PriceType priceType = (PriceType)objects[27];
                Organization agreementOrganization = (Organization)objects[28];
                Currency agreementCurrency = (Currency)objects[29];
                Organization organization = (Organization)objects[30];
                Organization supplyOrganizationOrganization = (Organization)objects[31];
                SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[32];

                if (taskToReturn.PortWorkServices.Any()) {
                    if (taskToReturn.PortWorkServices.Any(s => s.Id.Equals(portWorkService.Id))) {
                        PortWorkService serviceFromList = taskToReturn.PortWorkServices.First();

                        if (invoiceDocument != null && !serviceFromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                            serviceFromList.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null && !serviceFromList.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            serviceFromList.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrder == null || serviceFromList.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id))) return portWorkService;

                        supplyOrder.Client = client;
                        supplyOrder.Organization = organization;

                        serviceFromList.SupplyOrders.Add(supplyOrder);
                    } else {
                        if (invoiceDocument != null) portWorkService.InvoiceDocuments.Add(invoiceDocument);

                        if (serviceDetailItem != null) {
                            serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                            portWorkService.ServiceDetailItems.Add(serviceDetailItem);
                        }

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = currency;

                            supplyOrganizationAgreement.Organization = supplyOrganizationOrganization;
                        }

                        if (supplyOrder != null) {
                            if (client != null) {
                                if (clientBankDetails != null) {
                                    if (clientBankDetailAccountNumber != null) {
                                        clientBankDetailAccountNumber.Currency = clientBankDetailAccountNumberCurrency;

                                        clientBankDetails.AccountNumber = clientBankDetailAccountNumber;
                                    }

                                    if (clientBankDetailIbanNo != null) {
                                        clientBankDetailIbanNo.Currency = clientBankDetailIbanNoCurrency;

                                        clientBankDetails.ClientBankDetailIbanNo = clientBankDetailIbanNo;
                                    }
                                }

                                if (clientAgreement != null) {
                                    if (providerPricing != null) {
                                        if (pricing != null) pricing.PriceType = priceType;

                                        providerPricing.Currency = providerPricingCurrency;
                                        providerPricing.Pricing = pricing;
                                    }

                                    agreement.Organization = agreementOrganization;
                                    agreement.Currency = agreementCurrency;
                                    agreement.ProviderPricing = providerPricing;

                                    clientAgreement.Agreement = agreement;

                                    client.ClientAgreements.Add(clientAgreement);
                                }

                                client.Region = region;
                                client.RegionCode = regionCode;
                                client.Country = country;
                                client.ClientBankDetails = clientBankDetails;
                                client.TermsOfDelivery = termsOfDelivery;
                                client.PackingMarking = packingMarking;
                                client.PackingMarkingPayment = packingMarkingPayment;
                            }

                            supplyOrder.Client = client;
                            supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                            supplyOrder.Organization = organization;

                            portWorkService.SupplyOrders.Add(supplyOrder);
                        }

                        portWorkService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                        portWorkService.User = user;
                        portWorkService.PortWorkOrganization = portWorkOrganization;

                        taskToReturn.PortWorkServices.Add(portWorkService);
                    }
                } else {
                    if (invoiceDocument != null) portWorkService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        portWorkService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = supplyOrganizationOrganization;
                    }

                    if (supplyOrder != null) {
                        if (client != null) {
                            if (clientBankDetails != null) {
                                if (clientBankDetailAccountNumber != null) {
                                    clientBankDetailAccountNumber.Currency = clientBankDetailAccountNumberCurrency;

                                    clientBankDetails.AccountNumber = clientBankDetailAccountNumber;
                                }

                                if (clientBankDetailIbanNo != null) {
                                    clientBankDetailIbanNo.Currency = clientBankDetailIbanNoCurrency;

                                    clientBankDetails.ClientBankDetailIbanNo = clientBankDetailIbanNo;
                                }
                            }

                            if (clientAgreement != null) {
                                if (providerPricing != null) {
                                    if (pricing != null) pricing.PriceType = priceType;

                                    providerPricing.Currency = providerPricingCurrency;
                                    providerPricing.Pricing = pricing;
                                }

                                agreement.Organization = agreementOrganization;
                                agreement.Currency = agreementCurrency;
                                agreement.ProviderPricing = providerPricing;

                                clientAgreement.Agreement = agreement;

                                client.ClientAgreements.Add(clientAgreement);
                            }

                            client.Region = region;
                            client.RegionCode = regionCode;
                            client.Country = country;
                            client.ClientBankDetails = clientBankDetails;
                            client.TermsOfDelivery = termsOfDelivery;
                            client.PackingMarking = packingMarking;
                            client.PackingMarkingPayment = packingMarkingPayment;
                        }

                        supplyOrder.Client = client;
                        supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                        supplyOrder.Organization = organization;

                        portWorkService.SupplyOrders.Add(supplyOrder);
                    }

                    portWorkService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    portWorkService.User = user;
                    portWorkService.PortWorkOrganization = portWorkOrganization;

                    taskToReturn.PortWorkServices.Add(portWorkService);
                }

                return portWorkService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [PortWorkService] " +
                "LEFT JOIN [User] AS [PortWorkServiceUser] " +
                "ON [PortWorkServiceUser].ID = [PortWorkService].UserID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [PortWorkService].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrganization] AS [PortWorkOrganization] " +
                "ON [PortWorkOrganization].ID = [PortWorkService].PortWorkOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].PortWorkServiceID = [PortWorkService].ID " +
                "AND [InvoiceDocument].Deleted = 0 " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PortWorkServiceID = [PortWorkService].ID " +
                "AND [ServiceDetailItem].Deleted = 0 " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [SupplyOrder].ClientID " +
                "LEFT JOIN [Region] " +
                "ON [Region].ID = [Client].RegionID " +
                "LEFT JOIN [RegionCode] " +
                "ON [RegionCode].ID = [Client].RegionCodeID " +
                "LEFT JOIN [Country] " +
                "ON [Country].ID = [Client].CountryID " +
                "LEFT JOIN [ClientBankDetails] " +
                "ON [ClientBankDetails].ID = [Client].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] " +
                "ON [ClientBankDetailAccountNumber].ID = [ClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailAccountNumberCurrency] " +
                "ON [ClientBankDetailAccountNumberCurrency].ID = [ClientBankDetailAccountNumber].CurrencyID " +
                "AND [ClientBankDetailAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] " +
                "ON [ClientBankDetailIbanNo].ID = [ClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailIbanNoCurrency] " +
                "ON [ClientBankDetailIbanNoCurrency].ID = [ClientBankDetailIbanNo].CurrencyID " +
                "AND [ClientBankDetailIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] " +
                "ON [TermsOfDelivery].ID = [Client].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] " +
                "ON [PackingMarking].ID = [Client].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] " +
                "ON [PackingMarkingPayment].ID = [Client].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ClientID = [Client].ID " +
                "AND [ClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] " +
                "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProviderPricing].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] " +
                "ON [ProviderPricing].BasePricingID = [Pricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [PriceType] " +
                "ON [Pricing].PriceTypeID = [PriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
                "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
                "AND [AgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [SupplyOrganizationOrganization] " +
                "ON [SupplyOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [SupplyOrganizationOrganization].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrderNumber] " +
                "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
                "WHERE [PortWorkService].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.PortWorkService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.TransportationService))) {
            Type[] includesTypes = {
                typeof(TransportationService),
                typeof(User),
                typeof(SupplyPaymentTask),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency),
                typeof(InvoiceDocument),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization),
                typeof(Organization),
                typeof(SupplyOrderNumber)
            };

            Func<object[], TransportationService> includesMapper = objects => {
                TransportationService transportationService = (TransportationService)objects[0];
                User user = (User)objects[1];
                //SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
                SupplyOrganization transportationOrganization = (SupplyOrganization)objects[3];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[4];
                Currency currency = (Currency)objects[5];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[7];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[8];
                SupplyOrder supplyOrder = (SupplyOrder)objects[9];
                Client client = (Client)objects[10];
                Region region = (Region)objects[11];
                RegionCode regionCode = (RegionCode)objects[12];
                Country country = (Country)objects[13];
                ClientBankDetails clientBankDetails = (ClientBankDetails)objects[14];
                ClientBankDetailAccountNumber clientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[15];
                Currency clientBankDetailAccountNumberCurrency = (Currency)objects[16];
                ClientBankDetailIbanNo clientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[17];
                Currency clientBankDetailIbanNoCurrency = (Currency)objects[18];
                TermsOfDelivery termsOfDelivery = (TermsOfDelivery)objects[19];
                PackingMarking packingMarking = (PackingMarking)objects[20];
                PackingMarkingPayment packingMarkingPayment = (PackingMarkingPayment)objects[21];
                ClientAgreement clientAgreement = (ClientAgreement)objects[22];
                Agreement agreement = (Agreement)objects[23];
                ProviderPricing providerPricing = (ProviderPricing)objects[24];
                Currency providerPricingCurrency = (Currency)objects[25];
                Pricing pricing = (Pricing)objects[26];
                PriceType priceType = (PriceType)objects[27];
                Organization agreementOrganization = (Organization)objects[28];
                Currency agreementCurrency = (Currency)objects[29];
                Organization organization = (Organization)objects[30];
                Organization supplyOrganizationOrganization = (Organization)objects[31];
                SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[32];

                if (taskToReturn.TransportationServices.Any()) {
                    TransportationService serviceFromList = taskToReturn.TransportationServices.First();

                    if (invoiceDocument != null && !serviceFromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        serviceFromList.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !serviceFromList.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        serviceFromList.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder == null || serviceFromList.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id))) return transportationService;

                    supplyOrder.Client = client;
                    supplyOrder.Organization = organization;

                    serviceFromList.SupplyOrders.Add(supplyOrder);
                } else {
                    if (invoiceDocument != null) transportationService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        transportationService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = supplyOrganizationOrganization;
                    }

                    if (supplyOrder != null) {
                        if (client != null) {
                            if (clientBankDetails != null) {
                                if (clientBankDetailAccountNumber != null) {
                                    clientBankDetailAccountNumber.Currency = clientBankDetailAccountNumberCurrency;

                                    clientBankDetails.AccountNumber = clientBankDetailAccountNumber;
                                }

                                if (clientBankDetailIbanNo != null) {
                                    clientBankDetailIbanNo.Currency = clientBankDetailIbanNoCurrency;

                                    clientBankDetails.ClientBankDetailIbanNo = clientBankDetailIbanNo;
                                }
                            }

                            if (clientAgreement != null) {
                                if (providerPricing != null) {
                                    if (pricing != null) pricing.PriceType = priceType;

                                    providerPricing.Currency = providerPricingCurrency;
                                    providerPricing.Pricing = pricing;
                                }

                                agreement.Organization = agreementOrganization;
                                agreement.Currency = agreementCurrency;
                                agreement.ProviderPricing = providerPricing;

                                clientAgreement.Agreement = agreement;

                                client.ClientAgreements.Add(clientAgreement);
                            }

                            client.Region = region;
                            client.RegionCode = regionCode;
                            client.Country = country;
                            client.ClientBankDetails = clientBankDetails;
                            client.TermsOfDelivery = termsOfDelivery;
                            client.PackingMarking = packingMarking;
                            client.PackingMarkingPayment = packingMarkingPayment;
                        }

                        supplyOrder.Client = client;
                        supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                        supplyOrder.Organization = organization;

                        transportationService.SupplyOrders.Add(supplyOrder);
                    }

                    transportationService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    transportationService.User = user;
                    transportationService.TransportationOrganization = transportationOrganization;

                    taskToReturn.TransportationServices.Add(transportationService);
                }

                return transportationService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [TransportationService] " +
                "LEFT JOIN [User] AS [TransportationServiceUser] " +
                "ON [TransportationServiceUser].ID = [TransportationService].UserID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [TransportationService].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrganization] AS [TransportationOrganization] " +
                "ON [TransportationOrganization].ID = [TransportationService].TransportationOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [TransportationService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].TransportationServiceID = [TransportationService].ID " +
                "AND [InvoiceDocument].Deleted = 0 " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].TransportationServiceID = [TransportationService].ID " +
                "AND [ServiceDetailItem].Deleted = 0 " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].TransportationServiceID = [TransportationService].ID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [SupplyOrder].ClientID " +
                "LEFT JOIN [Region] " +
                "ON [Region].ID = [Client].RegionID " +
                "LEFT JOIN [RegionCode] " +
                "ON [RegionCode].ID = [Client].RegionCodeID " +
                "LEFT JOIN [Country] " +
                "ON [Country].ID = [Client].CountryID " +
                "LEFT JOIN [ClientBankDetails] " +
                "ON [ClientBankDetails].ID = [Client].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] " +
                "ON [ClientBankDetailAccountNumber].ID = [ClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailAccountNumberCurrency] " +
                "ON [ClientBankDetailAccountNumberCurrency].ID = [ClientBankDetailAccountNumber].CurrencyID " +
                "AND [ClientBankDetailAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] " +
                "ON [ClientBankDetailIbanNo].ID = [ClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailIbanNoCurrency] " +
                "ON [ClientBankDetailIbanNoCurrency].ID = [ClientBankDetailIbanNo].CurrencyID " +
                "AND [ClientBankDetailIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] " +
                "ON [TermsOfDelivery].ID = [Client].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] " +
                "ON [PackingMarking].ID = [Client].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] " +
                "ON [PackingMarkingPayment].ID = [Client].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ClientID = [Client].ID " +
                "AND [ClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] " +
                "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProviderPricing].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] " +
                "ON [ProviderPricing].BasePricingID = [Pricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [PriceType] " +
                "ON [Pricing].PriceTypeID = [PriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
                "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
                "AND [AgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [SupplyOrganizationOrganization] " +
                "ON [SupplyOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [SupplyOrganizationOrganization].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrderNumber] " +
                "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
                "WHERE [TransportationService].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.TransportationService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.PortCustomAgencyService))) {
            Type[] includesTypes = {
                typeof(PortCustomAgencyService),
                typeof(User),
                typeof(SupplyPaymentTask),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency),
                typeof(InvoiceDocument),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization),
                typeof(Organization),
                typeof(SupplyOrderNumber)
            };

            Func<object[], PortCustomAgencyService> includesMapper = objects => {
                PortCustomAgencyService portCustomAgencyService = (PortCustomAgencyService)objects[0];
                User user = (User)objects[1];
                //SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
                SupplyOrganization portCustomAgencyOrganization = (SupplyOrganization)objects[3];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[4];
                Currency currency = (Currency)objects[5];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[7];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[8];
                SupplyOrder supplyOrder = (SupplyOrder)objects[9];
                Client client = (Client)objects[10];
                Region region = (Region)objects[11];
                RegionCode regionCode = (RegionCode)objects[12];
                Country country = (Country)objects[13];
                ClientBankDetails clientBankDetails = (ClientBankDetails)objects[14];
                ClientBankDetailAccountNumber clientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[15];
                Currency clientBankDetailAccountNumberCurrency = (Currency)objects[16];
                ClientBankDetailIbanNo clientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[17];
                Currency clientBankDetailIbanNoCurrency = (Currency)objects[18];
                TermsOfDelivery termsOfDelivery = (TermsOfDelivery)objects[19];
                PackingMarking packingMarking = (PackingMarking)objects[20];
                PackingMarkingPayment packingMarkingPayment = (PackingMarkingPayment)objects[21];
                ClientAgreement clientAgreement = (ClientAgreement)objects[22];
                Agreement agreement = (Agreement)objects[23];
                ProviderPricing providerPricing = (ProviderPricing)objects[24];
                Currency providerPricingCurrency = (Currency)objects[25];
                Pricing pricing = (Pricing)objects[26];
                PriceType priceType = (PriceType)objects[27];
                Organization agreementOrganization = (Organization)objects[28];
                Currency agreementCurrency = (Currency)objects[29];
                Organization organization = (Organization)objects[30];
                Organization supplyOrganizationOrganization = (Organization)objects[31];
                SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[32];

                if (taskToReturn.PortCustomAgencyServices.Any()) {
                    PortCustomAgencyService serviceFromList = taskToReturn.PortCustomAgencyServices.First();

                    if (invoiceDocument != null && !serviceFromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        serviceFromList.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !serviceFromList.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        serviceFromList.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder == null || serviceFromList.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id))) return portCustomAgencyService;

                    supplyOrder.Client = client;
                    supplyOrder.Organization = organization;

                    serviceFromList.SupplyOrders.Add(supplyOrder);
                } else {
                    if (invoiceDocument != null) portCustomAgencyService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        portCustomAgencyService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = supplyOrganizationOrganization;
                    }

                    if (supplyOrder != null) {
                        if (client != null) {
                            if (clientBankDetails != null) {
                                if (clientBankDetailAccountNumber != null) {
                                    clientBankDetailAccountNumber.Currency = clientBankDetailAccountNumberCurrency;

                                    clientBankDetails.AccountNumber = clientBankDetailAccountNumber;
                                }

                                if (clientBankDetailIbanNo != null) {
                                    clientBankDetailIbanNo.Currency = clientBankDetailIbanNoCurrency;

                                    clientBankDetails.ClientBankDetailIbanNo = clientBankDetailIbanNo;
                                }
                            }

                            if (clientAgreement != null) {
                                if (providerPricing != null) {
                                    if (pricing != null) pricing.PriceType = priceType;

                                    providerPricing.Currency = providerPricingCurrency;
                                    providerPricing.Pricing = pricing;
                                }

                                agreement.Organization = agreementOrganization;
                                agreement.Currency = agreementCurrency;
                                agreement.ProviderPricing = providerPricing;

                                clientAgreement.Agreement = agreement;

                                client.ClientAgreements.Add(clientAgreement);
                            }

                            client.Region = region;
                            client.RegionCode = regionCode;
                            client.Country = country;
                            client.ClientBankDetails = clientBankDetails;
                            client.TermsOfDelivery = termsOfDelivery;
                            client.PackingMarking = packingMarking;
                            client.PackingMarkingPayment = packingMarkingPayment;
                        }

                        supplyOrder.Client = client;
                        supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                        supplyOrder.Organization = organization;

                        portCustomAgencyService.SupplyOrders.Add(supplyOrder);
                    }

                    portCustomAgencyService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    portCustomAgencyService.User = user;
                    portCustomAgencyService.PortCustomAgencyOrganization = portCustomAgencyOrganization;

                    taskToReturn.PortCustomAgencyServices.Add(portCustomAgencyService);
                }

                return portCustomAgencyService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [PortCustomAgencyService] " +
                "LEFT JOIN [User] AS [PortCustomAgencyServiceUser] " +
                "ON [PortCustomAgencyServiceUser].ID = [PortCustomAgencyService].UserID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [PortCustomAgencyService].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrganization] AS [PortCustomAgencyOrganization] " +
                "ON [PortCustomAgencyOrganization].ID = [PortCustomAgencyService].PortCustomAgencyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PortCustomAgencyService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
                "AND [InvoiceDocument].Deleted = 0 " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
                "AND [ServiceDetailItem].Deleted = 0 " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [SupplyOrder].ClientID " +
                "LEFT JOIN [Region] " +
                "ON [Region].ID = [Client].RegionID " +
                "LEFT JOIN [RegionCode] " +
                "ON [RegionCode].ID = [Client].RegionCodeID " +
                "LEFT JOIN [Country] " +
                "ON [Country].ID = [Client].CountryID " +
                "LEFT JOIN [ClientBankDetails] " +
                "ON [ClientBankDetails].ID = [Client].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] " +
                "ON [ClientBankDetailAccountNumber].ID = [ClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailAccountNumberCurrency] " +
                "ON [ClientBankDetailAccountNumberCurrency].ID = [ClientBankDetailAccountNumber].CurrencyID " +
                "AND [ClientBankDetailAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] " +
                "ON [ClientBankDetailIbanNo].ID = [ClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailIbanNoCurrency] " +
                "ON [ClientBankDetailIbanNoCurrency].ID = [ClientBankDetailIbanNo].CurrencyID " +
                "AND [ClientBankDetailIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] " +
                "ON [TermsOfDelivery].ID = [Client].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] " +
                "ON [PackingMarking].ID = [Client].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] " +
                "ON [PackingMarkingPayment].ID = [Client].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ClientID = [Client].ID " +
                "AND [ClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] " +
                "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProviderPricing].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] " +
                "ON [ProviderPricing].BasePricingID = [Pricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [PriceType] " +
                "ON [Pricing].PriceTypeID = [PriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
                "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
                "AND [AgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [SupplyOrganizationOrganization] " +
                "ON [SupplyOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [SupplyOrganizationOrganization].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrderNumber] " +
                "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
                "WHERE [PortCustomAgencyService].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.PortCustomAgencyService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.CustomAgencyService))) {
            Type[] includesTypes = {
                typeof(CustomAgencyService),
                typeof(User),
                typeof(SupplyPaymentTask),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency),
                typeof(InvoiceDocument),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization),
                typeof(Organization),
                typeof(SupplyOrderNumber)
            };

            Func<object[], CustomAgencyService> includesMapper = objects => {
                CustomAgencyService customAgencyService = (CustomAgencyService)objects[0];
                User user = (User)objects[1];
                //SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
                SupplyOrganization customAgencyOrganization = (SupplyOrganization)objects[3];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[4];
                Currency currency = (Currency)objects[5];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[7];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[8];
                SupplyOrder supplyOrder = (SupplyOrder)objects[9];
                Client client = (Client)objects[10];
                Region region = (Region)objects[11];
                RegionCode regionCode = (RegionCode)objects[12];
                Country country = (Country)objects[13];
                ClientBankDetails clientBankDetails = (ClientBankDetails)objects[14];
                ClientBankDetailAccountNumber clientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[15];
                Currency clientBankDetailAccountNumberCurrency = (Currency)objects[16];
                ClientBankDetailIbanNo clientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[17];
                Currency clientBankDetailIbanNoCurrency = (Currency)objects[18];
                TermsOfDelivery termsOfDelivery = (TermsOfDelivery)objects[19];
                PackingMarking packingMarking = (PackingMarking)objects[20];
                PackingMarkingPayment packingMarkingPayment = (PackingMarkingPayment)objects[21];
                ClientAgreement clientAgreement = (ClientAgreement)objects[22];
                Agreement agreement = (Agreement)objects[23];
                ProviderPricing providerPricing = (ProviderPricing)objects[24];
                Currency providerPricingCurrency = (Currency)objects[25];
                Pricing pricing = (Pricing)objects[26];
                PriceType priceType = (PriceType)objects[27];
                Organization agreementOrganization = (Organization)objects[28];
                Currency agreementCurrency = (Currency)objects[29];
                Organization organization = (Organization)objects[30];
                Organization supplyOrganizationOrganization = (Organization)objects[31];
                SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[32];

                if (taskToReturn.CustomAgencyServices.Any()) {
                    CustomAgencyService serviceFromList = taskToReturn.CustomAgencyServices.First();

                    if (invoiceDocument != null && !serviceFromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        serviceFromList.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !serviceFromList.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        serviceFromList.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder == null || serviceFromList.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id))) return customAgencyService;

                    supplyOrder.Client = client;
                    supplyOrder.Organization = organization;

                    serviceFromList.SupplyOrders.Add(supplyOrder);
                } else {
                    if (invoiceDocument != null) customAgencyService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        customAgencyService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = supplyOrganizationOrganization;
                    }

                    if (supplyOrder != null) {
                        if (client != null) {
                            if (clientBankDetails != null) {
                                if (clientBankDetailAccountNumber != null) {
                                    clientBankDetailAccountNumber.Currency = clientBankDetailAccountNumberCurrency;

                                    clientBankDetails.AccountNumber = clientBankDetailAccountNumber;
                                }

                                if (clientBankDetailIbanNo != null) {
                                    clientBankDetailIbanNo.Currency = clientBankDetailIbanNoCurrency;

                                    clientBankDetails.ClientBankDetailIbanNo = clientBankDetailIbanNo;
                                }
                            }

                            if (clientAgreement != null) {
                                if (providerPricing != null) {
                                    if (pricing != null) pricing.PriceType = priceType;

                                    providerPricing.Currency = providerPricingCurrency;
                                    providerPricing.Pricing = pricing;
                                }

                                agreement.Organization = agreementOrganization;
                                agreement.Currency = agreementCurrency;
                                agreement.ProviderPricing = providerPricing;

                                clientAgreement.Agreement = agreement;

                                client.ClientAgreements.Add(clientAgreement);
                            }

                            client.Region = region;
                            client.RegionCode = regionCode;
                            client.Country = country;
                            client.ClientBankDetails = clientBankDetails;
                            client.TermsOfDelivery = termsOfDelivery;
                            client.PackingMarking = packingMarking;
                            client.PackingMarkingPayment = packingMarkingPayment;
                        }

                        supplyOrder.Client = client;
                        supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                        supplyOrder.Organization = organization;

                        customAgencyService.SupplyOrders.Add(supplyOrder);
                    }

                    customAgencyService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    customAgencyService.User = user;
                    customAgencyService.CustomAgencyOrganization = customAgencyOrganization;

                    taskToReturn.CustomAgencyServices.Add(customAgencyService);
                }

                return customAgencyService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [CustomAgencyService] " +
                "LEFT JOIN [User] AS [CustomAgencyServiceUser] " +
                "ON [CustomAgencyServiceUser].ID = [CustomAgencyService].UserID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [CustomAgencyService].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrganization] AS [CustomAgencyOrganization] " +
                "ON [CustomAgencyOrganization].ID = [CustomAgencyService].CustomAgencyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [CustomAgencyService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].CustomAgencyServiceID = [CustomAgencyService].ID " +
                "AND [InvoiceDocument].Deleted = 0 " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].CustomAgencyServiceID = [CustomAgencyService].ID " +
                "AND [ServiceDetailItem].Deleted = 0 " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].CustomAgencyServiceID = [CustomAgencyService].ID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [SupplyOrder].ClientID " +
                "LEFT JOIN [Region] " +
                "ON [Region].ID = [Client].RegionID " +
                "LEFT JOIN [RegionCode] " +
                "ON [RegionCode].ID = [Client].RegionCodeID " +
                "LEFT JOIN [Country] " +
                "ON [Country].ID = [Client].CountryID " +
                "LEFT JOIN [ClientBankDetails] " +
                "ON [ClientBankDetails].ID = [Client].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] " +
                "ON [ClientBankDetailAccountNumber].ID = [ClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailAccountNumberCurrency] " +
                "ON [ClientBankDetailAccountNumberCurrency].ID = [ClientBankDetailAccountNumber].CurrencyID " +
                "AND [ClientBankDetailAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] " +
                "ON [ClientBankDetailIbanNo].ID = [ClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailIbanNoCurrency] " +
                "ON [ClientBankDetailIbanNoCurrency].ID = [ClientBankDetailIbanNo].CurrencyID " +
                "AND [ClientBankDetailIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] " +
                "ON [TermsOfDelivery].ID = [Client].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] " +
                "ON [PackingMarking].ID = [Client].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] " +
                "ON [PackingMarkingPayment].ID = [Client].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ClientID = [Client].ID " +
                "AND [ClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] " +
                "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProviderPricing].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] " +
                "ON [ProviderPricing].BasePricingID = [Pricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [PriceType] " +
                "ON [Pricing].PriceTypeID = [PriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
                "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
                "AND [AgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [SupplyOrganizationOrganization] " +
                "ON [SupplyOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [SupplyOrganizationOrganization].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrderNumber] " +
                "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
                "WHERE [CustomAgencyService].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.CustomAgencyService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.PlaneDeliveryService))) {
            Type[] includesTypes = {
                typeof(PlaneDeliveryService),
                typeof(User),
                typeof(SupplyPaymentTask),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency),
                typeof(InvoiceDocument),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization),
                typeof(Organization),
                typeof(SupplyOrderNumber)
            };

            Func<object[], PlaneDeliveryService> includesMapper = objects => {
                PlaneDeliveryService planeDeliveryService = (PlaneDeliveryService)objects[0];
                User user = (User)objects[1];
                //SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
                SupplyOrganization planeDeliveryOrganization = (SupplyOrganization)objects[3];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[4];
                Currency currency = (Currency)objects[5];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[7];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[8];
                SupplyOrder supplyOrder = (SupplyOrder)objects[9];
                Client client = (Client)objects[10];
                Region region = (Region)objects[11];
                RegionCode regionCode = (RegionCode)objects[12];
                Country country = (Country)objects[13];
                ClientBankDetails clientBankDetails = (ClientBankDetails)objects[14];
                ClientBankDetailAccountNumber clientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[15];
                Currency clientBankDetailAccountNumberCurrency = (Currency)objects[16];
                ClientBankDetailIbanNo clientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[17];
                Currency clientBankDetailIbanNoCurrency = (Currency)objects[18];
                TermsOfDelivery termsOfDelivery = (TermsOfDelivery)objects[19];
                PackingMarking packingMarking = (PackingMarking)objects[20];
                PackingMarkingPayment packingMarkingPayment = (PackingMarkingPayment)objects[21];
                ClientAgreement clientAgreement = (ClientAgreement)objects[22];
                Agreement agreement = (Agreement)objects[23];
                ProviderPricing providerPricing = (ProviderPricing)objects[24];
                Currency providerPricingCurrency = (Currency)objects[25];
                Pricing pricing = (Pricing)objects[26];
                PriceType priceType = (PriceType)objects[27];
                Organization agreementOrganization = (Organization)objects[28];
                Currency agreementCurrency = (Currency)objects[29];
                Organization organization = (Organization)objects[30];
                Organization supplyOrganizationOrganization = (Organization)objects[31];
                SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[32];

                if (taskToReturn.PlaneDeliveryServices.Any()) {
                    PlaneDeliveryService serviceFromList = taskToReturn.PlaneDeliveryServices.First();

                    if (invoiceDocument != null && !serviceFromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        serviceFromList.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !serviceFromList.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        serviceFromList.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder == null || serviceFromList.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id))) return planeDeliveryService;

                    supplyOrder.Client = client;
                    supplyOrder.Organization = organization;

                    serviceFromList.SupplyOrders.Add(supplyOrder);
                } else {
                    if (invoiceDocument != null) planeDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        planeDeliveryService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = supplyOrganizationOrganization;
                    }

                    if (supplyOrder != null) {
                        if (client != null) {
                            if (clientBankDetails != null) {
                                if (clientBankDetailAccountNumber != null) {
                                    clientBankDetailAccountNumber.Currency = clientBankDetailAccountNumberCurrency;

                                    clientBankDetails.AccountNumber = clientBankDetailAccountNumber;
                                }

                                if (clientBankDetailIbanNo != null) {
                                    clientBankDetailIbanNo.Currency = clientBankDetailIbanNoCurrency;

                                    clientBankDetails.ClientBankDetailIbanNo = clientBankDetailIbanNo;
                                }
                            }

                            if (clientAgreement != null) {
                                if (providerPricing != null) {
                                    if (pricing != null) pricing.PriceType = priceType;

                                    providerPricing.Currency = providerPricingCurrency;
                                    providerPricing.Pricing = pricing;
                                }

                                agreement.Organization = agreementOrganization;
                                agreement.Currency = agreementCurrency;
                                agreement.ProviderPricing = providerPricing;

                                clientAgreement.Agreement = agreement;

                                client.ClientAgreements.Add(clientAgreement);
                            }

                            client.Region = region;
                            client.RegionCode = regionCode;
                            client.Country = country;
                            client.ClientBankDetails = clientBankDetails;
                            client.TermsOfDelivery = termsOfDelivery;
                            client.PackingMarking = packingMarking;
                            client.PackingMarkingPayment = packingMarkingPayment;
                        }

                        supplyOrder.Client = client;
                        supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                        supplyOrder.Organization = organization;

                        planeDeliveryService.SupplyOrders.Add(supplyOrder);
                    }

                    planeDeliveryService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    planeDeliveryService.User = user;
                    planeDeliveryService.PlaneDeliveryOrganization = planeDeliveryOrganization;

                    taskToReturn.PlaneDeliveryServices.Add(planeDeliveryService);
                }

                return planeDeliveryService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [PlaneDeliveryService] " +
                "LEFT JOIN [User] AS [PlaneDeliveryServiceUser] " +
                "ON [PlaneDeliveryServiceUser].ID = [PlaneDeliveryService].UserID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [PlaneDeliveryService].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrganization] AS [PlaneDeliveryOrganization] " +
                "ON [PlaneDeliveryOrganization].ID = [PlaneDeliveryService].PlaneDeliveryOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PlaneDeliveryService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
                "AND [InvoiceDocument].Deleted = 0 " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
                "AND [ServiceDetailItem].Deleted = 0 " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [SupplyOrder].ClientID " +
                "LEFT JOIN [Region] " +
                "ON [Region].ID = [Client].RegionID " +
                "LEFT JOIN [RegionCode] " +
                "ON [RegionCode].ID = [Client].RegionCodeID " +
                "LEFT JOIN [Country] " +
                "ON [Country].ID = [Client].CountryID " +
                "LEFT JOIN [ClientBankDetails] " +
                "ON [ClientBankDetails].ID = [Client].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] " +
                "ON [ClientBankDetailAccountNumber].ID = [ClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailAccountNumberCurrency] " +
                "ON [ClientBankDetailAccountNumberCurrency].ID = [ClientBankDetailAccountNumber].CurrencyID " +
                "AND [ClientBankDetailAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] " +
                "ON [ClientBankDetailIbanNo].ID = [ClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailIbanNoCurrency] " +
                "ON [ClientBankDetailIbanNoCurrency].ID = [ClientBankDetailIbanNo].CurrencyID " +
                "AND [ClientBankDetailIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] " +
                "ON [TermsOfDelivery].ID = [Client].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] " +
                "ON [PackingMarking].ID = [Client].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] " +
                "ON [PackingMarkingPayment].ID = [Client].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ClientID = [Client].ID " +
                "AND [ClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] " +
                "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProviderPricing].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] " +
                "ON [ProviderPricing].BasePricingID = [Pricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [PriceType] " +
                "ON [Pricing].PriceTypeID = [PriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
                "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
                "AND [AgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [SupplyOrganizationOrganization] " +
                "ON [SupplyOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [SupplyOrganizationOrganization].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrderNumber] " +
                "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
                "WHERE [PlaneDeliveryService].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.PlaneDeliveryService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.VehicleDeliveryService))) {
            Type[] includesTypes = {
                typeof(VehicleDeliveryService),
                typeof(User),
                typeof(SupplyPaymentTask),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency),
                typeof(InvoiceDocument),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization),
                typeof(Organization),
                typeof(SupplyOrderNumber)
            };

            Func<object[], VehicleDeliveryService> includesMapper = objects => {
                VehicleDeliveryService vehicleDeliveryService = (VehicleDeliveryService)objects[0];
                User user = (User)objects[1];
                //SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
                SupplyOrganization vehicleDeliveryOrganization = (SupplyOrganization)objects[3];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[4];
                Currency currency = (Currency)objects[5];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[7];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[8];
                SupplyOrder supplyOrder = (SupplyOrder)objects[9];
                Client client = (Client)objects[10];
                Region region = (Region)objects[11];
                RegionCode regionCode = (RegionCode)objects[12];
                Country country = (Country)objects[13];
                ClientBankDetails clientBankDetails = (ClientBankDetails)objects[14];
                ClientBankDetailAccountNumber clientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[15];
                Currency clientBankDetailAccountNumberCurrency = (Currency)objects[16];
                ClientBankDetailIbanNo clientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[17];
                Currency clientBankDetailIbanNoCurrency = (Currency)objects[18];
                TermsOfDelivery termsOfDelivery = (TermsOfDelivery)objects[19];
                PackingMarking packingMarking = (PackingMarking)objects[20];
                PackingMarkingPayment packingMarkingPayment = (PackingMarkingPayment)objects[21];
                ClientAgreement clientAgreement = (ClientAgreement)objects[22];
                Agreement agreement = (Agreement)objects[23];
                ProviderPricing providerPricing = (ProviderPricing)objects[24];
                Currency providerPricingCurrency = (Currency)objects[25];
                Pricing pricing = (Pricing)objects[26];
                PriceType priceType = (PriceType)objects[27];
                Organization agreementOrganization = (Organization)objects[28];
                Currency agreementCurrency = (Currency)objects[29];
                Organization organization = (Organization)objects[30];
                Organization supplyOrganizationOrganization = (Organization)objects[31];
                SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[32];

                if (taskToReturn.VehicleDeliveryServices.Any()) {
                    VehicleDeliveryService serviceFromList = taskToReturn.VehicleDeliveryServices.First();

                    if (invoiceDocument != null && !serviceFromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        serviceFromList.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null && !serviceFromList.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        serviceFromList.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrder == null || serviceFromList.SupplyOrders.Any(o => o.Id.Equals(supplyOrder.Id))) return vehicleDeliveryService;

                    supplyOrder.Client = client;
                    supplyOrder.Organization = organization;

                    serviceFromList.SupplyOrders.Add(supplyOrder);
                } else {
                    if (invoiceDocument != null) vehicleDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        vehicleDeliveryService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = supplyOrganizationOrganization;
                    }

                    if (supplyOrder != null) {
                        if (client != null) {
                            if (clientBankDetails != null) {
                                if (clientBankDetailAccountNumber != null) {
                                    clientBankDetailAccountNumber.Currency = clientBankDetailAccountNumberCurrency;

                                    clientBankDetails.AccountNumber = clientBankDetailAccountNumber;
                                }

                                if (clientBankDetailIbanNo != null) {
                                    clientBankDetailIbanNo.Currency = clientBankDetailIbanNoCurrency;

                                    clientBankDetails.ClientBankDetailIbanNo = clientBankDetailIbanNo;
                                }
                            }

                            if (clientAgreement != null) {
                                if (providerPricing != null) {
                                    if (pricing != null) pricing.PriceType = priceType;

                                    providerPricing.Currency = providerPricingCurrency;
                                    providerPricing.Pricing = pricing;
                                }

                                agreement.Organization = agreementOrganization;
                                agreement.Currency = agreementCurrency;
                                agreement.ProviderPricing = providerPricing;

                                clientAgreement.Agreement = agreement;

                                client.ClientAgreements.Add(clientAgreement);
                            }

                            client.Region = region;
                            client.RegionCode = regionCode;
                            client.Country = country;
                            client.ClientBankDetails = clientBankDetails;
                            client.TermsOfDelivery = termsOfDelivery;
                            client.PackingMarking = packingMarking;
                            client.PackingMarkingPayment = packingMarkingPayment;
                        }

                        supplyOrder.Client = client;
                        supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                        supplyOrder.Organization = organization;

                        vehicleDeliveryService.SupplyOrders.Add(supplyOrder);
                    }

                    vehicleDeliveryService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    vehicleDeliveryService.User = user;
                    vehicleDeliveryService.VehicleDeliveryOrganization = vehicleDeliveryOrganization;

                    taskToReturn.VehicleDeliveryServices.Add(vehicleDeliveryService);
                }

                return vehicleDeliveryService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [VehicleDeliveryService] " +
                "LEFT JOIN [User] AS [VehicleDeliveryServiceUser] " +
                "ON [VehicleDeliveryServiceUser].ID = [VehicleDeliveryService].UserID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [VehicleDeliveryService].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrganization] AS [VehicleDeliveryOrganization] " +
                "ON [VehicleDeliveryOrganization].ID = [VehicleDeliveryService].VehicleDeliveryOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [VehicleDeliveryService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
                "AND [InvoiceDocument].Deleted = 0 " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
                "AND [ServiceDetailItem].Deleted = 0 " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [SupplyOrder].ClientID " +
                "LEFT JOIN [Region] " +
                "ON [Region].ID = [Client].RegionID " +
                "LEFT JOIN [RegionCode] " +
                "ON [RegionCode].ID = [Client].RegionCodeID " +
                "LEFT JOIN [Country] " +
                "ON [Country].ID = [Client].CountryID " +
                "LEFT JOIN [ClientBankDetails] " +
                "ON [ClientBankDetails].ID = [Client].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] " +
                "ON [ClientBankDetailAccountNumber].ID = [ClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailAccountNumberCurrency] " +
                "ON [ClientBankDetailAccountNumberCurrency].ID = [ClientBankDetailAccountNumber].CurrencyID " +
                "AND [ClientBankDetailAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] " +
                "ON [ClientBankDetailIbanNo].ID = [ClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailIbanNoCurrency] " +
                "ON [ClientBankDetailIbanNoCurrency].ID = [ClientBankDetailIbanNo].CurrencyID " +
                "AND [ClientBankDetailIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] " +
                "ON [TermsOfDelivery].ID = [Client].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] " +
                "ON [PackingMarking].ID = [Client].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] " +
                "ON [PackingMarkingPayment].ID = [Client].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ClientID = [Client].ID " +
                "AND [ClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] " +
                "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProviderPricing].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] " +
                "ON [ProviderPricing].BasePricingID = [Pricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [PriceType] " +
                "ON [Pricing].PriceTypeID = [PriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
                "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
                "AND [AgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [SupplyOrganizationOrganization] " +
                "ON [SupplyOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [SupplyOrganizationOrganization].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrderNumber] " +
                "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
                "WHERE [VehicleDeliveryService].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.VehicleDeliveryService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.ConsumablesOrder))) {
            Type[] includesTypes = {
                typeof(ConsumablesOrder),
                typeof(User),
                typeof(SupplyPaymentTask),
                typeof(ConsumablesOrderItem),
                typeof(ConsumableProductCategory),
                typeof(ConsumableProduct),
                typeof(SupplyOrganization),
                typeof(ConsumablesStorage),
                typeof(PaymentCostMovementOperation),
                typeof(PaymentCostMovement),
                typeof(MeasureUnit),
                typeof(ConsumablesOrderDocument),
                typeof(SupplyOrganizationAgreement),
                typeof(Organization),
                typeof(Currency)
            };

            Func<object[], ConsumablesOrder> includesMapper = objects => {
                ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[0];
                User user = (User)objects[1];
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
                ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[3];
                ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[4];
                ConsumableProduct consumableProduct = (ConsumableProduct)objects[5];
                SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[6];
                ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[7];
                PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[8];
                PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[9];
                MeasureUnit measureUnit = (MeasureUnit)objects[10];
                ConsumablesOrderDocument consumablesOrderDocument = (ConsumablesOrderDocument)objects[11];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[12];
                Organization supplyOrganizationOrganization = (Organization)objects[13];
                Currency supplyOrganizationAgreementCurrency = (Currency)objects[14];

                if (taskToReturn.ConsumablesOrder != null) {
                    if (consumablesOrderItem != null && !taskToReturn.ConsumablesOrder.ConsumablesOrderItems.Any(i => i.Id.Equals(consumablesOrderItem.Id))) {
                        if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                        if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                            supplyOrganizationAgreement.Organization = supplyOrganizationOrganization;
                        }

                        consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                        consumablesOrderItem.ConsumableProduct = consumableProduct;
                        consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                        consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                        consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                        taskToReturn.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                        consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                        taskToReturn.ConsumablesOrder.TotalAmount =
                            Math.Round(taskToReturn.ConsumablesOrder.TotalAmount + consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                        taskToReturn.ConsumablesOrder.TotalAmountWithoutVAT =
                            Math.Round(taskToReturn.ConsumablesOrder.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);
                    }

                    if (consumablesOrderDocument != null && !taskToReturn.ConsumablesOrder.ConsumablesOrderDocuments.Any(d => d.Id.Equals(consumablesOrderDocument.Id)))
                        taskToReturn.ConsumablesOrder.ConsumablesOrderDocuments.Add(consumablesOrderDocument);
                } else {
                    if (consumablesOrderItem != null) {
                        if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                        if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                        if (supplyOrganizationAgreement != null) {
                            supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                            supplyOrganizationAgreement.Organization = supplyOrganizationOrganization;
                        }

                        consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                        consumablesOrderItem.ConsumableProduct = consumableProduct;
                        consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                        consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                        consumablesOrderItem.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                        consumablesOrderItem.TotalPriceWithVAT = Math.Round(consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                        consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                        consumablesOrder.TotalAmount = Math.Round(consumablesOrder.TotalAmount + consumablesOrderItem.TotalPrice + consumablesOrderItem.VAT, 2);

                        consumablesOrder.TotalAmountWithoutVAT = Math.Round(consumablesOrder.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);
                    }

                    if (consumablesOrderDocument != null) consumablesOrder.ConsumablesOrderDocuments.Add(consumablesOrderDocument);

                    consumablesOrder.User = user;
                    consumablesOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    consumablesOrder.SupplyPaymentTask = supplyPaymentTask;
                    consumablesOrder.ConsumablesStorage = consumablesStorage;
                    consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;

                    taskToReturn.ConsumablesOrder = consumablesOrder;
                }

                return consumablesOrder;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [ConsumablesOrder] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [ConsumablesOrder].UserID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [ConsumablesOrder].SupplyPaymentTaskID " +
                "LEFT JOIN [ConsumablesOrderItem] " +
                "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "AND [ConsumablesOrderItem].Deleted = 0 " +
                "LEFT JOIN (" +
                "SELECT [ConsumableProductCategory].ID " +
                ", [ConsumableProductCategory].[Created] " +
                ", [ConsumableProductCategory].[Deleted] " +
                ", [ConsumableProductCategory].[NetUID] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] " +
                ", [ConsumableProductCategory].[Updated] " +
                "FROM [ConsumableProductCategory] " +
                "LEFT JOIN [ConsumableProductCategoryTranslation] " +
                "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
                "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture" +
                ") AS [ConsumableProductCategory] " +
                "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
                "LEFT JOIN (" +
                "SELECT [ConsumableProduct].ID " +
                ", [ConsumableProduct].[ConsumableProductCategoryID] " +
                ", [ConsumableProduct].[Created] " +
                ", [ConsumableProduct].[VendorCode] " +
                ", [ConsumableProduct].[Deleted] " +
                ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
                ", [ConsumableProduct].[NetUID] " +
                ", [ConsumableProduct].[MeasureUnitID] " +
                ", [ConsumableProduct].[Updated] " +
                "FROM [ConsumableProduct] " +
                "LEFT JOIN [ConsumableProductTranslation] " +
                "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture" +
                ") AS [ConsumableProduct] " +
                "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
                "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
                "LEFT JOIN [ConsumablesStorage] " +
                "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
                "LEFT JOIN [PaymentCostMovementOperation] " +
                "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
                "LEFT JOIN (" +
                "SELECT [PaymentCostMovement].ID " +
                ", [PaymentCostMovement].[Created] " +
                ", [PaymentCostMovement].[Deleted] " +
                ", [PaymentCostMovement].[NetUID] " +
                ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] " +
                ", [PaymentCostMovement].[Updated] " +
                "FROM [PaymentCostMovement] " +
                "LEFT JOIN [PaymentCostMovementTranslation] " +
                "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
                "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
                ") AS [PaymentCostMovement] " +
                "ON [PaymentCostMovement].ID = [PaymentCostMovementOperation].PaymentCostMovementID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [ConsumablesOrderDocument] " +
                "ON [ConsumablesOrderDocument].ConsumablesOrderID = [ConsumablesOrder].ID " +
                "AND [ConsumablesOrderDocument].Deleted = 0 " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].SupplyOrganizationID = [ConsumableProductOrganization].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [SupplyOrganizationOrganization] " +
                "ON [SupplyOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [SupplyOrganizationOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [SupplyOrganizationAgreementCurrency] " +
                "ON [SupplyOrganizationAgreementCurrency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [SupplyOrganizationAgreementCurrency].CultureCode = @Culture " +
                "WHERE [ConsumablesOrder].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.ConsumablesOrder)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.MergedService))) {
            Type[] includesTypes = {
                typeof(MergedService),
                typeof(User),
                typeof(SupplyPaymentTask),
                typeof(SupplyOrganization),
                typeof(SupplyOrganizationAgreement),
                typeof(Currency),
                typeof(InvoiceDocument),
                typeof(ServiceDetailItem),
                typeof(ServiceDetailItemKey),
                typeof(SupplyOrder),
                typeof(Client),
                typeof(Region),
                typeof(RegionCode),
                typeof(Country),
                typeof(ClientBankDetails),
                typeof(ClientBankDetailAccountNumber),
                typeof(Currency),
                typeof(ClientBankDetailIbanNo),
                typeof(Currency),
                typeof(TermsOfDelivery),
                typeof(PackingMarking),
                typeof(PackingMarkingPayment),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(ProviderPricing),
                typeof(Currency),
                typeof(Pricing),
                typeof(PriceType),
                typeof(Organization),
                typeof(Currency),
                typeof(Organization),
                typeof(Organization),
                typeof(SupplyOrderNumber),
                typeof(SupplyOrderUkraine),
                typeof(User),
                typeof(Organization),
                typeof(Client),
                typeof(RegionCode),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(Organization),
                typeof(Currency)
            };

            Func<object[], MergedService> includesMapper = objects => {
                MergedService mergedService = (MergedService)objects[0];
                User user = (User)objects[1];
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
                SupplyOrganization supplyOrganization = (SupplyOrganization)objects[3];
                SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[4];
                Currency currency = (Currency)objects[5];
                InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];
                ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[7];
                ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[8];

                SupplyOrder supplyOrder = (SupplyOrder)objects[9];
                Client client = (Client)objects[10];
                Region region = (Region)objects[11];
                RegionCode regionCode = (RegionCode)objects[12];
                Country country = (Country)objects[13];
                ClientBankDetails clientBankDetails = (ClientBankDetails)objects[14];
                ClientBankDetailAccountNumber clientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[15];
                Currency clientBankDetailAccountNumberCurrency = (Currency)objects[16];
                ClientBankDetailIbanNo clientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[17];
                Currency clientBankDetailIbanNoCurrency = (Currency)objects[18];
                TermsOfDelivery termsOfDelivery = (TermsOfDelivery)objects[19];
                PackingMarking packingMarking = (PackingMarking)objects[20];
                PackingMarkingPayment packingMarkingPayment = (PackingMarkingPayment)objects[21];
                ClientAgreement clientAgreement = (ClientAgreement)objects[22];
                Agreement agreement = (Agreement)objects[23];
                ProviderPricing providerPricing = (ProviderPricing)objects[24];
                Currency providerPricingCurrency = (Currency)objects[25];
                Pricing pricing = (Pricing)objects[26];
                PriceType priceType = (PriceType)objects[27];
                Organization agreementOrganization = (Organization)objects[28];
                Currency agreementCurrency = (Currency)objects[29];
                Organization organization = (Organization)objects[30];
                Organization supplyOrganizationOrganization = (Organization)objects[31];
                SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[32];

                SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[33];
                User responsible = (User)objects[34];
                Organization ukOrganization = (Organization)objects[35];
                Client supplier = (Client)objects[36];
                RegionCode supplierRegionCode = (RegionCode)objects[37];
                ClientAgreement supplierClientAgreement = (ClientAgreement)objects[38];
                Agreement supplierAgreement = (Agreement)objects[39];
                Organization supplierAgreementOrganization = (Organization)objects[40];
                Currency supplierAgreementCurrency = (Currency)objects[41];

                if (taskToReturn.MergedServices.Any()) {
                    MergedService serviceFromList = taskToReturn.MergedServices.First();

                    if (invoiceDocument != null && !serviceFromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                        serviceFromList.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem == null || serviceFromList.ServiceDetailItems.Any(i => i.Id.Equals(serviceDetailItem.Id))) return mergedService;

                    serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                    serviceFromList.ServiceDetailItems.Add(serviceDetailItem);
                } else {
                    if (invoiceDocument != null) mergedService.InvoiceDocuments.Add(invoiceDocument);

                    if (serviceDetailItem != null) {
                        serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

                        mergedService.ServiceDetailItems.Add(serviceDetailItem);
                    }

                    if (supplyOrganizationAgreement != null) {
                        supplyOrganizationAgreement.Currency = currency;

                        supplyOrganizationAgreement.Organization = supplyOrganizationOrganization;
                    }

                    if (supplyOrderUkraine != null) {
                        supplierAgreement.Organization = supplierAgreementOrganization;
                        supplierAgreement.Currency = supplierAgreementCurrency;

                        supplierClientAgreement.Agreement = supplierAgreement;

                        supplier.RegionCode = supplierRegionCode;

                        supplyOrderUkraine.Supplier = supplier;
                        supplyOrderUkraine.Responsible = responsible;
                        supplyOrderUkraine.Organization = ukOrganization;
                        supplyOrderUkraine.ClientAgreement = supplierClientAgreement;

                        mergedService.SupplyOrderUkraine = supplyOrderUkraine;
                    }

                    if (supplyPaymentTask != null) mergedService.SupplyPaymentTask = supplyPaymentTask;

                    if (supplyOrder != null) {
                        if (client != null) {
                            if (clientBankDetails != null) {
                                if (clientBankDetailAccountNumber != null) {
                                    clientBankDetailAccountNumber.Currency = clientBankDetailAccountNumberCurrency;

                                    clientBankDetails.AccountNumber = clientBankDetailAccountNumber;
                                }

                                if (clientBankDetailIbanNo != null) {
                                    clientBankDetailIbanNo.Currency = clientBankDetailIbanNoCurrency;

                                    clientBankDetails.ClientBankDetailIbanNo = clientBankDetailIbanNo;
                                }
                            }

                            if (clientAgreement != null) {
                                if (providerPricing != null) {
                                    if (pricing != null) pricing.PriceType = priceType;

                                    providerPricing.Currency = providerPricingCurrency;
                                    providerPricing.Pricing = pricing;
                                }

                                agreement.Organization = agreementOrganization;
                                agreement.Currency = agreementCurrency;
                                agreement.ProviderPricing = providerPricing;

                                clientAgreement.Agreement = agreement;

                                client.ClientAgreements.Add(clientAgreement);
                            }

                            client.Region = region;
                            client.RegionCode = regionCode;
                            client.Country = country;
                            client.ClientBankDetails = clientBankDetails;
                            client.TermsOfDelivery = termsOfDelivery;
                            client.PackingMarking = packingMarking;
                            client.PackingMarkingPayment = packingMarkingPayment;
                        }

                        supplyOrder.Client = client;
                        supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                        supplyOrder.Organization = organization;

                        mergedService.SupplyOrder = supplyOrder;
                    }

                    mergedService.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                    mergedService.User = user;
                    mergedService.SupplyOrganization = supplyOrganization;

                    taskToReturn.MergedServices.Add(mergedService);
                }

                return mergedService;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [MergedService] " +
                "LEFT JOIN [User] AS [MergedServiceUser] " +
                "ON [MergedServiceUser].ID = [MergedService].UserID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [MergedService].SupplyPaymentTaskID " +
                "LEFT JOIN [SupplyOrganization] AS [MergedServiceSupplyOrganization] " +
                "ON [MergedServiceSupplyOrganization].ID = [MergedService].SupplyOrganizationID " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [MergedService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].MergedServiceID = [MergedService].ID " +
                "AND [InvoiceDocument].Deleted = 0 " +
                "LEFT JOIN [ServiceDetailItem] " +
                "ON [ServiceDetailItem].MergedServiceID = [MergedService].ID " +
                "AND [ServiceDetailItem].Deleted = 0 " +
                "LEFT JOIN [ServiceDetailItemKey] " +
                "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [MergedService].SupplyOrderID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [SupplyOrder].ClientID " +
                "LEFT JOIN [Region] " +
                "ON [Region].ID = [Client].RegionID " +
                "LEFT JOIN [RegionCode] " +
                "ON [RegionCode].ID = [Client].RegionCodeID " +
                "LEFT JOIN [Country] " +
                "ON [Country].ID = [Client].CountryID " +
                "LEFT JOIN [ClientBankDetails] " +
                "ON [ClientBankDetails].ID = [Client].ClientBankDetailsID " +
                "LEFT JOIN [ClientBankDetailAccountNumber] " +
                "ON [ClientBankDetailAccountNumber].ID = [ClientBankDetails].AccountNumberID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailAccountNumberCurrency] " +
                "ON [ClientBankDetailAccountNumberCurrency].ID = [ClientBankDetailAccountNumber].CurrencyID " +
                "AND [ClientBankDetailAccountNumberCurrency].CultureCode = @Culture " +
                "LEFT JOIN [ClientBankDetailIbanNo] " +
                "ON [ClientBankDetailIbanNo].ID = [ClientBankDetails].ClientBankDetailIbanNoID " +
                "LEFT JOIN [views].[CurrencyView] AS [ClientBankDetailIbanNoCurrency] " +
                "ON [ClientBankDetailIbanNoCurrency].ID = [ClientBankDetailIbanNo].CurrencyID " +
                "AND [ClientBankDetailIbanNoCurrency].CultureCode = @Culture " +
                "LEFT JOIN [TermsOfDelivery] " +
                "ON [TermsOfDelivery].ID = [Client].TermsOfDeliveryID " +
                "LEFT JOIN [PackingMarking] " +
                "ON [PackingMarking].ID = [Client].PackingMarkingID " +
                "LEFT JOIN [PackingMarkingPayment] " +
                "ON [PackingMarkingPayment].ID = [Client].PackingMarkingPaymentID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ClientID = [Client].ID " +
                "AND [ClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [ProviderPricing] " +
                "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [ProFormSupplyOrderClientAgreementProviderPricingCurrency] " +
                "ON [ProFormSupplyOrderClientAgreementProviderPricingCurrency].ID = [ProviderPricing].CurrencyID " +
                "AND [ProFormSupplyOrderClientAgreementProviderPricingCurrency].CultureCode = @Culture " +
                "LEFT JOIN [Pricing] " +
                "ON [ProviderPricing].BasePricingID = [Pricing].ID " +
                "LEFT JOIN ( " +
                "SELECT [PriceType].ID " +
                ",[PriceType].Created " +
                ",[PriceType].Deleted " +
                ",(CASE WHEN [PriceTypeTranslation].[Name] IS NOT NULL THEN [PriceTypeTranslation].[Name] ELSE [PriceType].[Name] END) AS [Name] " +
                ",[PriceType].NetUID " +
                ",[PriceType].Updated " +
                "FROM [PriceType] " +
                "LEFT JOIN [PriceTypeTranslation] " +
                "ON [PriceTypeTranslation].PriceTypeID = [PriceType].ID " +
                "AND [PriceTypeTranslation].CultureCode = @Culture " +
                "AND [PriceTypeTranslation].Deleted = 0 " +
                ") AS [PriceType] " +
                "ON [Pricing].PriceTypeID = [PriceType].ID " +
                "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
                "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
                "AND [AgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
                "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
                "AND [AgreementCurrency].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrder].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [SupplyOrganizationOrganization] " +
                "ON [SupplyOrganizationOrganization].ID = [SupplyOrganizationAgreement].OrganizationID " +
                "AND [SupplyOrganizationOrganization].CultureCode = @Culture " +
                "LEFT JOIN [SupplyOrderNumber] " +
                "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [MergedService].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [SupplyOrderUkraine].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [UkOrganization] " +
                "ON [UkOrganization].ID = [SupplyOrderUkraine].OrganizationID " +
                "AND [UkOrganization].CultureCode = @Culture " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SupplyOrderUkraine].SupplierID " +
                "LEFT JOIN [RegionCode] AS [SupplierRegionCode] " +
                "ON [SupplierRegionCode].ID = [Supplier].RegionCodeID " +
                "LEFT JOIN [ClientAgreement] AS [UkClientAgreement] " +
                "ON [UkClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
                "LEFT JOIN [Agreement] AS [UkClientAgreementAgreement] " +
                "ON [UkClientAgreementAgreement].ID = [UkClientAgreement].AgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [UkClientAgreementAgreementOrganization] " +
                "ON [UkClientAgreementAgreementOrganization].ID = [UkClientAgreementAgreement].OrganizationID " +
                "AND [UkClientAgreementAgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [views].[CurrencyView] AS [UkClientAgreementAgreementCurrency] " +
                "ON [UkClientAgreementAgreementCurrency].ID = [UkClientAgreementAgreement].CurrencyID " +
                "AND [UkClientAgreementAgreementCurrency].CultureCode = @Culture " +
                "WHERE [MergedService].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.MergedService)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (joinServices.Any(s => s.Type.Equals(JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol))) {
            Type[] includesTypes = {
                typeof(SupplyOrderUkrainePaymentDeliveryProtocol),
                typeof(SupplyOrderUkrainePaymentDeliveryProtocolKey),
                typeof(User),
                typeof(SupplyPaymentTask),
                typeof(User),
                typeof(SupplyOrderUkraine),
                typeof(User),
                typeof(Organization),
                typeof(Client),
                typeof(ClientAgreement),
                typeof(Agreement),
                typeof(Currency)
            };

            Func<object[], SupplyOrderUkrainePaymentDeliveryProtocol> includesMapper = objects => {
                SupplyOrderUkrainePaymentDeliveryProtocol protocol = (SupplyOrderUkrainePaymentDeliveryProtocol)objects[0];
                SupplyOrderUkrainePaymentDeliveryProtocolKey protocolKey = (SupplyOrderUkrainePaymentDeliveryProtocolKey)objects[1];
                User protocolUser = (User)objects[2];
                SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[3];
                User supplyPaymentTaskUser = (User)objects[4];
                SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[5];
                User responsible = (User)objects[6];
                Organization organization = (Organization)objects[7];
                Client supplier = (Client)objects[8];
                ClientAgreement clientAgreement = (ClientAgreement)objects[9];
                Agreement agreement = (Agreement)objects[10];
                Currency currency = (Currency)objects[11];

                supplyPaymentTask.User = supplyPaymentTaskUser;

                agreement.Currency = currency;

                clientAgreement.Agreement = agreement;

                supplyOrderUkraine.Supplier = supplier;
                supplyOrderUkraine.Responsible = responsible;
                supplyOrderUkraine.Organization = organization;
                supplyOrderUkraine.ClientAgreement = clientAgreement;

                protocol.User = protocolUser;
                protocol.SupplyPaymentTask = supplyPaymentTask;
                protocol.SupplyOrderUkraine = supplyOrderUkraine;
                protocol.SupplyOrderUkrainePaymentDeliveryProtocolKey = protocolKey;

                taskToReturn.SupplyOrderUkrainePaymentDeliveryProtocols.Add(protocol);

                return protocol;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [SupplyOrderUkrainePaymentDeliveryProtocol] " +
                "LEFT JOIN [SupplyOrderUkrainePaymentDeliveryProtocolKey] " +
                "ON [SupplyOrderUkrainePaymentDeliveryProtocolKey].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyOrderUkrainePaymentDeliveryProtocolKeyID " +
                "LEFT JOIN [User] AS [ProtocolUser] " +
                "ON [ProtocolUser].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].UserID " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyPaymentTaskID " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [SupplyPaymentTask].UserID " +
                "LEFT JOIN [SupplyOrderUkraine] " +
                "ON [SupplyOrderUkraine].ID = [SupplyOrderUkrainePaymentDeliveryProtocol].SupplyOrderUkraineID " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [SupplyOrderUkraine].ResponsibleID " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SupplyOrderUkraine].SupplierID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [ClientAgreement].AgreementID = [Agreement].ID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [Agreement].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "WHERE [SupplyOrderUkrainePaymentDeliveryProtocol].ID IN @Ids",
                includesTypes,
                includesMapper,
                new {
                    Ids = joinServices.Where(s => s.Type.Equals(JoinServiceType.SupplyOrderUkrainePaymentDeliveryProtocol)).Select(s => s.Id),
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        return taskToReturn;
    }

    public List<SupplyPaymentTask> GetAllByIds(IEnumerable<long> ids) {
        return _connection.Query<SupplyPaymentTask, User, SupplyPaymentTask>(
                "SELECT * FROM [SupplyPaymentTask] " +
                "LEFT JOIN [User] " +
                "ON [SupplyPaymentTask].UserID = [User].ID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids",
                (task, user) => {
                    task.User = user;

                    return task;
                },
                new { Ids = ids }
            )
            .ToList();
    }

    public void Update(SupplyPaymentTask supplyPaymentTask) {
        _connection.Execute(
            "UPDATE SupplyPaymentTask " +
            "SET Comment = @Comment, UserID = @UserID, PayToDate = @PayToDate, TaskAssignedTo = @TaskAssignedTo, TaskStatus = @TaskStatus, " +
            "TaskStatusUpdated = @TaskStatusUpdated, NetPrice = @NetPrice, GrossPrice = @GrossPrice, UpdatedById = @UpdatedById, Updated = getutcdate(), IsAccounting = @IsAccounting " +
            "WHERE NetUID = @NetUID",
            supplyPaymentTask
        );
    }

    public void Update(IEnumerable<SupplyPaymentTask> supplyPaymentTasks) {
        _connection.Execute(
            "UPDATE SupplyPaymentTask " +
            "SET Comment = @Comment, UserID = @UserID, PayToDate = @PayToDate, TaskAssignedTo = @TaskAssignedTo, TaskStatus = @TaskStatus, " +
            "TaskStatusUpdated = @TaskStatusUpdated, NetPrice = @NetPrice, GrossPrice = @GrossPrice, Updated = getutcdate(), IsAccounting = @IsAccounting " +
            "WHERE NetUID = @NetUID",
            supplyPaymentTasks
        );
    }

    public void UpdateTaskPrices(SupplyPaymentTask supplyPaymentTask) {
        _connection.Execute(
            "UPDATE [SupplyPaymentTask] " +
            "SET NetPrice = @NetPrice, GrossPrice = @GrossPrice, Updated = GETUTCDATE() " +
            "WHERE [SupplyPaymentTask].ID = @Id",
            supplyPaymentTask
        );
    }

    public void SetTaskAvailableForPayment(SupplyPaymentTask supplyPaymentTask) {
        _connection.Execute(
            "UPDATE [SupplyPaymentTask] " +
            "SET IsAvailableForPayment = @IsAvailableForPayment, Updated = GETUTCDATE() " +
            "WHERE [SupplyPaymentTask].ID = @Id",
            supplyPaymentTask
        );
    }

    public void UpdateTaskStatus(SupplyPaymentTask supplyPaymentTask) {
        _connection.Execute(
            "UPDATE SupplyPaymentTask SET TaskStatus = @TaskStatus, TaskStatusUpdated = getutcdate() WHERE NetUID = @NetUID",
            supplyPaymentTask
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SupplyPaymentTask] " +
            "SET Deleted = 1 " +
            "WHERE ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RemoveById(long taskId, long deletedById) {
        _connection.Execute(
            "UPDATE [SupplyPaymentTask] " +
            "SET Deleted = 1, Updated = GETUTCDATE(), DeletedById = @DeletedById " +
            "WHERE ID = @TaskId",
            new { TaskId = taskId, DeletedById = deletedById }
        );
    }
}