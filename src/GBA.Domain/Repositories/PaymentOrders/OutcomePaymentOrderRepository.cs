using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Common.Extensions;
using GBA.Domain.Entities;
using GBA.Domain.Entities.AccountingDocumentNames;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Clients.OrganizationClients;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.TotalDashboards;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Domain.Repositories.PaymentOrders;

public sealed class OutcomePaymentOrderRepository : IOutcomePaymentOrderRepository {
    private readonly IDbConnection _connection;

    private readonly string FROM_ONE_C = "Ввід боргів з 1С";

    public OutcomePaymentOrderRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(OutcomePaymentOrder outcomePaymentOrder) {
        return _connection.Query<long>(
                "INSERT INTO [OutcomePaymentOrder] " +
                "(Number, Comment, FromDate, Amount, Account, UserId, OrganizationId, PaymentCurrencyRegisterId, IsUnderReport, IsUnderReportDone, " +
                "ColleagueId, AdvanceNumber, ConsumableProductOrganizationId, ExchangeRate, AfterExchangeAmount, ClientAgreementId, " +
                "SupplyOrderPolandPaymentDeliveryProtocolId, SupplyOrganizationAgreementId, VAT, VatPercent, OrganizationClientId, OrganizationClientAgreementId, " +
                "TaxFreeId, SadId, Updated, OperationType, ClientID, [CustomNumber], [PaymentPurpose], [EuroAmount], [ArrivalNumber], [IsManagementAccounting], " +
                "[IsAccounting]) " +
                "VALUES (@Number, @Comment, @FromDate, @Amount, @Account, @UserId, @OrganizationId, @PaymentCurrencyRegisterId, @IsUnderReport, @IsUnderReportDone, " +
                "@ColleagueId, @AdvanceNumber, @ConsumableProductOrganizationId, @ExchangeRate, @AfterExchangeAmount, @ClientAgreementId, " +
                "@SupplyOrderPolandPaymentDeliveryProtocolId, @SupplyOrganizationAgreementId, @VAT, @VatPercent, @OrganizationClientId, @OrganizationClientAgreementId, " +
                "@TaxFreeId, @SadId, getutcdate(), @OperationType, @ClientId, @CustomNumber, @PaymentPurpose, @EuroAmount, @ArrivalNumber, @IsManagementAccounting, " +
                "@IsAccounting); " +
                "SELECT SCOPE_IDENTITY()",
                outcomePaymentOrder
            )
            .Single();
    }

    public void Update(OutcomePaymentOrder outcomePaymentOrder) {
        _connection.Execute(
            "UPDATE [OutcomePaymentOrder] " +
            "SET Comment = @Comment, FromDate = @FromDate, Account = @Account, UserId = @UserId, ColleagueId = @ColleagueId, " +
            "OrganizationId = @OrganizationId, IsUnderReport = @IsUnderReport, IsUnderReportDone = @IsUnderReportDone, " +
            "ConsumableProductOrganizationId = @ConsumableProductOrganizationId, ClientAgreementId = @ClientAgreementId, " +
            "SupplyOrderPolandPaymentDeliveryProtocolId = @SupplyOrderPolandPaymentDeliveryProtocolId, Updated = getutcdate(), " +
            "[CustomNumber] = @CustomNumber, [PaymentPurpose] = @PaymentPurpose, [IsCanceled] = @IsCanceled " +
            "WHERE [OutcomePaymentOrder].ID = @Id",
            outcomePaymentOrder
        );
    }

    public void SetIsUnderReportDoneById(long id, bool isUnderReportDone) {
        _connection.Execute(
            "UPDATE [OutcomePaymentOrder] " +
            "SET IsUnderReportDone = @IsUnderReportDone, Updated = getutcdate() " +
            "WHERE [OutcomePaymentOrder].ID = @Id",
            new { Id = id, IsUnderReportDone = isUnderReportDone }
        );
    }

    public OutcomePaymentOrder GetByIdWithCalculatedAmount(long id) {
        return _connection.Query<OutcomePaymentOrder, AssignedPaymentOrder, OutcomePaymentOrder>(
                "SELECT [OutcomePaymentOrder].ID " +
                ", [OutcomePaymentOrder].[Account]" +
                ",ROUND([OutcomePaymentOrder].Amount " +
                "+ " +
                "ISNULL(( " +
                "SELECT SUM([AssignedOutcome].Amount) " +
                "FROM [AssignedPaymentOrder] " +
                "LEFT JOIN [OutcomePaymentOrder] AS [AssignedOutcome] " +
                "ON [AssignedOutcome].ID = [AssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
                "WHERE [AssignedPaymentOrder].RootOutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "AND [AssignedPaymentOrder].Deleted = 0" +
                "), 0) " +
                "- " +
                "ISNULL(( " +
                "SELECT SUM([AssignedIncome].Amount) " +
                "FROM [AssignedPaymentOrder] " +
                "LEFT JOIN [IncomePaymentOrder] AS [AssignedIncome] " +
                "ON [AssignedIncome].ID = [AssignedPaymentOrder].AssignedIncomePaymentOrderID " +
                "WHERE [AssignedPaymentOrder].RootOutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "AND [AssignedPaymentOrder].Deleted = 0" +
                "), 0) " +
                ", 2) AS [Amount] " +
                ", [OutcomePaymentOrder].[Comment]" +
                ", [OutcomePaymentOrder].[Created]" +
                ", [OutcomePaymentOrder].[Deleted]" +
                ", [OutcomePaymentOrder].[FromDate]" +
                ", [OutcomePaymentOrder].[NetUID]" +
                ", [OutcomePaymentOrder].[Number]" +
                ", [OutcomePaymentOrder].[OrganizationID]" +
                ", [OutcomePaymentOrder].[PaymentCurrencyRegisterID]" +
                ", [OutcomePaymentOrder].[Updated]" +
                ", [OutcomePaymentOrder].[UserID]" +
                ", [OutcomePaymentOrder].[IsUnderReport]" +
                ", [OutcomePaymentOrder].[ColleagueID]" +
                ", [AssignedPaymentOrder].* " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [AssignedPaymentOrder] " +
                "ON [AssignedPaymentOrder].AssignedOutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                "WHERE [OutcomePaymentOrder].ID = @Id",
                (outcome, assignedOrder) => {
                    outcome.RootAssignedPaymentOrder = assignedOrder;

                    return outcome;
                },
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public OutcomePaymentOrder GetById(long id) {
        OutcomePaymentOrder toReturn = null;

        string sqlExpression =
            "SELECT * " +
            "FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OutcomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
            "LEFT JOIN (" +
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
            "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
            "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
            "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
            "ON [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
            "LEFT JOIN [ConsumablesOrder] " +
            "ON [ConsumablesOrder].ID = [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID " +
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
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
            "LEFT JOIN [User] AS [ConsumablesOrderUser] " +
            "ON [ConsumablesOrderUser].ID = [ConsumablesOrder].UserID " +
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
            "LEFT JOIN [SupplyOrganization] AS [OutcomeConsumableProductOrganization] " +
            "ON [OutcomeConsumableProductOrganization].ID = [OutcomePaymentOrder].ConsumableProductOrganizationID " +
            "LEFT JOIN [Client] AS [OutcomeClient] " +
            "ON [OutcomeClient].ID = [OutcomePaymentOrder].ClientID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [OutcomePaymentOrder].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [OutcomeAgreement] " +
            "ON [OutcomeAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [OutcomeAgreementCurrency] " +
            "ON [OutcomeAgreementCurrency].ID = [OutcomeAgreement].CurrencyID " +
            "AND [OutcomeAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [ConsumablesAgreement] " +
            "ON [ConsumablesAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [ConsumablesAgreementCurrency] " +
            "ON [ConsumablesAgreementCurrency].ID = [ConsumablesAgreement].CurrencyID " +
            "AND [ConsumablesAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [OrganizationClient] " +
            "ON [OrganizationClient].ID = [OutcomePaymentOrder].OrganizationClientID " +
            "LEFT JOIN [OrganizationClientAgreement] " +
            "ON [OrganizationClientAgreement].ID = [OutcomePaymentOrder].OrganizationClientAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [OrganizationClientAgreementCurrency] " +
            "ON [OrganizationClientAgreementCurrency].ID = [OrganizationClientAgreement].CurrencyID " +
            "AND [OrganizationClientAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [TaxFree] " +
            "ON [TaxFree].ID = [OutcomePaymentOrder].TaxFreeID " +
            "LEFT JOIN [Sad] " +
            "ON [Sad].ID = [OutcomePaymentOrder].SadID " +
            "WHERE [OutcomePaymentOrder].ID = @Id";

        Type[] types = {
            typeof(OutcomePaymentOrder),
            typeof(Organization),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(OutcomePaymentOrderConsumablesOrder),
            typeof(ConsumablesOrder),
            typeof(ConsumablesOrderItem),
            typeof(ConsumableProductCategory),
            typeof(ConsumableProduct),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(User),
            typeof(PaymentCostMovementOperation),
            typeof(PaymentCostMovement),
            typeof(MeasureUnit),
            typeof(SupplyOrganization),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(Agreement),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(OrganizationClient),
            typeof(OrganizationClientAgreement),
            typeof(Currency),
            typeof(TaxFree),
            typeof(Sad)
        };

        Func<object[], OutcomePaymentOrder> mapper = objects => {
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[0];
            Organization organization = (Organization)objects[1];
            User user = (User)objects[2];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[3];
            PaymentMovement paymentMovement = (PaymentMovement)objects[4];
            PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency currency = (Currency)objects[6];
            PaymentRegister paymentRegister = (PaymentRegister)objects[7];
            Organization paymentRegisterOrganization = (Organization)objects[8];
            OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[9];
            ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[10];
            ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[11];
            ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[12];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[13];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[14];
            User colleague = (User)objects[15];
            User consumablesOrderUser = (User)objects[16];
            PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[17];
            PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[18];
            MeasureUnit measureUnit = (MeasureUnit)objects[19];
            SupplyOrganization outcomeConsumableProductOrganization = (SupplyOrganization)objects[20];
            Client outcomesClient = (Client)objects[21];
            ClientAgreement clientAgreement = (ClientAgreement)objects[22];
            Client client = (Client)objects[23];
            Agreement agreement = (Agreement)objects[24];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[25];
            Currency supplyOrganizationAgreementCurrency = (Currency)objects[26];
            SupplyOrganizationAgreement consumablesOrderSupplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[27];
            Currency consumablesOrderSupplyOrganizationAgreementCurrency = (Currency)objects[28];
            OrganizationClient organizationClient = (OrganizationClient)objects[29];
            OrganizationClientAgreement organizationClientAgreement = (OrganizationClientAgreement)objects[30];
            Currency organizationClientAgreementCurrency = (Currency)objects[31];
            TaxFree taxFree = (TaxFree)objects[32];
            Sad sad = (Sad)objects[33];

            if (toReturn == null) {
                if (consumablesOrder != null && consumablesOrderItem != null) {
                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                    if (consumablesOrderSupplyOrganizationAgreement != null)
                        consumablesOrderSupplyOrganizationAgreement.Currency = consumablesOrderSupplyOrganizationAgreementCurrency;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.SupplyOrganizationAgreement = consumablesOrderSupplyOrganizationAgreement;

                    consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                    consumablesOrder.User = consumablesOrderUser;
                    consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrder.SupplyOrganizationAgreement = consumablesOrderSupplyOrganizationAgreement;

                    outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                    outcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                }

                if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                if (clientAgreement != null) {
                    clientAgreement.Client = client;
                    clientAgreement.Agreement = agreement;
                }

                if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                if (organizationClientAgreement != null) organizationClientAgreement.Currency = organizationClientAgreementCurrency;

                paymentRegister.Organization = paymentRegisterOrganization;

                paymentCurrencyRegister.PaymentRegister = paymentRegister;
                paymentCurrencyRegister.Currency = currency;

                outcomePaymentOrder.OrganizationClient = organizationClient;
                outcomePaymentOrder.OrganizationClientAgreement = organizationClientAgreement;
                outcomePaymentOrder.TaxFree = taxFree;
                outcomePaymentOrder.Sad = sad;
                outcomePaymentOrder.Organization = organization;
                outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;
                outcomePaymentOrder.User = user;
                outcomePaymentOrder.Client = outcomesClient;
                outcomePaymentOrder.ClientAgreement = clientAgreement;
                outcomePaymentOrder.Colleague = colleague;
                outcomePaymentOrder.ConsumableProductOrganization = outcomeConsumableProductOrganization;
                outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                outcomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                toReturn = outcomePaymentOrder;
            } else {
                if (outcomePaymentOrderConsumablesOrder != null &&
                    toReturn.OutcomePaymentOrderConsumablesOrders.Any(j => j.Id.Equals(outcomePaymentOrderConsumablesOrder.Id))) {
                    if (consumablesOrder == null || consumablesOrderItem == null) return outcomePaymentOrder;

                    OutcomePaymentOrderConsumablesOrder orderFromList =
                        toReturn.OutcomePaymentOrderConsumablesOrders.First(j => j.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));

                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                    if (consumablesOrderSupplyOrganizationAgreement != null)
                        consumablesOrderSupplyOrganizationAgreement.Currency = consumablesOrderSupplyOrganizationAgreementCurrency;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.SupplyOrganizationAgreement = consumablesOrderSupplyOrganizationAgreement;

                    orderFromList.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);
                } else {
                    if (consumablesOrder == null || consumablesOrderItem == null || outcomePaymentOrderConsumablesOrder == null) return outcomePaymentOrder;

                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                    if (consumablesOrderSupplyOrganizationAgreement != null)
                        consumablesOrderSupplyOrganizationAgreement.Currency = consumablesOrderSupplyOrganizationAgreementCurrency;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.SupplyOrganizationAgreement = consumablesOrderSupplyOrganizationAgreement;

                    consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                    consumablesOrder.User = consumablesOrderUser;
                    consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrder.SupplyOrganizationAgreement = consumablesOrderSupplyOrganizationAgreement;

                    outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                    toReturn.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                }
            }

            return outcomePaymentOrder;
        };

        var props = new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            props
        );

        if (toReturn == null) return toReturn;

        _connection.Query<AssignedPaymentOrder, OutcomePaymentOrder, IncomePaymentOrder, AssignedPaymentOrder>(
            "SELECT * " +
            "FROM [AssignedPaymentOrder] " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
            "LEFT JOIN [IncomePaymentOrder] " +
            "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].AssignedIncomePaymentOrderID " +
            "WHERE [AssignedPaymentOrder].RootOutcomePaymentOrderID = @Id " +
            "AND [AssignedPaymentOrder].Deleted = 0",
            (assigned, assignedOutcome, assignedIncome) => {
                assigned.AssignedOutcomePaymentOrder = assignedOutcome;
                assigned.AssignedIncomePaymentOrder = assignedIncome;

                toReturn.AssignedPaymentOrders.Add(assigned);

                return assigned;
            },
            new { Id = id }
        );

        _connection.Query<AssignedPaymentOrder, OutcomePaymentOrder, IncomePaymentOrder, AssignedPaymentOrder>(
            "SELECT * " +
            "FROM [AssignedPaymentOrder] " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].RootOutcomePaymentOrderID " +
            "LEFT JOIN [IncomePaymentOrder] " +
            "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].RootIncomePaymentOrderID " +
            "WHERE [AssignedPaymentOrder].AssignedOutcomePaymentOrderID = @Id " +
            "AND [AssignedPaymentOrder].Deleted = 0",
            (assigned, assignedOutcome, assignedIncome) => {
                assigned.AssignedOutcomePaymentOrder = assignedOutcome;
                assigned.AssignedIncomePaymentOrder = assignedIncome;

                toReturn.RootAssignedPaymentOrder = assigned;

                return assigned;
            },
            new { Id = id }
        );

        Type[] fuelingsTypes = {
            typeof(CompanyCarFueling),
            typeof(CompanyCar),
            typeof(User),
            typeof(User),
            typeof(SupplyOrganization),
            typeof(PaymentCostMovementOperation),
            typeof(PaymentCostMovement),
            typeof(User)
        };

        Func<object[], CompanyCarFueling> fuelingsMapper = objects => {
            CompanyCarFueling companyCarFueling = (CompanyCarFueling)objects[0];
            CompanyCar companyCar = (CompanyCar)objects[1];
            User createdBy = (User)objects[2];
            User updatedBy = (User)objects[3];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[4];
            PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[5];
            PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[6];
            User user = (User)objects[7];

            companyCar.CreatedBy = createdBy;
            companyCar.UpdatedBy = updatedBy;

            paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

            companyCarFueling.CompanyCar = companyCar;
            companyCarFueling.ConsumableProductOrganization = consumableProductOrganization;
            companyCarFueling.PaymentCostMovementOperation = paymentCostMovementOperation;
            companyCarFueling.User = user;

            toReturn.CompanyCarFuelings.Add(companyCarFueling);

            return companyCarFueling;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [CompanyCarFueling] " +
            "LEFT JOIN [CompanyCar] " +
            "ON [CompanyCar].ID = [CompanyCarFueling].CompanyCarID " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [CompanyCar].CreatedByID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [UpdatedBy].ID = [CompanyCar].UpdatedByID " +
            "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
            "ON [ConsumableProductOrganization].ID = [CompanyCarFueling].ConsumableProductOrganizationID " +
            "LEFT JOIN [PaymentCostMovementOperation] " +
            "ON [PaymentCostMovementOperation].CompanyCarFuelingID = [CompanyCarFueling].ID " +
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
            "LEFT JOIN [User] " +
            "ON [User].ID = [CompanyCarFueling].UserID " +
            "WHERE [CompanyCarFueling].Deleted = 0 " +
            "AND [CompanyCarFueling].OutcomePaymentOrderID = @Id",
            fuelingsTypes,
            fuelingsMapper,
            props
        );

        return toReturn;
    }

    public OutcomePaymentOrder GetByIdForSupplies(long id) {
        OutcomePaymentOrder toReturn = null;

        string sqlExpression =
            "SELECT * " +
            "FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OutcomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
            "LEFT JOIN (" +
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
            "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
            "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
            "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
            "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
            "ON [OutcomePaymentOrderSupplyPaymentTask].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [OutcomePaymentOrderSupplyPaymentTask].SupplyPaymentTaskID " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [OutcomePaymentOrder].ConsumableProductOrganizationID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [OutcomePaymentOrder].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [SupplyOrderPolandPaymentDeliveryProtocol] " +
            "ON [SupplyOrderPolandPaymentDeliveryProtocol].ID = [OutcomePaymentOrder].SupplyOrderPolandPaymentDeliveryProtocolID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [OutcomeAgreement] " +
            "ON [OutcomeAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [SupplyOrganizationOrganization] " +
            "ON [SupplyOrganizationOrganization].ID = [OutcomeAgreement].OrganizationID " +
            "AND [SupplyOrganizationOrganization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [OutcomeAgreementCurrency] " +
            "ON [OutcomeAgreementCurrency].ID = [OutcomeAgreement].CurrencyID " +
            "AND [OutcomeAgreementCurrency].CultureCode = @Culture " +
            "WHERE [OutcomePaymentOrder].ID = @Id";

        Type[] types = {
            typeof(OutcomePaymentOrder),
            typeof(Organization),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(User),
            typeof(OutcomePaymentOrderSupplyPaymentTask),
            typeof(SupplyPaymentTask),
            typeof(SupplyOrganization),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(SupplyOrderPolandPaymentDeliveryProtocol),
            typeof(SupplyOrganizationAgreement),
            typeof(Organization),
            typeof(Currency)
        };

        Func<object[], OutcomePaymentOrder> mapper = objects => {
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[0];
            Organization organization = (Organization)objects[1];
            User user = (User)objects[2];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[3];
            PaymentMovement paymentMovement = (PaymentMovement)objects[4];
            PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency currency = (Currency)objects[6];
            PaymentRegister paymentRegister = (PaymentRegister)objects[7];
            Organization paymentRegisterOrganization = (Organization)objects[8];
            User colleague = (User)objects[9];
            OutcomePaymentOrderSupplyPaymentTask junctionTask = (OutcomePaymentOrderSupplyPaymentTask)objects[10];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[11];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[12];
            ClientAgreement clientAgreement = (ClientAgreement)objects[13];
            Client client = (Client)objects[14];
            SupplyOrderPolandPaymentDeliveryProtocol protocol = (SupplyOrderPolandPaymentDeliveryProtocol)objects[15];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[16];
            Organization supplyOrganizationOrganization = (Organization)objects[17];
            Currency supplyOrganizationAgreementCurrency = (Currency)objects[18];

            if (toReturn == null) {
                if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                if (clientAgreement != null) clientAgreement.Client = client;

                if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Organization = supplyOrganizationOrganization;

                if (junctionTask != null) {
                    junctionTask.SupplyPaymentTask = supplyPaymentTask;

                    outcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);
                }

                if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                paymentRegister.Organization = paymentRegisterOrganization;

                paymentCurrencyRegister.PaymentRegister = paymentRegister;
                paymentCurrencyRegister.Currency = currency;

                outcomePaymentOrder.Organization = organization;
                outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;
                outcomePaymentOrder.ClientAgreement = clientAgreement;
                outcomePaymentOrder.ConsumableProductOrganization = supplyOrganization;
                outcomePaymentOrder.SupplyOrderPolandPaymentDeliveryProtocol = protocol;
                outcomePaymentOrder.User = user;
                outcomePaymentOrder.Colleague = colleague;
                outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                outcomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                toReturn = outcomePaymentOrder;
            } else {
                if (junctionTask != null) {
                    junctionTask.SupplyPaymentTask = supplyPaymentTask;

                    toReturn.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);
                }
            }

            return outcomePaymentOrder;
        };

        var props = new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            props
        );

        if (toReturn != null) {
            _connection.Query<AssignedPaymentOrder, OutcomePaymentOrder, IncomePaymentOrder, AssignedPaymentOrder>(
                "SELECT * " +
                "FROM [AssignedPaymentOrder] " +
                "LEFT JOIN [OutcomePaymentOrder] " +
                "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
                "LEFT JOIN [IncomePaymentOrder] " +
                "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].AssignedIncomePaymentOrderID " +
                "WHERE [AssignedPaymentOrder].RootOutcomePaymentOrderID = @Id " +
                "AND [AssignedPaymentOrder].Deleted = 0",
                (assigned, assignedOutcome, assignedIncome) => {
                    assigned.AssignedOutcomePaymentOrder = assignedOutcome;
                    assigned.AssignedIncomePaymentOrder = assignedIncome;

                    toReturn.AssignedPaymentOrders.Add(assigned);

                    return assigned;
                },
                new { Id = id }
            );

            _connection.Query<AssignedPaymentOrder, OutcomePaymentOrder, IncomePaymentOrder, AssignedPaymentOrder>(
                "SELECT * " +
                "FROM [AssignedPaymentOrder] " +
                "LEFT JOIN [OutcomePaymentOrder] " +
                "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].RootOutcomePaymentOrderID " +
                "LEFT JOIN [IncomePaymentOrder] " +
                "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].RootIncomePaymentOrderID " +
                "WHERE [AssignedPaymentOrder].AssignedOutcomePaymentOrderID = @Id " +
                "AND [AssignedPaymentOrder].Deleted = 0",
                (assigned, assignedOutcome, assignedIncome) => {
                    assigned.AssignedOutcomePaymentOrder = assignedOutcome;
                    assigned.AssignedIncomePaymentOrder = assignedIncome;

                    toReturn.RootAssignedPaymentOrder = assigned;

                    return assigned;
                },
                new { Id = id }
            );

            Type[] fuelingsTypes = {
                typeof(CompanyCarFueling),
                typeof(CompanyCar),
                typeof(User),
                typeof(User),
                typeof(SupplyOrganization),
                typeof(PaymentCostMovementOperation),
                typeof(PaymentCostMovement),
                typeof(User)
            };

            Func<object[], CompanyCarFueling> fuelingsMapper = objects => {
                CompanyCarFueling companyCarFueling = (CompanyCarFueling)objects[0];
                CompanyCar companyCar = (CompanyCar)objects[1];
                User createdBy = (User)objects[2];
                User updatedBy = (User)objects[3];
                SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[4];
                PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[5];
                PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[6];
                User user = (User)objects[7];

                companyCar.CreatedBy = createdBy;
                companyCar.UpdatedBy = updatedBy;

                paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                companyCarFueling.CompanyCar = companyCar;
                companyCarFueling.ConsumableProductOrganization = consumableProductOrganization;
                companyCarFueling.PaymentCostMovementOperation = paymentCostMovementOperation;
                companyCarFueling.User = user;

                toReturn.CompanyCarFuelings.Add(companyCarFueling);

                return companyCarFueling;
            };

            _connection.Query(
                "SELECT * " +
                "FROM [CompanyCarFueling] " +
                "LEFT JOIN [CompanyCar] " +
                "ON [CompanyCar].ID = [CompanyCarFueling].CompanyCarID " +
                "LEFT JOIN [User] AS [CreatedBy] " +
                "ON [CreatedBy].ID = [CompanyCar].CreatedByID " +
                "LEFT JOIN [User] AS [UpdatedBy] " +
                "ON [UpdatedBy].ID = [CompanyCar].UpdatedByID " +
                "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
                "ON [ConsumableProductOrganization].ID = [CompanyCarFueling].ConsumableProductOrganizationID " +
                "LEFT JOIN [PaymentCostMovementOperation] " +
                "ON [PaymentCostMovementOperation].CompanyCarFuelingID = [CompanyCarFueling].ID " +
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
                "LEFT JOIN [User] " +
                "ON [User].ID = [CompanyCarFueling].UserID " +
                "WHERE [CompanyCarFueling].Deleted = 0 " +
                "AND [CompanyCarFueling].OutcomePaymentOrderID = @Id",
                fuelingsTypes,
                fuelingsMapper,
                props
            );
        }

        return toReturn;
    }

    public OutcomePaymentOrder GetByNetId(Guid netId) {
        OutcomePaymentOrder toReturn = null;

        string sqlExpression =
            "SELECT [OutcomePaymentOrder].* " +
            ", ( " +
            "SELECT ROUND( " +
            "( " +
            "SELECT (0 - [ForDifferenceOutcome].Amount) " +
            "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
            "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
            ") " +
            "+ " +
            "( " +
            "SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPriceWithVat), 0)) " +
            "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
            "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
            "ON [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID = [ForDifferenceOutcome].ID " +
            "LEFT JOIN [ConsumablesOrder] " +
            "ON [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "LEFT JOIN [ConsumablesOrderItem] " +
            "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
            "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
            "AND [ForDifferenceOutcome].IsUnderReport = 1 " +
            ") " +
            "+ " +
            "( " +
            "SELECT (ISNULL(SUM([CompanyCarFueling].TotalPriceWithVat), 0)) " +
            "FROM [CompanyCarFueling] " +
            "WHERE [CompanyCarFueling].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
            "AND [CompanyCarFueling].Deleted = 0 " +
            ") " +
            "+ " +
            "( " +
            "SELECT ISNULL(SUM([AssignedIncome].Amount), 0) " +
            "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
            "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
            "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
            "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
            "LEFT JOIN [IncomePaymentOrder] AS [AssignedIncome] " +
            "ON [AssignedIncome].ID = [ForDifferenceAssignedPaymentOrder].AssignedIncomePaymentOrderID " +
            "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
            "AND [AssignedIncome].IsCanceled = 0 " +
            ") " +
            "- " +
            "( " +
            "SELECT ISNULL(SUM([AssignedOutcome].Amount), 0) " +
            "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
            "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
            "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
            "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
            "LEFT JOIN [OutcomePaymentOrder] AS [AssignedOutcome] " +
            "ON [AssignedOutcome].ID = [ForDifferenceAssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
            "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
            ") " +
            ", 2) " +
            ") AS [DifferenceAmount] " +
            ", [Organization].* " +
            ", [User].* " +
            ", [PaymentMovementOperation].* " +
            ", [PaymentMovement].* " +
            ", [PaymentCurrencyRegister].* " +
            ", [Currency].* " +
            ", [PaymentRegister].* " +
            ", [PaymentRegisterOrganization].* " +
            ", [Colleague].* " +
            ", [OutcomeConsumableProductOrganization].* " +
            ", [SupplyOrganizationOrganization].* " +
            ", [ClientAgreement].* " +
            ", [Client].* " +
            ", [OutcomesClient].* " +
            ", [SupplyOrderPolandPaymentDeliveryProtocol].* " +
            ", [OutcomeAgreement].* " +
            ", [OutcomeAgreementCurrency].* " +
            ", [OrganizationClient].* " +
            ", [OrganizationClientAgreement].* " +
            ", [OrganizationClientAgreementCurrency].* " +
            ", [TaxFree].* " +
            ", [Sad].* " +
            ", [AccountingOperationName].* " +
            "FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OutcomePaymentOrder].UserID " +
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
            "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
            "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
            "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
            "LEFT JOIN [SupplyOrganization] AS [OutcomeConsumableProductOrganization] " +
            "ON [OutcomePaymentOrder].ConsumableProductOrganizationID = [OutcomeConsumableProductOrganization].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [OutcomePaymentOrder].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [Client] AS [OutcomesClient] " +
            "ON [OutcomesClient].ID = [OutcomePaymentOrder].ClientID " +
            "LEFT JOIN [SupplyOrderPolandPaymentDeliveryProtocol] " +
            "ON [SupplyOrderPolandPaymentDeliveryProtocol].ID = [OutcomePaymentOrder].SupplyOrderPolandPaymentDeliveryProtocolID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [OutcomeAgreement] " +
            "ON [OutcomeAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [SupplyOrganizationOrganization] " +
            "ON [SupplyOrganizationOrganization].ID = [OutcomeAgreement].OrganizationID " +
            "AND [SupplyOrganizationOrganization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [OutcomeAgreementCurrency] " +
            "ON [OutcomeAgreementCurrency].ID = [OutcomeAgreement].CurrencyID " +
            "AND [OutcomeAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [OrganizationClient] " +
            "ON [OrganizationClient].ID = [OutcomePaymentOrder].OrganizationClientID " +
            "LEFT JOIN [OrganizationClientAgreement] " +
            "ON [OrganizationClientAgreement].ID = [OutcomePaymentOrder].OrganizationClientAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [OrganizationClientAgreementCurrency] " +
            "ON [OrganizationClientAgreementCurrency].ID = [OrganizationClientAgreement].CurrencyID " +
            "AND [OrganizationClientAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [TaxFree] " +
            "ON [TaxFree].ID = [OutcomePaymentOrder].TaxFreeID " +
            "LEFT JOIN [Sad] " +
            "ON [Sad].ID = [OutcomePaymentOrder].SadID " +
            "LEFT JOIN [AccountingOperationName] " +
            "ON [AccountingOperationName].OperationType = [OutcomePaymentOrder].OperationType " +
            "WHERE [OutcomePaymentOrder].NetUID = @NetId ";

        Type[] types = {
            typeof(OutcomePaymentOrder),
            typeof(Organization),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(User),
            typeof(SupplyOrganization),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(Client),
            typeof(SupplyOrderPolandPaymentDeliveryProtocol),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(OrganizationClient),
            typeof(OrganizationClientAgreement),
            typeof(Organization),
            typeof(Currency),
            typeof(TaxFree),
            typeof(Sad),
            typeof(AccountingOperationName)
        };

        Func<object[], OutcomePaymentOrder> mapper = objects => {
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[0];
            Organization organization = (Organization)objects[1];
            User user = (User)objects[2];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[3];
            PaymentMovement paymentMovement = (PaymentMovement)objects[4];
            PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency currency = (Currency)objects[6];
            PaymentRegister paymentRegister = (PaymentRegister)objects[7];
            Organization paymentRegisterOrganization = (Organization)objects[8];
            User colleague = (User)objects[9];
            SupplyOrganization outcomeConsumableProductOrganization = (SupplyOrganization)objects[10];
            ClientAgreement clientAgreement = (ClientAgreement)objects[11];
            Client client = (Client)objects[12];
            Client outcomesClient = (Client)objects[13];
            SupplyOrderPolandPaymentDeliveryProtocol protocol = (SupplyOrderPolandPaymentDeliveryProtocol)objects[14];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[15];
            Currency supplyOrganizationAgreementCurrency = (Currency)objects[16];
            OrganizationClient organizationClient = (OrganizationClient)objects[17];
            OrganizationClientAgreement organizationClientAgreement = (OrganizationClientAgreement)objects[18];
            Organization outcomeConsumableProductOrganizationOrganization = (Organization)objects[19];
            Currency organizationClientAgreementCurrency = (Currency)objects[20];
            TaxFree taxFree = (TaxFree)objects[21];
            Sad sad = (Sad)objects[22];
            AccountingOperationName accountingOperationName = (AccountingOperationName)objects[23];

            if (toReturn == null) {
                if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                if (clientAgreement != null) clientAgreement.Client = client;

                if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Organization = outcomeConsumableProductOrganizationOrganization;

                if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                if (organizationClientAgreement != null) organizationClientAgreement.Currency = organizationClientAgreementCurrency;

                paymentRegister.Organization = paymentRegisterOrganization;

                paymentCurrencyRegister.PaymentRegister = paymentRegister;
                paymentCurrencyRegister.Currency = currency;

                outcomePaymentOrder.OrganizationClient = organizationClient;
                outcomePaymentOrder.OrganizationClientAgreement = organizationClientAgreement;
                outcomePaymentOrder.TaxFree = taxFree;
                outcomePaymentOrder.Sad = sad;
                outcomePaymentOrder.Organization = organization;
                outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;
                outcomePaymentOrder.User = user;
                outcomePaymentOrder.Colleague = colleague;
                outcomePaymentOrder.Client = outcomesClient;
                outcomePaymentOrder.ClientAgreement = clientAgreement;
                outcomePaymentOrder.SupplyOrderPolandPaymentDeliveryProtocol = protocol;
                outcomePaymentOrder.ConsumableProductOrganization = outcomeConsumableProductOrganization;
                outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                outcomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                outcomePaymentOrder.OperationTypeName = paymentRegister.Type == PaymentRegisterType.Cash ? accountingOperationName.CashNameUK : accountingOperationName.BankNameUK;

                toReturn = outcomePaymentOrder;
            }

            return outcomePaymentOrder;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (toReturn == null) return toReturn;

        string sqlQueryOrder =
            "SELECT " +
            "[OutcomePaymentOrderConsumablesOrder].* " +
            ", [ConsumablesOrder].* " +
            ", [ConsumablesOrderItem].* " +
            ", [ConsumableProductCategory].* " +
            ", [ConsumableProduct].* " +
            ", [ConsumableProductOrganization].* " +
            ", [ConsumablesOrderUser].* " +
            ", [ConsumablesStorage].* " +
            ", [PaymentCostMovementOperation].* " +
            ", [PaymentCostMovement].* " +
            ", [MeasureUnit].* " +
            ", [ConsumablesAgreement].* " +
            ", [ConsumablesAgreementCurrency].* " +
            ", [Organization].* " +
            "FROM [OutcomePaymentOrderConsumablesOrder] " +
            "LEFT JOIN [ConsumablesOrder] " +
            "ON [ConsumablesOrder].ID = [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID " +
            "LEFT JOIN [ConsumablesOrderItem] " +
            "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "AND [ConsumablesOrderItem].Deleted = 0 " +
            "LEFT JOIN ( " +
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
            "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture " +
            ") AS [ConsumableProductCategory] " +
            "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
            "LEFT JOIN ( " +
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
            "AND [ConsumableProductTranslation].CultureCode = @Culture " +
            ") AS [ConsumableProduct] " +
            "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
            "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
            "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
            "LEFT JOIN [User] AS [ConsumablesOrderUser] " +
            "ON [ConsumablesOrderUser].ID = [ConsumablesOrder].UserID " +
            "LEFT JOIN [ConsumablesStorage] " +
            "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
            "LEFT JOIN [PaymentCostMovementOperation] " +
            "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
            "LEFT JOIN ( " +
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
            "LEFT JOIN [SupplyOrganizationAgreement] AS [ConsumablesAgreement] " +
            "ON [ConsumablesAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [ConsumablesAgreementCurrency] " +
            "ON [ConsumablesAgreementCurrency].ID = [ConsumablesAgreement].CurrencyID " +
            "AND [ConsumablesAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [ConsumablesAgreement].OrganizationID " +
            "WHERE [OutcomePaymentOrderConsumablesOrder].[OutcomePaymentOrderID] = @Id " +
            "AND [OutcomePaymentOrderConsumablesOrder].Deleted = 0 ";

        Type[] orderTypes = {
            typeof(OutcomePaymentOrderConsumablesOrder),
            typeof(ConsumablesOrder),
            typeof(ConsumablesOrderItem),
            typeof(ConsumableProductCategory),
            typeof(ConsumableProduct),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(ConsumablesStorage),
            typeof(PaymentCostMovementOperation),
            typeof(PaymentCostMovement),
            typeof(MeasureUnit),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(Organization)
        };

        Func<object[], OutcomePaymentOrderConsumablesOrder> mapperOrder = objects => {
            OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[0];
            ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[1];
            ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[2];
            ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[3];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[4];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[5];
            User user = (User)objects[6];
            ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[7];
            PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[8];
            PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[9];
            MeasureUnit measureUnit = (MeasureUnit)objects[10];
            SupplyOrganizationAgreement agreement = (SupplyOrganizationAgreement)objects[11];
            Currency currency = (Currency)objects[12];
            Organization organization = (Organization)objects[13];

            if (!toReturn.OutcomePaymentOrderConsumablesOrders.Any(j => j.Id.Equals(outcomePaymentOrderConsumablesOrder.Id))) {
                toReturn.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;
            } else {
                outcomePaymentOrderConsumablesOrder = toReturn.OutcomePaymentOrderConsumablesOrders.First(j => j.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));
            }

            if (paymentCostMovementOperation != null)
                paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

            if (consumableProduct != null)
                consumableProduct.MeasureUnit = measureUnit;

            if (agreement != null) {
                agreement.Currency = currency;
                agreement.Organization = organization;
            }

            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
            consumablesOrderItem.ConsumableProduct = consumableProduct;
            consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
            consumablesOrderItem.ConsumableProductOrganization = supplyOrganization;
            consumablesOrderItem.SupplyOrganizationAgreement = agreement;

            outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

            outcomePaymentOrderConsumablesOrder.ConsumablesOrder.User = user;
            outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumableProductOrganization = supplyOrganization;
            outcomePaymentOrderConsumablesOrder.ConsumablesOrder.SupplyOrganizationAgreement = agreement;
            outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesStorage = consumablesStorage;

            return outcomePaymentOrderConsumablesOrder;
        };

        _connection.Query(
            sqlQueryOrder,
            orderTypes,
            mapperOrder,
            new { toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName });

        _connection.Query<AssignedPaymentOrder, OutcomePaymentOrder, IncomePaymentOrder, AssignedPaymentOrder>(
            "SELECT * " +
            "FROM [AssignedPaymentOrder] " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
            "LEFT JOIN [IncomePaymentOrder] " +
            "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].AssignedIncomePaymentOrderID " +
            "WHERE [AssignedPaymentOrder].RootOutcomePaymentOrderID = @Id " +
            "AND [AssignedPaymentOrder].Deleted = 0",
            (assigned, assignedOutcome, assignedIncome) => {
                assigned.AssignedOutcomePaymentOrder = assignedOutcome;
                assigned.AssignedIncomePaymentOrder = assignedIncome;

                toReturn.AssignedPaymentOrders.Add(assigned);

                return assigned;
            },
            new { toReturn.Id }
        );

        _connection.Query<AssignedPaymentOrder, OutcomePaymentOrder, IncomePaymentOrder, AssignedPaymentOrder>(
            "SELECT * " +
            "FROM [AssignedPaymentOrder] " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].RootOutcomePaymentOrderID " +
            "LEFT JOIN [IncomePaymentOrder] " +
            "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].RootIncomePaymentOrderID " +
            "WHERE [AssignedPaymentOrder].AssignedOutcomePaymentOrderID = @Id " +
            "AND [AssignedPaymentOrder].Deleted = 0",
            (assigned, assignedOutcome, assignedIncome) => {
                assigned.AssignedOutcomePaymentOrder = assignedOutcome;
                assigned.AssignedIncomePaymentOrder = assignedIncome;

                toReturn.RootAssignedPaymentOrder = assigned;

                return assigned;
            },
            new {
                toReturn.Id
            }
        );

        Type[] fuelingsTypes = {
            typeof(CompanyCarFueling),
            typeof(CompanyCar),
            typeof(User),
            typeof(User),
            typeof(SupplyOrganization),
            typeof(PaymentCostMovementOperation),
            typeof(PaymentCostMovement),
            typeof(User),
            typeof(SupplyOrganizationAgreement),
            typeof(Organization)
        };

        Func<object[], CompanyCarFueling> fuelingsMapper = objects => {
            CompanyCarFueling companyCarFueling = (CompanyCarFueling)objects[0];
            CompanyCar companyCar = (CompanyCar)objects[1];
            User createdBy = (User)objects[2];
            User updatedBy = (User)objects[3];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[4];
            PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[5];
            PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[6];
            User user = (User)objects[7];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[8];
            Organization organization = (Organization)objects[9];

            if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Organization = organization;

            companyCar.CreatedBy = createdBy;
            companyCar.UpdatedBy = updatedBy;

            paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

            companyCarFueling.CompanyCar = companyCar;
            companyCarFueling.ConsumableProductOrganization = consumableProductOrganization;
            companyCarFueling.PaymentCostMovementOperation = paymentCostMovementOperation;
            companyCarFueling.User = user;

            toReturn.CompanyCarFuelings.Add(companyCarFueling);

            return companyCarFueling;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [CompanyCarFueling] " +
            "LEFT JOIN [CompanyCar] " +
            "ON [CompanyCar].ID = [CompanyCarFueling].CompanyCarID " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [CompanyCar].CreatedByID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [UpdatedBy].ID = [CompanyCar].UpdatedByID " +
            "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
            "ON [ConsumableProductOrganization].ID = [CompanyCarFueling].ConsumableProductOrganizationID " +
            "LEFT JOIN [PaymentCostMovementOperation] " +
            "ON [PaymentCostMovementOperation].CompanyCarFuelingID = [CompanyCarFueling].ID " +
            "LEFT JOIN (" +
            "SELECT [PaymentCostMovement].ID " +
            ", [PaymentCostMovement].[Created] " +
            ", [PaymentCostMovement].[Deleted] " +
            ", [PaymentCostMovement].[NetUID] " +
            ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] "
            +
            ", [PaymentCostMovement].[Updated] " +
            "FROM [PaymentCostMovement] " +
            "LEFT JOIN [PaymentCostMovementTranslation] " +
            "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
            "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
            ") AS [PaymentCostMovement] " +
            "ON [PaymentCostMovement].ID = [PaymentCostMovementOperation].PaymentCostMovementID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [CompanyCarFueling].UserID " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [CompanyCarFueling].SupplyOrganizationAgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
            "WHERE [CompanyCarFueling].Deleted = 0 " +
            "AND [CompanyCarFueling].OutcomePaymentOrderID = @Id",
            fuelingsTypes,
            fuelingsMapper,
            new {
                toReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );
        return toReturn;
    }

    public OutcomePaymentOrder GetLastRecord(PaymentRegisterType type) {
        return _connection.Query<OutcomePaymentOrder>(
                "SELECT TOP(1) * " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].ID = [PaymentCurrencyRegister].PaymentRegisterID " +
                "WHERE [OutcomePaymentOrder].Deleted = 0 " +
                "AND [PaymentRegister].Type = @Type " +
                "AND [OutcomePaymentOrder].[Number] NOT LIKE '%' + @FromOneC + '%' " +
                "ORDER BY [OutcomePaymentOrder].ID DESC",
                new {
                    FromOneC = FROM_ONE_C,
                    Type = type
                }
            )
            .SingleOrDefault();
    }

    public OutcomePaymentOrder GetLastAdvanceRecord() {
        return _connection.Query<OutcomePaymentOrder>(
                "SELECT TOP(1) * " +
                "FROM [OutcomePaymentOrder] " +
                "WHERE [OutcomePaymentOrder].Deleted = 0 " +
                "AND [OutcomePaymentOrder].IsUnderReport = 1 " +
                "AND [OutcomePaymentOrder].[Number] NOT LIKE '%' + @FromOneC + '%' " +
                "ORDER BY [OutcomePaymentOrder].ID DESC",
                new { FromOneC = FROM_ONE_C }
            )
            .SingleOrDefault();
    }

    public IEnumerable<OutcomePaymentOrder> GetCurrentOutcomesByCompanyCarNetId(Guid netId) {
        List<OutcomePaymentOrder> outcomePaymentOrders = new();

        Type[] types = {
            typeof(OutcomePaymentOrder),
            typeof(User),
            typeof(User),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(CompanyCarFueling),
            typeof(CompanyCar),
            typeof(SupplyOrganization),
            typeof(PaymentCostMovementOperation),
            typeof(PaymentCostMovement),
            typeof(User)
        };

        Func<object[], OutcomePaymentOrder> mapper = objects => {
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[0];
            User user = (User)objects[1];
            User colleague = (User)objects[2];
            Organization organization = (Organization)objects[3];
            PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[4];
            Currency currency = (Currency)objects[5];
            PaymentRegister paymentRegister = (PaymentRegister)objects[6];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[7];
            PaymentMovement paymentMovement = (PaymentMovement)objects[8];
            CompanyCarFueling companyCarFueling = (CompanyCarFueling)objects[9];
            CompanyCar companyCar = (CompanyCar)objects[10];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[11];
            PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[12];
            PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[13];
            User companyCarFuelingUser = (User)objects[14];

            if (!outcomePaymentOrders.Any(o => o.Id.Equals(outcomePaymentOrder.Id))) {
                paymentCurrencyRegister.Currency = currency;
                paymentCurrencyRegister.PaymentRegister = paymentRegister;

                paymentMovementOperation.PaymentMovement = paymentMovement;

                paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                companyCarFueling.CompanyCar = companyCar;
                companyCarFueling.ConsumableProductOrganization = consumableProductOrganization;
                companyCarFueling.PaymentCostMovementOperation = paymentCostMovementOperation;
                companyCarFueling.User = companyCarFuelingUser;

                outcomePaymentOrder.CompanyCarFuelings.Add(companyCarFueling);

                outcomePaymentOrder.User = user;
                outcomePaymentOrder.Colleague = colleague;
                outcomePaymentOrder.Organization = organization;
                outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;
                outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;

                outcomePaymentOrders.Add(outcomePaymentOrder);
            } else {
                companyCarFueling.CompanyCar = companyCar;
                companyCarFueling.ConsumableProductOrganization = consumableProductOrganization;
                companyCarFueling.PaymentCostMovementOperation = paymentCostMovementOperation;
                companyCarFueling.User = companyCarFuelingUser;

                outcomePaymentOrders.First(o => o.Id.Equals(outcomePaymentOrder.Id)).CompanyCarFuelings.Add(companyCarFueling);
            }

            return outcomePaymentOrder;
        };

        _connection.Query(
            "SELECT [OutcomePaymentOrder].* " +
            ",[User].* " +
            ",[Colleague].* " +
            ",[Organization].* " +
            ",[PaymentCurrencyRegister].* " +
            ",[Currency].* " +
            ",[PaymentRegister].* " +
            ",[PaymentMovementOperation].* " +
            ",[PaymentMovement].* " +
            ",[CompanyCarFueling].* " +
            ",[CompanyCar].* " +
            ",[ConsumableProductOrganization].* " +
            ",[PaymentCostMovementOperation].* " +
            ",[PaymentCostMovement].* " +
            ",[CompanyCarFuelingUser].* " +
            "FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OutcomePaymentOrder].UserID " +
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [PaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [PaymentMovementOperation].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
            "LEFT JOIN (" +
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
            "LEFT JOIN [CompanyCarFueling] " +
            "ON [CompanyCarFueling].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
            "LEFT JOIN [CompanyCar] " +
            "ON [CompanyCar].ID = [CompanyCarFueling].CompanyCarID " +
            "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
            "ON [ConsumableProductOrganization].ID = [CompanyCarFueling].ConsumableProductOrganizationID " +
            "LEFT JOIN [PaymentCostMovementOperation] " +
            "ON [PaymentCostMovementOperation].CompanyCarFuelingID = [CompanyCarFueling].ID " +
            "LEFT JOIN (" +
            "SELECT [PaymentCostMovement].ID " +
            ", [PaymentCostMovement].[Created] " +
            ", [PaymentCostMovement].[Deleted] " +
            ", [PaymentCostMovement].[NetUID] " +
            ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] "
            +
            ", [PaymentCostMovement].[Updated] " +
            "FROM [PaymentCostMovement] " +
            "LEFT JOIN [PaymentCostMovementTranslation] " +
            "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
            "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
            ") AS [PaymentCostMovement] " +
            "ON [PaymentCostMovement].ID = [PaymentCostMovementOperation].PaymentCostMovementID " +
            "LEFT JOIN [User] AS [CompanyCarFuelingUser] " +
            "ON [CompanyCarFuelingUser].ID = [CompanyCarFueling].UserID " +
            "LEFT JOIN [CompanyCarRoadList] " +
            "ON [CompanyCarRoadList].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
            "AND [CompanyCarRoadList].Deleted = 0 " +
            "WHERE [CompanyCarRoadList].ID IS NULL " +
            "AND [CompanyCar].NetUID = @NetId " +
            "ORDER BY [OutcomePaymentOrder].ID DESC",
            types,
            mapper,
            new {
                NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );
        return outcomePaymentOrders;
    }

    public IEnumerable<OutcomePaymentOrder> GetAllByColleagueNetId(Guid colleagueNetId) {
        return _connection.Query<OutcomePaymentOrder>(
            "SELECT [OutcomePaymentOrder].* " +
            "FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
            "WHERE [OutcomePaymentOrder].Deleted = 0 " +
            "AND [Colleague].NetUID = @ColleagueNetId",
            new {
                ColleagueNetId = colleagueNetId
            }
        );
    }

    public Tuple<IEnumerable<OutcomePaymentOrder>, decimal, decimal> GetAll(
        long limit,
        long offset,
        DateTime from,
        DateTime to,
        string value,
        Guid? currencyNetId,
        Guid? registerNetId,
        Guid? paymentMovementNetId,
        long[] organizatoinIds) {
        List<OutcomePaymentOrder> toReturn = new();
        decimal positiveDifferenceAmount = decimal.Zero;
        decimal negativeDifferenceAmount = decimal.Zero;

        Type[] types = {
            typeof(OutcomePaymentOrder),
            typeof(Organization),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(User),
            typeof(SupplyOrganization),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(SupplyOrderPolandPaymentDeliveryProtocol),
            typeof(OutcomePaymentOrderSupplyPaymentTask),
            typeof(SupplyPaymentTask),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(OrganizationClient),
            typeof(OrganizationClientAgreement),
            typeof(Organization),
            typeof(Currency),
            typeof(TaxFree),
            typeof(Sad),
            typeof(AccountingOperationName)
        };

        Func<object[], OutcomePaymentOrder> mapper = objects => {
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[0];
            Organization organization = (Organization)objects[1];
            User user = (User)objects[2];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[3];
            PaymentMovement paymentMovement = (PaymentMovement)objects[4];
            PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency currency = (Currency)objects[6];
            PaymentRegister paymentRegister = (PaymentRegister)objects[7];
            Organization paymentRegisterOrganization = (Organization)objects[8];
            User colleague = (User)objects[9];
            SupplyOrganization outcomeConsumableProductOrganization = (SupplyOrganization)objects[10];
            Client outcomeClient = (Client)objects[11];
            ClientAgreement clientAgreement = (ClientAgreement)objects[12];
            Client client = (Client)objects[13];
            SupplyOrderPolandPaymentDeliveryProtocol protocol = (SupplyOrderPolandPaymentDeliveryProtocol)objects[14];
            OutcomePaymentOrderSupplyPaymentTask junctionTask = (OutcomePaymentOrderSupplyPaymentTask)objects[15];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[16];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[17];
            Currency supplyOrganizationAgreementCurrency = (Currency)objects[18];
            OrganizationClient organizationClient = (OrganizationClient)objects[19];
            OrganizationClientAgreement organizationClientAgreement = (OrganizationClientAgreement)objects[20];
            Organization outcomeConsumableProductOrganizationOrganization = (Organization)objects[21];
            Currency organizationClientAgreementCurrency = (Currency)objects[22];
            TaxFree taxFree = (TaxFree)objects[23];
            Sad sad = (Sad)objects[24];
            AccountingOperationName accountingOperationName = (AccountingOperationName)objects[25];

            if (Math.Abs(outcomePaymentOrder.DifferenceAmount).Equals(outcomePaymentOrder.Amount)) outcomePaymentOrder.DifferenceAmount = decimal.Zero;

            if (!toReturn.Any(o => o.Id.Equals(outcomePaymentOrder.Id))) {
                if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                if (clientAgreement != null) clientAgreement.Client = client;

                if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Organization = outcomeConsumableProductOrganizationOrganization;

                if (junctionTask != null) {
                    junctionTask.SupplyPaymentTask = supplyPaymentTask;

                    outcomePaymentOrder.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);
                }

                if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                if (organizationClientAgreement != null) organizationClientAgreement.Currency = organizationClientAgreementCurrency;

                outcomePaymentOrder.Colleague = colleague;

                paymentRegister.Organization = paymentRegisterOrganization;

                paymentCurrencyRegister.PaymentRegister = paymentRegister;
                paymentCurrencyRegister.Currency = currency;

                outcomePaymentOrder.OrganizationClient = organizationClient;
                outcomePaymentOrder.OrganizationClientAgreement = organizationClientAgreement;
                outcomePaymentOrder.TaxFree = taxFree;
                outcomePaymentOrder.Sad = sad;
                outcomePaymentOrder.Organization = organization;
                outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;
                outcomePaymentOrder.User = user;
                outcomePaymentOrder.Colleague = colleague;
                outcomePaymentOrder.Client = outcomeClient;
                outcomePaymentOrder.ClientAgreement = clientAgreement;
                outcomePaymentOrder.SupplyOrderPolandPaymentDeliveryProtocol = protocol;
                outcomePaymentOrder.ConsumableProductOrganization = outcomeConsumableProductOrganization;
                outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                outcomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                outcomePaymentOrder.OperationTypeName = paymentRegister.Type == PaymentRegisterType.Cash ? accountingOperationName.CashNameUK : accountingOperationName.BankNameUK;
                outcomePaymentOrder.EuroAmount = decimal.Round(outcomePaymentOrder.EuroAmount, 2, MidpointRounding.AwayFromZero);

                toReturn.Add(outcomePaymentOrder);

                if (!outcomePaymentOrder.IsUnderReport) return outcomePaymentOrder;

                if (outcomePaymentOrder.DifferenceAmount > decimal.Zero)
                    positiveDifferenceAmount = Math.Round(positiveDifferenceAmount + outcomePaymentOrder.DifferenceAmount, 2);
                else if (outcomePaymentOrder.DifferenceAmount < decimal.Zero)
                    negativeDifferenceAmount = Math.Round(negativeDifferenceAmount + outcomePaymentOrder.DifferenceAmount, 2);
            } else {
                OutcomePaymentOrder fromList = toReturn.First(o => o.Id.Equals(outcomePaymentOrder.Id));

                if (junctionTask == null || fromList.OutcomePaymentOrderSupplyPaymentTasks.Any(j => j.Id.Equals(junctionTask.Id))) return outcomePaymentOrder;

                junctionTask.SupplyPaymentTask = supplyPaymentTask;

                fromList.OutcomePaymentOrderSupplyPaymentTasks.Add(junctionTask);
            }

            return outcomePaymentOrder;
        };

        value = value.Trim();
        string[] concreteValues = value.Split(' ');
        dynamic props = new ExpandoObject();
        props.Limit = limit;
        props.Offset = offset;
        props.CurrencyNetId = currencyNetId ?? Guid.Empty;
        props.RegisterNetId = registerNetId ?? Guid.Empty;
        props.PaymentMovementNetId = paymentMovementNetId ?? Guid.Empty;
        props.Value = value;
        props.From = from;
        props.To = to;
        props.Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        props.OrganizationIds = organizatoinIds;
        for (int i = 0;
             i < concreteValues.Length;
             i++)
            (props as ExpandoObject).AddProperty($"Var{i}", concreteValues[i]);

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [OutcomePaymentOrder].ID DESC) AS RowNumber " +
            ", [OutcomePaymentOrder].ID " +
            ", COUNT(*) OVER() [TotalRowsQty] " +
            "FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [PaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
            "LEFT JOIN [PaymentMovement] " +
            "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
            "LEFT JOIN [PaymentMovementTranslation] " +
            "ON [PaymentMovementTranslation].PaymentMovementID = [PaymentMovement].ID " +
            "AND [PaymentMovementTranslation].CultureCode = @Culture " +
            "AND [PaymentMovementTranslation].Deleted = 0 " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OutcomePaymentOrder].UserID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [OutcomePaymentOrder].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [OutcomePaymentOrder].Deleted = 0 " +
            "AND [OutcomePaymentOrder].FromDate >= @From " +
            "AND [OutcomePaymentOrder].FromDate <= @To ";
        for (int i = 0;
             i < concreteValues.Length;
             i++)
            sqlExpression +=
                "AND ( " +
                $"[OutcomePaymentOrder].Amount like '%' + @Var{i} + '%' " +
                $"OR [OutcomePaymentOrder].Comment like '%' + @Var{i} + '%' " +
                $"OR [OutcomePaymentOrder].Number like '%' + @Var{i} + '%' " +
                $"OR [PaymentRegister].Name like '%' + @Var{i} + '%' " +
                $"OR [User].LastName like '%' + @Var{i} + '%' " +
                $"OR [Currency].Code like '%' + @Var{i} + '%' " +
                $"OR [Client].FullName like '%' + @Var{i} + '%' " +
                $"OR [PaymentMovementTranslation].Name like '%' + @Var{i} + '%' " +
                $"OR [OrganizationTranslation].Name like '%' + @Var{i} + '%' " +
                $"OR [Colleague].LastName like '%' + @Var{i} + '%' " +
                ") ";

        if (currencyNetId.HasValue) sqlExpression += "AND [Currency].NetUID = @CurrencyNetId ";

        if (registerNetId.HasValue) sqlExpression += "AND [PaymentRegister].NetUID = @RegisterNetId ";

        if (paymentMovementNetId.HasValue) sqlExpression += "AND [PaymentMovement].NetUID = @PaymentMovementNetId ";

        if (organizatoinIds.Any()) sqlExpression += "AND [Organization].ID IN @OrganizationIds ";

        sqlExpression += ") " +
                         "SELECT [OutcomePaymentOrder].*, " +
                         "(SELECT TOP 1 TotalRowsQty FROM [Search_CTE]) AS TotalRowsQty, " +
                         "(SELECT ROUND( " +
                         "(SELECT (0 - [ForDifferenceOutcome].Amount) " +
                         "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                         "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID ) " +
                         "+ " +
                         "(SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPriceWithVat), 0)) " +
                         "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
                         "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
                         "ON [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID = [ForDifferenceOutcome].ID " +
                         "LEFT JOIN [ConsumablesOrder] " +
                         "ON [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = [ConsumablesOrder].ID " +
                         "LEFT JOIN [ConsumablesOrderItem] " +
                         "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
                         "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID AND [ForDifferenceOutcome].IsUnderReport = 1 ) " +
                         "+ " +
                         "( SELECT (ISNULL(SUM([CompanyCarFueling].TotalPriceWithVat), 0)) " +
                         "FROM [CompanyCarFueling] " +
                         "WHERE [CompanyCarFueling].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                         "AND [CompanyCarFueling].Deleted = 0 ) " +
                         "+ " +
                         "( SELECT ISNULL(SUM([AssignedIncome].Amount), 0) " +
                         "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
                         "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
                         "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
                         "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
                         "LEFT JOIN [IncomePaymentOrder] AS [AssignedIncome] " +
                         "ON [AssignedIncome].ID = [ForDifferenceAssignedPaymentOrder].AssignedIncomePaymentOrderID " +
                         "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
                         "AND [AssignedIncome].IsCanceled = 0) " +
                         "- " +
                         "( SELECT ISNULL(SUM([AssignedOutcome].Amount), 0) " +
                         "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
                         "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
                         "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
                         "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
                         "LEFT JOIN [OutcomePaymentOrder] AS [AssignedOutcome] " +
                         "ON [AssignedOutcome].ID = [ForDifferenceAssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
                         "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID ) , 2) " +
                         ") AS [DifferenceAmount] " +
                         ", [Organization].* " +
                         ", [User].* " +
                         ", [PaymentMovementOperation].* " +
                         ", [PaymentMovement].* " +
                         ", [PaymentCurrencyRegister].* " +
                         ", [Currency].* " +
                         ", [PaymentRegister].* " +
                         ", [PaymentRegisterOrganization].* " +
                         ", [Colleague].* " +
                         ", [OutcomeConsumableProductOrganization].* " +
                         ", [OutcomeClient].* " +
                         ", [ClientAgreement].* " +
                         ", [Client].* " +
                         ", [SupplyOrderPolandPaymentDeliveryProtocol].* " +
                         ", [OutcomePaymentOrderSupplyPaymentTask].* " +
                         ", [SupplyPaymentTask].* " +
                         ", [OutcomeAgreement].* " +
                         ", [OutcomeAgreementCurrency].* " +
                         ", [OrganizationClient].* " +
                         ", [OrganizationClientAgreement].* " +
                         ", [SupplyOrganizationOrganization].* " +
                         ", [OrganizationClientAgreementCurrency].* " +
                         ", [TaxFree].* " +
                         ", [Sad].* " +
                         ", [AccountingOperationName].* " +
                         "FROM [OutcomePaymentOrder] " +
                         "LEFT JOIN [Organization] " +
                         "ON [Organization].[ID] = [OutcomePaymentOrder].[OrganizationID] " +
                         "LEFT JOIN [User] " +
                         "ON [User].[ID] = [OutcomePaymentOrder].[UserID] " +
                         "LEFT JOIN [PaymentMovementOperation] " +
                         "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
                         "LEFT JOIN ( " +
                         "SELECT " +
                         "[PaymentMovement].ID " +
                         ", [PaymentMovement].[Created] " +
                         ", [PaymentMovement].[Deleted] " +
                         ", [PaymentMovement].[NetUID] " +
                         ", (CASE " +
                         "WHEN [PaymentMovementTranslation].[Name] IS NOT NULL " +
                         "THEN [PaymentMovementTranslation].[Name] " +
                         "ELSE [PaymentMovement].[OperationName] " +
                         "END " +
                         ") AS [OperationName] " +
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
                         "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
                         "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
                         "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
                         "LEFT JOIN [User] AS [Colleague] " +
                         "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
                         "LEFT JOIN [SupplyOrganization] AS [OutcomeConsumableProductOrganization] " +
                         "ON [OutcomePaymentOrder].ConsumableProductOrganizationID = [OutcomeConsumableProductOrganization].ID " +
                         "LEFT JOIN [Client] AS [OutcomeClient] " +
                         "ON [OutcomeClient].ID = [OutcomePaymentOrder].ClientID " +
                         "LEFT JOIN [ClientAgreement] " +
                         "ON [ClientAgreement].ID = [OutcomePaymentOrder].ClientAgreementID " +
                         "LEFT JOIN [Client] " +
                         "ON [Client].ID = [ClientAgreement].ClientID " +
                         "LEFT JOIN [SupplyOrderPolandPaymentDeliveryProtocol] " +
                         "ON [SupplyOrderPolandPaymentDeliveryProtocol].ID = [OutcomePaymentOrder].SupplyOrderPolandPaymentDeliveryProtocolID " +
                         "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                         "ON [OutcomePaymentOrderSupplyPaymentTask].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
                         "LEFT JOIN [SupplyPaymentTask] " +
                         "ON [SupplyPaymentTask].ID = [OutcomePaymentOrderSupplyPaymentTask].SupplyPaymentTaskID " +
                         "LEFT JOIN [SupplyOrganizationAgreement] AS [OutcomeAgreement] " +
                         "ON [OutcomeAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
                         "LEFT JOIN [views].[CurrencyView] AS [OutcomeAgreementCurrency] " +
                         "ON [OutcomeAgreementCurrency].ID = [OutcomeAgreement].CurrencyID " +
                         "AND [OutcomeAgreementCurrency].CultureCode = @Culture " +
                         "LEFT JOIN [Organization] AS [SupplyOrganizationOrganization] " +
                         "ON [SupplyOrganizationOrganization].[ID] = [OutcomeAgreement].OrganizationID " +
                         "LEFT JOIN [OrganizationClient] " +
                         "ON [OrganizationClient].ID = [OutcomePaymentOrder].OrganizationClientID " +
                         "LEFT JOIN [OrganizationClientAgreement] " +
                         "ON [OrganizationClientAgreement].ID = [OutcomePaymentOrder].OrganizationClientAgreementID " +
                         "LEFT JOIN ( " +
                         "SELECT " +
                         "[Currency].[ID] " +
                         ", [Currency].[Code] " +
                         ", [Currency].[Created] " +
                         ", [Currency].[Deleted] " +
                         ", (CASE " +
                         "WHEN [CurrencyTranslation].[Name] IS NOT NULL " +
                         "THEN [CurrencyTranslation].[Name] " +
                         "ELSE [Currency].[Name] " +
                         "END) AS [Name] " +
                         ", [Currency].[NetUID] " +
                         ", [Currency].[Updated] " +
                         "FROM [Currency] " +
                         "LEFT JOIN [CurrencyTranslation] " +
                         "ON [CurrencyTranslation].CurrencyID = [Currency].ID " +
                         "AND [CurrencyTranslation].CultureCode = @Culture " +
                         "AND [CurrencyTranslation].Deleted = 0 " +
                         ") AS [OrganizationClientAgreementCurrency] " +
                         "ON [OrganizationClientAgreementCurrency].ID = [OrganizationClientAgreement].CurrencyID " +
                         "LEFT JOIN [TaxFree] " +
                         "ON [TaxFree].ID = [OutcomePaymentOrder].TaxFreeID " +
                         "LEFT JOIN [Sad] " +
                         "ON [Sad].ID = [OutcomePaymentOrder].SadID " +
                         "LEFT JOIN [AccountingOperationName] " +
                         "ON [AccountingOperationName].OperationType = [OutcomePaymentOrder].OperationType " +
                         "WHERE [OutcomePaymentOrder].ID IN ( " +
                         "SELECT [Search_CTE].ID " +
                         "FROM [Search_CTE] " +
                         "WHERE [Search_CTE].RowNumber > @Offset " +
                         "AND [Search_CTE].RowNumber <= @Limit + @Offset ) " +
                         "ORDER BY [OutcomePaymentOrder].FromDate DESC ";

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            (object)props
        );

        if (!toReturn.Any()) return new Tuple<IEnumerable<OutcomePaymentOrder>, decimal, decimal>(toReturn, positiveDifferenceAmount, negativeDifferenceAmount);

        string sqlQueryItems =
            "SELECT " +
            "[OutcomePaymentOrderConsumablesOrder].* " +
            ", [ConsumablesOrder].* " +
            ", [ConsumablesOrderUser].* " +
            ", [ConsumablesStorage].* " +
            ", [ConsumablesOrderItem].* " +
            ", [ConsumableProductCategory].* " +
            ", [ConsumableProduct].* " +
            ", [ConsumableProductOrganization].* " +
            ", [PaymentCostMovementOperation].* " +
            ", [PaymentCostMovement].* " +
            ", [MeasureUnit].* " +
            ", [ConsumablesAgreement].* " +
            ", [ConsumablesAgreementCurrency].* " +
            "FROM [OutcomePaymentOrderConsumablesOrder] " +
            "LEFT JOIN [ConsumablesOrder] " +
            "ON [ConsumablesOrder].ID = [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID " +
            "LEFT JOIN [User] AS [ConsumablesOrderUser] " +
            "ON [ConsumablesOrderUser].[ID] = [ConsumablesOrder].[UserID] " +
            "LEFT JOIN [ConsumablesStorage] " +
            "ON [ConsumablesStorage].[ID] = [ConsumablesOrder].[ConsumablesStorageID] " +
            "LEFT JOIN [ConsumablesOrderItem] " +
            "ON [ConsumablesOrderItem].[ConsumablesOrderID] = [ConsumablesOrder].[ID] " +
            "AND [ConsumablesOrderItem].[Deleted] = 0 " +
            "LEFT JOIN ( " +
            "SELECT " +
            "[ConsumableProductCategory].ID " +
            ", [ConsumableProductCategory].[Created] " +
            ", [ConsumableProductCategory].[Deleted] " +
            ", [ConsumableProductCategory].[NetUID] " +
            ", (CASE " +
            "WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL " +
            "THEN [ConsumableProductCategoryTranslation].Name " +
            "ELSE [ConsumableProductCategory].Name " +
            "END) AS [Name] " +
            ", (CASE " +
            "WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL " +
            "THEN [ConsumableProductCategoryTranslation].Description " +
            "ELSE [ConsumableProductCategory].Description " +
            "END) AS [Description] " +
            ", [ConsumableProductCategory].[Updated] " +
            "FROM [ConsumableProductCategory] " +
            "LEFT JOIN [ConsumableProductCategoryTranslation] " +
            "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
            "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture " +
            ") AS [ConsumableProductCategory] " +
            "ON [ConsumableProductCategory].ID = [ConsumablesOrderItem].ConsumableProductCategoryID " +
            "LEFT JOIN ( " +
            "SELECT " +
            "[ConsumableProduct].ID " +
            ", [ConsumableProduct].[ConsumableProductCategoryID] " +
            ", [ConsumableProduct].[Created] " +
            ", [ConsumableProduct].[VendorCode] " +
            ", [ConsumableProduct].[Deleted] " +
            ", (CASE " +
            "WHEN [ConsumableProductTranslation].Name IS NOT NULL " +
            "THEN [ConsumableProductTranslation].Name " +
            "ELSE [ConsumableProduct].Name " +
            "END) AS [Name] " +
            ", [ConsumableProduct].[NetUID] " +
            ", [ConsumableProduct].[MeasureUnitID] " +
            ", [ConsumableProduct].[Updated] " +
            "FROM [ConsumableProduct] " +
            "LEFT JOIN [ConsumableProductTranslation] " +
            "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
            "AND [ConsumableProductTranslation].CultureCode = @Culture " +
            ") AS [ConsumableProduct] " +
            "ON [ConsumableProduct].ID = [ConsumablesOrderItem].ConsumableProductID " +
            "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
            "ON [ConsumableProductOrganization].ID = [ConsumablesOrderItem].ConsumableProductOrganizationID " +
            "LEFT JOIN [PaymentCostMovementOperation] " +
            "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
            "LEFT JOIN ( " +
            "SELECT " +
            "[PaymentCostMovement].ID " +
            ", [PaymentCostMovement].[Created] " +
            ", [PaymentCostMovement].[Deleted] " +
            ", [PaymentCostMovement].[NetUID] " +
            ", (CASE " +
            "WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL " +
            "THEN [PaymentCostMovementTranslation].[OperationName] " +
            "ELSE [PaymentCostMovement].[OperationName] " +
            "END) AS [OperationName] " +
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
            "LEFT JOIN [SupplyOrganizationAgreement] AS [ConsumablesAgreement] " +
            "ON [ConsumablesAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
            "LEFT JOIN [Currency] AS [ConsumablesAgreementCurrency] " +
            "ON [ConsumablesAgreementCurrency].[ID] = [ConsumablesAgreement].CurrencyID " +
            "WHERE [OutcomePaymentOrderConsumablesOrder].Deleted = 0 " +
            "AND [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID IN @Ids ";

        Type[] typesItems = {
            typeof(OutcomePaymentOrderConsumablesOrder),
            typeof(ConsumablesOrder),
            typeof(User),
            typeof(ConsumablesStorage),
            typeof(ConsumablesOrderItem),
            typeof(ConsumableProductCategory),
            typeof(ConsumableProduct),
            typeof(SupplyOrganization),
            typeof(PaymentCostMovementOperation),
            typeof(PaymentCostMovement),
            typeof(MeasureUnit),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency)
        };

        Func<object[], OutcomePaymentOrderConsumablesOrder> mapperItems = objects => {
            OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[0];
            ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[1];
            User consumablesOrderUser = (User)objects[2];
            ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[3];
            ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[4];
            ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[5];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[6];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[7];
            PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[8];
            PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[9];
            MeasureUnit measureUnit = (MeasureUnit)objects[10];
            SupplyOrganizationAgreement consumablesOrderSupplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[11];
            Currency consumablesOrderSupplyOrganizationAgreementCurrency = (Currency)objects[12];

            OutcomePaymentOrder outcomePaymentOrder =
                toReturn
                    .First(x => x.Id.Equals(outcomePaymentOrderConsumablesOrder.OutcomePaymentOrderId));

            if (!outcomePaymentOrder.OutcomePaymentOrderConsumablesOrders
                    .Any(o => o.Id.Equals(outcomePaymentOrderConsumablesOrder.Id)))
                outcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
            else
                outcomePaymentOrderConsumablesOrder =
                    outcomePaymentOrder.OutcomePaymentOrderConsumablesOrders
                        .First(o => o.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));

            if (consumablesOrder == null) return outcomePaymentOrderConsumablesOrder;

            if (outcomePaymentOrderConsumablesOrder.ConsumablesOrder == null)
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

            outcomePaymentOrderConsumablesOrder.ConsumablesOrder.User = consumablesOrderUser;
            outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesStorage = consumablesStorage;

            if (!outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesOrderItems.Any(x => x.Id.Equals(consumablesOrderItem.Id)))
                outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);
            else
                consumablesOrderItem = outcomePaymentOrderConsumablesOrder.ConsumablesOrder.ConsumablesOrderItems.First(x => x.Id.Equals(consumablesOrderItem.Id));

            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
            consumableProduct.MeasureUnit = measureUnit;
            consumablesOrderItem.ConsumableProduct = consumableProduct;
            paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;
            consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;

            if (consumablesOrderSupplyOrganizationAgreement == null) return outcomePaymentOrderConsumablesOrder;

            consumablesOrderItem.SupplyOrganizationAgreement = consumablesOrderSupplyOrganizationAgreement;
            consumablesOrderSupplyOrganizationAgreement.Currency = consumablesOrderSupplyOrganizationAgreementCurrency;

            return outcomePaymentOrderConsumablesOrder;
        };

        _connection.Query(
            sqlQueryItems, typesItems, mapperItems,
            new {
                Ids = toReturn.Select(x => x.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            });

        Type[] assignedPaymentOrderTypes = {
            typeof(AssignedPaymentOrder),
            typeof(OutcomePaymentOrder),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(Organization),
            typeof(User),
            typeof(IncomePaymentOrder),
            typeof(Client),
            typeof(Organization),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(User)
        };

        Func<object[], AssignedPaymentOrder> assignedPaymentOrderMapper = objects => {
            AssignedPaymentOrder assignedPaymentOrder = (AssignedPaymentOrder)objects[0];
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[1];
            User outcomePaymentOrderUser = (User)objects[2];
            PaymentMovementOperation outcomePaymentOrderPaymentMovementOperation = (PaymentMovementOperation)objects[3];
            PaymentMovement outcomePaymentOrderPaymentMovement = (PaymentMovement)objects[4];
            PaymentCurrencyRegister outcomePaymentOrderPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency outcomePaymentOrderCurrency = (Currency)objects[6];
            PaymentRegister outcomePaymentOrderPaymentRegister = (PaymentRegister)objects[7];
            Organization outcomePaymentOrderPaymentRegisterOrganization = (Organization)objects[8];
            Organization outcomePaymentOrderOrganization = (Organization)objects[9];
            User outcomePaymentOrderColleague = (User)objects[10];
            IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[11];
            Client incomePaymentOrderClient = (Client)objects[12];
            Organization incomePaymentOrderOrganization = (Organization)objects[13];
            Currency incomePaymentOrderCurrency = (Currency)objects[14];
            PaymentRegister incomePaymentOrderPaymentRegister = (PaymentRegister)objects[15];
            Organization incomePaymentOrderPaymentRegisterOrganization = (Organization)objects[16];
            PaymentCurrencyRegister incomePaymentOrderPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[17];
            Currency incomePaymentOrderPaymentCurrencyRegisterCurrency = (Currency)objects[18];
            User incomePaymentOrderUser = (User)objects[19];
            PaymentMovementOperation incomePaymentOrderPaymentMovementOperation = (PaymentMovementOperation)objects[20];
            PaymentMovement incomePaymentOrderPaymentMovement = (PaymentMovement)objects[21];
            User incomePaymentOrderColleague = (User)objects[22];

            if (outcomePaymentOrder != null) {
                if (outcomePaymentOrderPaymentMovementOperation != null) outcomePaymentOrderPaymentMovementOperation.PaymentMovement = outcomePaymentOrderPaymentMovement;

                outcomePaymentOrderPaymentRegister.Organization = outcomePaymentOrderPaymentRegisterOrganization;

                outcomePaymentOrderPaymentCurrencyRegister.PaymentRegister = outcomePaymentOrderPaymentRegister;
                outcomePaymentOrderPaymentCurrencyRegister.Currency = outcomePaymentOrderCurrency;

                outcomePaymentOrder.User = outcomePaymentOrderUser;
                outcomePaymentOrder.Colleague = outcomePaymentOrderColleague;
                outcomePaymentOrder.PaymentMovementOperation = outcomePaymentOrderPaymentMovementOperation;
                outcomePaymentOrder.Organization = outcomePaymentOrderOrganization;
                outcomePaymentOrder.PaymentCurrencyRegister = outcomePaymentOrderPaymentCurrencyRegister;
            }

            if (incomePaymentOrder != null) {
                if (incomePaymentOrderPaymentCurrencyRegister != null) {
                    incomePaymentOrderPaymentCurrencyRegister.Currency = incomePaymentOrderPaymentCurrencyRegisterCurrency;

                    incomePaymentOrderPaymentRegister.PaymentCurrencyRegisters.Add(incomePaymentOrderPaymentCurrencyRegister);
                }

                if (incomePaymentOrderPaymentMovementOperation != null) {
                    incomePaymentOrderPaymentMovementOperation.PaymentMovement = incomePaymentOrderPaymentMovement;

                    incomePaymentOrder.PaymentMovementOperation = incomePaymentOrderPaymentMovementOperation;
                }

                incomePaymentOrderPaymentRegister.Organization = incomePaymentOrderPaymentRegisterOrganization;

                incomePaymentOrder.Client = incomePaymentOrderClient;
                incomePaymentOrder.Organization = incomePaymentOrderOrganization;
                incomePaymentOrder.Currency = incomePaymentOrderCurrency;
                incomePaymentOrder.PaymentRegister = incomePaymentOrderPaymentRegister;
                incomePaymentOrder.User = incomePaymentOrderUser;
                incomePaymentOrder.Colleague = incomePaymentOrderColleague;
            }

            assignedPaymentOrder.AssignedOutcomePaymentOrder = outcomePaymentOrder;
            assignedPaymentOrder.AssignedIncomePaymentOrder = incomePaymentOrder;

            if (assignedPaymentOrder.RootOutcomePaymentOrderId != null)
                toReturn.First(o => o.Id.Equals(assignedPaymentOrder.RootOutcomePaymentOrderId.Value)).AssignedPaymentOrders.Add(assignedPaymentOrder);

            return assignedPaymentOrder;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [AssignedPaymentOrder] " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderUser] " +
            "ON [OutcomePaymentOrderUser].ID = [OutcomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] AS [OutcomePaymentOrderPaymentMovementOperation] " +
            "ON [OutcomePaymentOrder].ID = [OutcomePaymentOrderPaymentMovementOperation].OutcomePaymentOrderID " +
            "LEFT JOIN (" +
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
            ") AS [OutcomePaymentOrderPaymentMovement] " +
            "ON [OutcomePaymentOrderPaymentMovement].ID = [OutcomePaymentOrderPaymentMovementOperation].PaymentMovementID " +
            "LEFT JOIN [PaymentCurrencyRegister] AS [OutcomePaymentOrderPaymentCurrencyRegister] " +
            "ON [OutcomePaymentOrderPaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [OutcomePaymentOrderPaymentCurrencyRegisterCurrency] " +
            "ON [OutcomePaymentOrderPaymentCurrencyRegisterCurrency].ID = [OutcomePaymentOrderPaymentCurrencyRegister].CurrencyID " +
            "AND [OutcomePaymentOrderPaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] AS [OutcomePaymentOrderPaymentRegister] " +
            "ON [OutcomePaymentOrderPaymentRegister].ID = [OutcomePaymentOrderPaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [OutcomePaymentOrderPaymentRegisterOrganization] " +
            "ON [OutcomePaymentOrderPaymentRegisterOrganization].ID = [OutcomePaymentOrderPaymentRegister].OrganizationID " +
            "AND [OutcomePaymentOrderPaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [OutcomePaymentOrderOrganization] " +
            "ON [OutcomePaymentOrderOrganization].ID = [OutcomePaymentOrder].OrganizationID " +
            "AND [OutcomePaymentOrderOrganization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderColleague] " +
            "ON [OutcomePaymentOrderColleague].ID = [OutcomePaymentOrder].ColleagueID " +
            "LEFT JOIN [IncomePaymentOrder] " +
            "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].AssignedIncomePaymentOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [IncomePaymentOrder].ClientID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
            "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
            "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "AND [PaymentCurrencyRegister].CurrencyID = [Currency].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [PaymentCurrencyRegisterCurrency] " +
            "ON [PaymentCurrencyRegisterCurrency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [PaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [IncomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [IncomePaymentOrder].ID = [PaymentMovementOperation].IncomePaymentOrderID " +
            "LEFT JOIN (" +
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
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [IncomePaymentOrder].ColleagueID " +
            "WHERE [AssignedPaymentOrder].RootOutcomePaymentOrderID IN @Ids " +
            "AND [AssignedPaymentOrder].Deleted = 0",
            assignedPaymentOrderTypes,
            assignedPaymentOrderMapper,
            new {
                Ids = toReturn.Select(o => o.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        Type[] rootPaymentOrderTypes = {
            typeof(AssignedPaymentOrder),
            typeof(OutcomePaymentOrder),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(Organization),
            typeof(User),
            typeof(OutcomePaymentOrderConsumablesOrder),
            typeof(ConsumablesOrder),
            typeof(ConsumablesOrderItem),
            typeof(ConsumableProductCategory),
            typeof(ConsumableProduct),
            typeof(SupplyOrganization),
            typeof(IncomePaymentOrder),
            typeof(Client),
            typeof(Organization),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(User),
            typeof(IncomePaymentOrderSale),
            typeof(Sale),
            typeof(SaleNumber),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(MeasureUnit),
            typeof(CompanyCarFueling)
        };

        Func<object[], AssignedPaymentOrder> rootPaymentOrderMapper = objects => {
            AssignedPaymentOrder assignedPaymentOrder = (AssignedPaymentOrder)objects[0];
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[1];
            User outcomePaymentOrderUser = (User)objects[2];
            PaymentMovementOperation outcomePaymentOrderPaymentMovementOperation = (PaymentMovementOperation)objects[3];
            PaymentMovement outcomePaymentOrderPaymentMovement = (PaymentMovement)objects[4];
            PaymentCurrencyRegister outcomePaymentOrderPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency outcomePaymentOrderCurrency = (Currency)objects[6];
            PaymentRegister outcomePaymentOrderPaymentRegister = (PaymentRegister)objects[7];
            Organization outcomePaymentOrderPaymentRegisterOrganization = (Organization)objects[8];
            Organization outcomePaymentOrderOrganization = (Organization)objects[9];
            User outcomePaymentOrderColleague = (User)objects[10];
            OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[11];
            ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[12];
            ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[13];
            ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[14];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[15];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[16];
            IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[17];
            Client incomePaymentOrderClient = (Client)objects[18];
            Organization incomePaymentOrderOrganization = (Organization)objects[19];
            Currency incomePaymentOrderCurrency = (Currency)objects[20];
            PaymentRegister incomePaymentOrderPaymentRegister = (PaymentRegister)objects[21];
            Organization incomePaymentOrderPaymentRegisterOrganization = (Organization)objects[22];
            PaymentCurrencyRegister incomePaymentOrderPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[23];
            Currency incomePaymentOrderPaymentCurrencyRegisterCurrency = (Currency)objects[24];
            User incomePaymentOrderUser = (User)objects[25];
            PaymentMovementOperation incomePaymentOrderPaymentMovementOperation = (PaymentMovementOperation)objects[26];
            PaymentMovement incomePaymentOrderPaymentMovement = (PaymentMovement)objects[27];
            User incomePaymentOrderColleague = (User)objects[28];
            IncomePaymentOrderSale incomePaymentOrderSale = (IncomePaymentOrderSale)objects[29];
            Sale sale = (Sale)objects[30];
            SaleNumber saleNumber = (SaleNumber)objects[31];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[32];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[33];
            MeasureUnit measureUnit = (MeasureUnit)objects[34];
            CompanyCarFueling companyCarFueling = (CompanyCarFueling)objects[35];

            if (assignedPaymentOrder.AssignedOutcomePaymentOrderId == null) return assignedPaymentOrder;

            OutcomePaymentOrder fromList = toReturn.First(o => o.Id.Equals(assignedPaymentOrder.AssignedOutcomePaymentOrderId.Value));

            if (fromList.RootAssignedPaymentOrder == null) {
                if (outcomePaymentOrder != null) {
                    if (outcomePaymentOrderPaymentMovementOperation != null) outcomePaymentOrderPaymentMovementOperation.PaymentMovement = outcomePaymentOrderPaymentMovement;

                    if (outcomePaymentOrderConsumablesOrder != null) {
                        if (consumablesOrderItem != null) {
                            if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;

                            consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);
                        }

                        outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                        outcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                    }

                    if (companyCarFueling != null) outcomePaymentOrder.CompanyCarFuelings.Add(companyCarFueling);

                    outcomePaymentOrderPaymentRegister.Organization = outcomePaymentOrderPaymentRegisterOrganization;

                    outcomePaymentOrderPaymentCurrencyRegister.PaymentRegister = outcomePaymentOrderPaymentRegister;
                    outcomePaymentOrderPaymentCurrencyRegister.Currency = outcomePaymentOrderCurrency;

                    outcomePaymentOrder.User = outcomePaymentOrderUser;
                    outcomePaymentOrder.Colleague = outcomePaymentOrderColleague;
                    outcomePaymentOrder.PaymentMovementOperation = outcomePaymentOrderPaymentMovementOperation;
                    outcomePaymentOrder.Organization = outcomePaymentOrderOrganization;
                    outcomePaymentOrder.PaymentCurrencyRegister = outcomePaymentOrderPaymentCurrencyRegister;
                }

                if (incomePaymentOrder != null) {
                    if (incomePaymentOrderPaymentCurrencyRegister != null) {
                        incomePaymentOrderPaymentCurrencyRegister.Currency = incomePaymentOrderPaymentCurrencyRegisterCurrency;

                        incomePaymentOrderPaymentRegister.PaymentCurrencyRegisters.Add(incomePaymentOrderPaymentCurrencyRegister);
                    }

                    if (incomePaymentOrderPaymentMovementOperation != null) {
                        incomePaymentOrderPaymentMovementOperation.PaymentMovement = incomePaymentOrderPaymentMovement;

                        incomePaymentOrder.PaymentMovementOperation = incomePaymentOrderPaymentMovementOperation;
                    }

                    if (incomePaymentOrderSale != null) {
                        sale.SaleNumber = saleNumber;
                        sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                        sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                        incomePaymentOrderSale.Sale = sale;

                        incomePaymentOrder.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
                    }

                    incomePaymentOrderPaymentRegister.Organization = incomePaymentOrderPaymentRegisterOrganization;

                    incomePaymentOrder.Client = incomePaymentOrderClient;
                    incomePaymentOrder.Organization = incomePaymentOrderOrganization;
                    incomePaymentOrder.Currency = incomePaymentOrderCurrency;
                    incomePaymentOrder.PaymentRegister = incomePaymentOrderPaymentRegister;
                    incomePaymentOrder.User = incomePaymentOrderUser;
                    incomePaymentOrder.Colleague = incomePaymentOrderColleague;
                }

                assignedPaymentOrder.AssignedOutcomePaymentOrder = outcomePaymentOrder;
                assignedPaymentOrder.AssignedIncomePaymentOrder = incomePaymentOrder;

                fromList.RootAssignedPaymentOrder = assignedPaymentOrder;
            } else {
                if (outcomePaymentOrder != null && outcomePaymentOrderConsumablesOrder != null) {
                    if (fromList.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(o =>
                            o.Id.Equals(outcomePaymentOrderConsumablesOrder.Id))) {
                        if (consumablesOrderItem != null) {
                            OutcomePaymentOrderConsumablesOrder orderFromList =
                                fromList.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.First(o =>
                                    o.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));

                            if (!orderFromList.ConsumablesOrder.ConsumablesOrderItems.Any(i => i.Id.Equals(consumablesOrderItem.Id))) {
                                if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                                consumablesOrderItem.ConsumableProduct = consumableProduct;
                                consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                                consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;

                                orderFromList.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);
                            }
                        }
                    } else {
                        if (consumablesOrderItem != null) {
                            if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;

                            consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);
                        }

                        outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                        fromList.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                    }
                }

                if (companyCarFueling != null &&
                    !fromList.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.CompanyCarFuelings.Any(f => f.Id.Equals(companyCarFueling.Id)))
                    fromList.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.CompanyCarFuelings.Add(companyCarFueling);

                if (incomePaymentOrder == null || incomePaymentOrderSale == null) return assignedPaymentOrder;

                if (fromList.RootAssignedPaymentOrder.AssignedIncomePaymentOrder.IncomePaymentOrderSales.Any(s => s.Id.Equals(incomePaymentOrderSale.Id)))
                    return assignedPaymentOrder;

                sale.SaleNumber = saleNumber;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                incomePaymentOrderSale.Sale = sale;

                fromList.RootAssignedPaymentOrder.AssignedIncomePaymentOrder.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
            }

            return assignedPaymentOrder;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [AssignedPaymentOrder] " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].RootOutcomePaymentOrderID " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderUser] " +
            "ON [OutcomePaymentOrderUser].ID = [OutcomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] AS [OutcomePaymentOrderPaymentMovementOperation] " +
            "ON [OutcomePaymentOrder].ID = [OutcomePaymentOrderPaymentMovementOperation].OutcomePaymentOrderID " +
            "LEFT JOIN (" +
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
            ") AS [OutcomePaymentOrderPaymentMovement] " +
            "ON [OutcomePaymentOrderPaymentMovement].ID = [OutcomePaymentOrderPaymentMovementOperation].PaymentMovementID " +
            "LEFT JOIN [PaymentCurrencyRegister] AS [OutcomePaymentOrderPaymentCurrencyRegister] " +
            "ON [OutcomePaymentOrderPaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [OutcomePaymentOrderPaymentCurrencyRegisterCurrency] " +
            "ON [OutcomePaymentOrderPaymentCurrencyRegisterCurrency].ID = [OutcomePaymentOrderPaymentCurrencyRegister].CurrencyID " +
            "AND [OutcomePaymentOrderPaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] AS [OutcomePaymentOrderPaymentRegister] " +
            "ON [OutcomePaymentOrderPaymentRegister].ID = [OutcomePaymentOrderPaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [OutcomePaymentOrderPaymentRegisterOrganization] " +
            "ON [OutcomePaymentOrderPaymentRegisterOrganization].ID = [OutcomePaymentOrderPaymentRegister].OrganizationID " +
            "AND [OutcomePaymentOrderPaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [OutcomePaymentOrderOrganization] " +
            "ON [OutcomePaymentOrderOrganization].ID = [OutcomePaymentOrder].OrganizationID " +
            "AND [OutcomePaymentOrderOrganization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderColleague] " +
            "ON [OutcomePaymentOrderColleague].ID = [OutcomePaymentOrder].ColleagueID " +
            "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
            "ON [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
            "LEFT JOIN [ConsumablesOrder] " +
            "ON [ConsumablesOrder].ID = [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID " +
            "LEFT JOIN [ConsumablesOrderItem] " +
            "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "AND [ConsumablesOrderItem].Deleted = 0 " +
            "LEFT JOIN (" +
            "SELECT [ConsumableProductCategory].ID " +
            ", [ConsumableProductCategory].[Created] " +
            ", [ConsumableProductCategory].[Deleted] " +
            ", [ConsumableProductCategory].[NetUID] " +
            ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
            ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] "
            +
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
            "LEFT JOIN [IncomePaymentOrder] " +
            "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].RootIncomePaymentOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [IncomePaymentOrder].ClientID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
            "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
            "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [PaymentCurrencyRegisterCurrency] " +
            "ON [PaymentCurrencyRegisterCurrency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [PaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [IncomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [IncomePaymentOrder].ID = [PaymentMovementOperation].IncomePaymentOrderID " +
            "LEFT JOIN (" +
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
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [IncomePaymentOrder].ColleagueID " +
            "LEFT JOIN [IncomePaymentOrderSale] " +
            "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [IncomePaymentOrderSale].SaleID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON [BaseSalePaymentStatus].ID = [Sale].BaseSalePaymentStatusID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [CompanyCarFueling] " +
            "ON [CompanyCarFueling].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
            "WHERE [AssignedPaymentOrder].AssignedOutcomePaymentOrderID IN @Ids " +
            "AND [AssignedPaymentOrder].Deleted = 0",
            rootPaymentOrderTypes,
            rootPaymentOrderMapper,
            new {
                Ids = toReturn.Select(o => o.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        var joinProps = new { Ids = toReturn.Select(o => o.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        Type[] fuelingsTypes = {
            typeof(CompanyCarFueling),
            typeof(CompanyCar),
            typeof(User),
            typeof(User),
            typeof(SupplyOrganization),
            typeof(PaymentCostMovementOperation),
            typeof(PaymentCostMovement),
            typeof(User)
        };

        Func<object[], CompanyCarFueling> fuelingsMapper = objects => {
            CompanyCarFueling companyCarFueling = (CompanyCarFueling)objects[0];
            CompanyCar companyCar = (CompanyCar)objects[1];
            User createdBy = (User)objects[2];
            User updatedBy = (User)objects[3];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[4];
            PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[5];
            PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[6];
            User user = (User)objects[7];

            companyCar.CreatedBy = createdBy;
            companyCar.UpdatedBy = updatedBy;

            paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

            companyCarFueling.CompanyCar = companyCar;
            companyCarFueling.ConsumableProductOrganization = consumableProductOrganization;
            companyCarFueling.PaymentCostMovementOperation = paymentCostMovementOperation;
            companyCarFueling.User = user;

            OutcomePaymentOrder fromList = toReturn.First(o => o.Id.Equals(companyCarFueling.OutcomePaymentOrderId));

            fromList.CompanyCarFuelings.Add(companyCarFueling);

            fromList.AddedFuelAmount = Math.Round(fromList.AddedFuelAmount + companyCarFueling.FuelAmount, 2);

            return companyCarFueling;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [CompanyCarFueling] " +
            "LEFT JOIN [CompanyCar] " +
            "ON [CompanyCar].ID = [CompanyCarFueling].CompanyCarID " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [CompanyCar].CreatedByID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [UpdatedBy].ID = [CompanyCar].UpdatedByID " +
            "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
            "ON [ConsumableProductOrganization].ID = [CompanyCarFueling].ConsumableProductOrganizationID " +
            "LEFT JOIN [PaymentCostMovementOperation] " +
            "ON [PaymentCostMovementOperation].CompanyCarFuelingID = [CompanyCarFueling].ID " +
            "LEFT JOIN (" +
            "SELECT [PaymentCostMovement].ID " +
            ", [PaymentCostMovement].[Created] " +
            ", [PaymentCostMovement].[Deleted] " +
            ", [PaymentCostMovement].[NetUID] " +
            ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] "
            +
            ", [PaymentCostMovement].[Updated] " +
            "FROM [PaymentCostMovement] " +
            "LEFT JOIN [PaymentCostMovementTranslation] " +
            "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
            "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
            ") AS [PaymentCostMovement] " +
            "ON [PaymentCostMovement].ID = [PaymentCostMovementOperation].PaymentCostMovementID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [CompanyCarFueling].UserID " +
            "WHERE [CompanyCarFueling].Deleted = 0 " +
            "AND [CompanyCarFueling].OutcomePaymentOrderID IN @Ids",
            fuelingsTypes,
            fuelingsMapper,
            joinProps
        );

        Type[] roadListsTypes = {
            typeof(CompanyCarRoadList),
            typeof(CompanyCar),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User)
        };

        Func<object[], CompanyCarRoadList> roadListsMapper = objects => {
            CompanyCarRoadList roadList = (CompanyCarRoadList)objects[0];
            CompanyCar companyCar = (CompanyCar)objects[1];
            User responsible = (User)objects[2];
            User createdBy = (User)objects[3];
            User updatedBy = (User)objects[4];
            User carCreatedBy = (User)objects[5];
            User carUpdatedBy = (User)objects[6];

            companyCar.CreatedBy = carCreatedBy;
            companyCar.UpdatedBy = carUpdatedBy;

            roadList.CompanyCar = companyCar;
            roadList.Responsible = responsible;
            roadList.CreatedBy = createdBy;
            roadList.UpdatedBy = updatedBy;

            OutcomePaymentOrder fromList = toReturn.First(o => o.Id.Equals(roadList.OutcomePaymentOrderId));

            fromList.CompanyCarRoadLists.Add(roadList);

            fromList.SpentFuelAmount = Math.Round(fromList.AddedFuelAmount + roadList.FuelAmount, 2);

            return roadList;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [CompanyCarRoadList] " +
            "LEFT JOIN [CompanyCar] " +
            "ON [CompanyCar].ID = [CompanyCarRoadList].CompanyCarID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [CompanyCarRoadList].ResponsibleID " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [CompanyCarRoadList].CreatedByID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [UpdatedBy].ID = [CompanyCarRoadList].UpdatedByID " +
            "LEFT JOIN [User] AS [CarCreatedBy] " +
            "ON [CarCreatedBy].ID = [CompanyCar].CreatedByID " +
            "LEFT JOIN [User] AS [CarUpdatedBy] " +
            "ON [CarUpdatedBy].ID = [CompanyCar].UpdatedByID " +
            "WHERE [CompanyCarRoadList].Deleted = 0 " +
            "AND [CompanyCarRoadList].OutcomePaymentOrderID IN @Ids",
            roadListsTypes,
            roadListsMapper,
            joinProps
        );
        return new Tuple<IEnumerable<OutcomePaymentOrder>, decimal, decimal>(toReturn, positiveDifferenceAmount, negativeDifferenceAmount);
    }

    public Tuple<IEnumerable<OutcomePaymentOrder>, decimal, decimal> GetAllUnderReport(
        long limit,
        long offset,
        DateTime from,
        DateTime to,
        string value,
        Guid? currencyNetId,
        Guid? registerNetId,
        Guid? paymentMovementNetId) {
        List<OutcomePaymentOrder> toReturn = new();
        decimal positiveDifferenceAmount = decimal.Zero;
        decimal negativeDifferenceAmount = decimal.Zero;

        Type[] types = {
            typeof(OutcomePaymentOrder),
            typeof(Organization),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(OutcomePaymentOrderConsumablesOrder),
            typeof(ConsumablesOrder),
            typeof(ConsumablesOrderItem),
            typeof(ConsumableProductCategory),
            typeof(ConsumableProduct),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(UserRole),
            typeof(User),
            typeof(ConsumablesStorage),
            typeof(PaymentCostMovementOperation),
            typeof(PaymentCostMovement),
            typeof(MeasureUnit),
            typeof(SupplyOrganization),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency)
        };

        Func<object[], OutcomePaymentOrder> mapper = objects => {
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[0];
            Organization organization = (Organization)objects[1];
            User user = (User)objects[2];
            PaymentMovementOperation paymentMovementOperation = (PaymentMovementOperation)objects[3];
            PaymentMovement paymentMovement = (PaymentMovement)objects[4];
            PaymentCurrencyRegister paymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency currency = (Currency)objects[6];
            PaymentRegister paymentRegister = (PaymentRegister)objects[7];
            Organization paymentRegisterOrganization = (Organization)objects[8];
            OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[9];
            ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[10];
            ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[11];
            ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[12];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[13];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[14];
            User colleague = (User)objects[15];
            UserRole colleagueRole = (UserRole)objects[16];
            User consumablesOrderUser = (User)objects[17];
            ConsumablesStorage consumablesStorage = (ConsumablesStorage)objects[18];
            PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[19];
            PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[20];
            MeasureUnit measureUnit = (MeasureUnit)objects[21];
            SupplyOrganization outcomeConsumableProductOrganization = (SupplyOrganization)objects[22];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[23];
            Currency supplyOrganizationAgreementCurrency = (Currency)objects[24];
            SupplyOrganizationAgreement consumablesOrderSupplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[25];
            Currency consumablesOrderSupplyOrganizationAgreementCurrency = (Currency)objects[26];

            if (Math.Abs(outcomePaymentOrder.DifferenceAmount).Equals(outcomePaymentOrder.Amount)) outcomePaymentOrder.DifferenceAmount = decimal.Zero;

            if (!toReturn.Any(o => o.Id.Equals(outcomePaymentOrder.Id))) {
                outcomePaymentOrder.Amount = decimal.Zero;

                if (consumablesOrder != null && consumablesOrderItem != null) {
                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                    if (consumablesOrderSupplyOrganizationAgreement != null)
                        consumablesOrderSupplyOrganizationAgreement.Currency = consumablesOrderSupplyOrganizationAgreementCurrency;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.SupplyOrganizationAgreement = consumablesOrderSupplyOrganizationAgreement;

                    consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                    consumablesOrder.User = consumablesOrderUser;
                    consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrder.ConsumablesStorage = consumablesStorage;
                    consumablesOrder.SupplyOrganizationAgreement = consumablesOrderSupplyOrganizationAgreement;

                    consumablesOrder.TotalAmount = Math.Round(consumablesOrder.TotalAmount + consumablesOrderItem.TotalPriceWithVAT, 2);

                    consumablesOrder.TotalAmountWithoutVAT = Math.Round(consumablesOrder.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);

                    outcomePaymentOrder.Amount = consumablesOrder.TotalAmount;

                    outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                    outcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                }

                if (paymentMovementOperation != null) paymentMovementOperation.PaymentMovement = paymentMovement;

                if (colleagueRole != null) colleague.UserRole = colleagueRole;

                if (supplyOrganizationAgreement != null) supplyOrganizationAgreement.Currency = supplyOrganizationAgreementCurrency;

                paymentRegister.Organization = paymentRegisterOrganization;

                paymentCurrencyRegister.PaymentRegister = paymentRegister;
                paymentCurrencyRegister.Currency = currency;

                outcomePaymentOrder.Organization = organization;
                outcomePaymentOrder.PaymentCurrencyRegister = paymentCurrencyRegister;
                outcomePaymentOrder.User = user;
                outcomePaymentOrder.Colleague = colleague;
                outcomePaymentOrder.ConsumableProductOrganization = outcomeConsumableProductOrganization;
                outcomePaymentOrder.PaymentMovementOperation = paymentMovementOperation;
                outcomePaymentOrder.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                toReturn.Add(outcomePaymentOrder);

                if (outcomePaymentOrder.DifferenceAmount > decimal.Zero)
                    positiveDifferenceAmount = Math.Round(positiveDifferenceAmount + outcomePaymentOrder.DifferenceAmount, 2);
                else if (outcomePaymentOrder.DifferenceAmount < decimal.Zero)
                    negativeDifferenceAmount = Math.Round(negativeDifferenceAmount + outcomePaymentOrder.DifferenceAmount, 2);
            } else {
                OutcomePaymentOrder fromList = toReturn.First(o => o.Id.Equals(outcomePaymentOrder.Id));

                if (outcomePaymentOrderConsumablesOrder != null &&
                    fromList.OutcomePaymentOrderConsumablesOrders.Any(j => j.Id.Equals(outcomePaymentOrderConsumablesOrder.Id))) {
                    if (consumablesOrder == null || consumablesOrderItem == null) return outcomePaymentOrder;

                    OutcomePaymentOrderConsumablesOrder orderFromList =
                        fromList.OutcomePaymentOrderConsumablesOrders.First(j => j.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));

                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                    if (consumablesOrderSupplyOrganizationAgreement != null)
                        consumablesOrderSupplyOrganizationAgreement.Currency = consumablesOrderSupplyOrganizationAgreementCurrency;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.SupplyOrganizationAgreement = consumablesOrderSupplyOrganizationAgreement;

                    orderFromList.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                    orderFromList.ConsumablesOrder.TotalAmount =
                        Math.Round(orderFromList.ConsumablesOrder.TotalAmount + consumablesOrderItem.TotalPriceWithVAT, 2);

                    fromList.Amount = Math.Round(fromList.Amount + consumablesOrderItem.TotalPriceWithVAT, 2);

                    orderFromList.ConsumablesOrder.TotalAmountWithoutVAT =
                        Math.Round(orderFromList.ConsumablesOrder.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);
                } else {
                    if (consumablesOrder == null || consumablesOrderItem == null || outcomePaymentOrderConsumablesOrder == null) return outcomePaymentOrder;

                    if (paymentCostMovementOperation != null) paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

                    if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                    if (consumablesOrderSupplyOrganizationAgreement != null)
                        consumablesOrderSupplyOrganizationAgreement.Currency = consumablesOrderSupplyOrganizationAgreementCurrency;

                    consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                    consumablesOrderItem.ConsumableProduct = consumableProduct;
                    consumablesOrderItem.PaymentCostMovementOperation = paymentCostMovementOperation;
                    consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrderItem.SupplyOrganizationAgreement = consumablesOrderSupplyOrganizationAgreement;

                    consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);

                    consumablesOrder.User = consumablesOrderUser;
                    consumablesOrder.ConsumableProductOrganization = consumableProductOrganization;
                    consumablesOrder.ConsumablesStorage = consumablesStorage;
                    consumablesOrder.SupplyOrganizationAgreement = consumablesOrderSupplyOrganizationAgreement;

                    consumablesOrder.TotalAmount = Math.Round(consumablesOrder.TotalAmount + consumablesOrderItem.TotalPriceWithVAT, 2);

                    fromList.Amount = Math.Round(fromList.Amount + consumablesOrderItem.TotalPriceWithVAT, 2);

                    consumablesOrder.TotalAmountWithoutVAT = Math.Round(consumablesOrder.TotalAmountWithoutVAT + consumablesOrderItem.TotalPrice, 2);

                    outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                    fromList.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                }
            }

            return outcomePaymentOrder;
        };

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [OutcomePaymentOrder].FromDate DESC) AS RowNumber " +
            ", [OutcomePaymentOrder].ID " +
            ", COUNT(*) OVER() [TotalRowsQty] " +
            "FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [PaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
            "LEFT JOIN [PaymentMovement] " +
            "ON [PaymentMovement].ID = [PaymentMovementOperation].PaymentMovementID " +
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
            "WHERE [OutcomePaymentOrder].Deleted = 0 " +
            "AND [OutcomePaymentOrder].IsUnderReport = 1 " +
            "AND [OutcomePaymentOrder].FromDate >= @From " +
            "AND [OutcomePaymentOrder].FromDate <= @To " +
            "AND ( " +
            "[OutcomePaymentOrder].Amount like '%' + @Value + '%' " +
            "OR [OutcomePaymentOrder].Comment like '%' + @Value + '%' " +
            "OR [OutcomePaymentOrder].AdvanceNumber like '%' + @Value + '%' " +
            "OR [PaymentRegister].Name like '%' + @Value + '%' " +
            "OR [Colleague].LastName like '%' + @Value + '%' " +
            ") ";
        if (currencyNetId.HasValue) sqlExpression += "AND [Currency].NetUID = @CurrencyNetId ";

        if (registerNetId.HasValue) sqlExpression += "AND [PaymentRegister].NetUID = @RegisterNetId ";

        if (paymentMovementNetId.HasValue) sqlExpression += "AND [PaymentMovement].NetUID = @PaymentMovementNetId ";

        sqlExpression +=
            ") " +
            "SELECT [OutcomePaymentOrder].*, " +
            "(SELECT TOP 1 TotalRowsQty FROM [Search_CTE]) AS TotalRowsQty " +
            ", ( " +
            "SELECT ROUND( " +
            "( " +
            "SELECT (0 - [ForDifferenceOutcome].Amount) " +
            "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
            "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
            ") " +
            "+ " +
            "( " +
            "SELECT (ISNULL(SUM([ConsumablesOrderItem].TotalPriceWithVAT), 0)) " +
            "FROM [OutcomePaymentOrder] AS [ForDifferenceOutcome] " +
            "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
            "ON [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID = [ForDifferenceOutcome].ID " +
            "LEFT JOIN [ConsumablesOrder] " +
            "ON [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "LEFT JOIN [ConsumablesOrderItem] " +
            "ON [ConsumablesOrder].ID = [ConsumablesOrderItem].ConsumablesOrderID " +
            "WHERE [ForDifferenceOutcome].ID = [OutcomePaymentOrder].ID " +
            "AND [ForDifferenceOutcome].IsUnderReport = 1 " +
            ") " +
            "+ " +
            "( " +
            "SELECT (ISNULL(SUM([CompanyCarFueling].TotalPriceWithVat), 0)) " +
            "FROM [CompanyCarFueling] " +
            "WHERE [CompanyCarFueling].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
            "AND [CompanyCarFueling].Deleted = 0 " +
            ") " +
            "+ " +
            "( " +
            "SELECT ISNULL(SUM([AssignedIncome].Amount), 0) " +
            "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
            "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
            "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
            "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
            "LEFT JOIN [IncomePaymentOrder] AS [AssignedIncome] " +
            "ON [AssignedIncome].ID = [ForDifferenceAssignedPaymentOrder].AssignedIncomePaymentOrderID " +
            "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
            "AND [AssignedIncome].IsCanceled = 0" +
            ") " +
            "- " +
            "( " +
            "SELECT ISNULL(SUM([AssignedOutcome].Amount), 0) " +
            "FROM [OutcomePaymentOrder] AS [ForDifferenceRootOutcome] " +
            "LEFT JOIN [AssignedPaymentOrder] AS [ForDifferenceAssignedPaymentOrder] " +
            "ON [ForDifferenceAssignedPaymentOrder].RootOutcomePaymentOrderID = [ForDifferenceRootOutcome].ID " +
            "AND [ForDifferenceAssignedPaymentOrder].Deleted = 0 " +
            "LEFT JOIN [OutcomePaymentOrder] AS [AssignedOutcome] " +
            "ON [AssignedOutcome].ID = [ForDifferenceAssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
            "WHERE [ForDifferenceRootOutcome].ID = [OutcomePaymentOrder].ID " +
            ") " +
            ", 2) " +
            ") AS [DifferenceAmount] " +
            ", [Organization].*" +
            ", [User].*" +
            ", [PaymentMovementOperation].*" +
            ", [PaymentMovement].*" +
            ", [PaymentCurrencyRegister].*" +
            ", [Currency].*" +
            ", [PaymentRegister].*" +
            ", [PaymentRegisterOrganization].*" +
            ", [OutcomePaymentOrderConsumablesOrder].*" +
            ", [ConsumablesOrder].*" +
            ", [ConsumablesOrderItem].*" +
            ", [ConsumableProductCategory].*" +
            ", [ConsumableProduct].*" +
            ", [ConsumableProductOrganization].*" +
            ", [Colleague].*" +
            ", [ColleagueRole].*" +
            ", [ConsumablesOrderUser].*" +
            ", [ConsumablesStorage].*" +
            ", [PaymentCostMovementOperation].*" +
            ", [PaymentCostMovement].* " +
            ", [MeasureUnit].*" +
            ", [OutcomeConsumableProductOrganization].*" +
            ", [OutcomeAgreement].*" +
            ", [OutcomeAgreementCurrency].*" +
            ", [ConsumablesAgreement].*" +
            ", [ConsumablesAgreementCurrency].* " +
            "FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OutcomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [OutcomePaymentOrder].ID = [PaymentMovementOperation].OutcomePaymentOrderID " +
            "LEFT JOIN (" +
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
            "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
            "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
            "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
            "ON [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
            "LEFT JOIN [ConsumablesOrder] " +
            "ON [ConsumablesOrder].ID = [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID " +
            "LEFT JOIN [ConsumablesOrderItem] " +
            "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "AND [ConsumablesOrderItem].Deleted = 0 " +
            "LEFT JOIN (" +
            "SELECT [ConsumableProductCategory].ID " +
            ", [ConsumableProductCategory].[Created] " +
            ", [ConsumableProductCategory].[Deleted] " +
            ", [ConsumableProductCategory].[NetUID] " +
            ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
            ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] "
            +
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
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [OutcomePaymentOrder].ColleagueID " +
            "LEFT JOIN (" +
            "SELECT [UserRole].ID " +
            ",[UserRole].[Created] " +
            ",[UserRole].[Dashboard] " +
            ",[UserRole].[Deleted] " +
            ",(CASE WHEN [UserRoleTranslation].[Name] IS NOT NULL THEN [UserRoleTranslation].[Name] ELSE [UserRole].[Name] END) AS [Name] " +
            ",[UserRole].[NetUID] ,[UserRole].[Updated] " +
            ",[UserRole].[UserRoleType] " +
            "FROM [UserRole] " +
            "LEFT JOIN [UserRoleTranslation] " +
            "ON [UserRoleTranslation].UserRoleID = [UserRole].ID " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            "AND [UserRoleTranslation].Deleted = 0" +
            ") AS [ColleagueRole] " +
            "ON [Colleague].UserRoleID = [ColleagueRole].ID " +
            "LEFT JOIN [User] AS [ConsumablesOrderUser] " +
            "ON [ConsumablesOrderUser].ID = [ConsumablesOrder].UserID " +
            "LEFT JOIN [ConsumablesStorage] " +
            "ON [ConsumablesStorage].ID = [ConsumablesOrder].ConsumablesStorageID " +
            "LEFT JOIN [PaymentCostMovementOperation] " +
            "ON [PaymentCostMovementOperation].ConsumablesOrderItemID = [ConsumablesOrderItem].ID " +
            "LEFT JOIN (" +
            "SELECT [PaymentCostMovement].ID " +
            ", [PaymentCostMovement].[Created] " +
            ", [PaymentCostMovement].[Deleted] " +
            ", [PaymentCostMovement].[NetUID] " +
            ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] "
            +
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
            "LEFT JOIN [SupplyOrganization] AS [OutcomeConsumableProductOrganization] " +
            "ON [OutcomePaymentOrder].ConsumableProductOrganizationID = [OutcomeConsumableProductOrganization].ID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [OutcomeAgreement] " +
            "ON [OutcomeAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [OutcomeAgreementCurrency] " +
            "ON [OutcomeAgreementCurrency].ID = [OutcomeAgreement].CurrencyID " +
            "AND [OutcomeAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [ConsumablesAgreement] " +
            "ON [ConsumablesAgreement].ID = [ConsumablesOrderItem].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [ConsumablesAgreementCurrency] " +
            "ON [ConsumablesAgreementCurrency].ID = [ConsumablesAgreement].CurrencyID " +
            "AND [ConsumablesAgreementCurrency].CultureCode = @Culture " +
            "WHERE [OutcomePaymentOrder].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ") " +
            "ORDER BY [OutcomePaymentOrder].FromDate DESC";
        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                Limit = limit,
                Offset = offset,
                CurrencyNetId = currencyNetId ?? Guid.Empty,
                RegisterNetId = registerNetId ?? Guid.Empty,
                PaymentMovementNetId = paymentMovementNetId ?? Guid.Empty,
                Value = value,
                From = from,
                To = to,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );
        if (!toReturn.Any()) return new Tuple<IEnumerable<OutcomePaymentOrder>, decimal, decimal>(toReturn, positiveDifferenceAmount, negativeDifferenceAmount);

        Type[] assignedPaymentOrderTypes = {
            typeof(AssignedPaymentOrder),
            typeof(OutcomePaymentOrder),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(Organization),
            typeof(User),
            typeof(IncomePaymentOrder),
            typeof(Client),
            typeof(Organization),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(User)
        };

        Func<object[], AssignedPaymentOrder> assignedPaymentOrderMapper = objects => {
            AssignedPaymentOrder assignedPaymentOrder = (AssignedPaymentOrder)objects[0];
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[1];

            User outcomePaymentOrderUser = (User)objects[2];
            PaymentMovementOperation outcomePaymentOrderPaymentMovementOperation = (PaymentMovementOperation)objects[3];
            PaymentMovement outcomePaymentOrderPaymentMovement = (PaymentMovement)objects[4];
            PaymentCurrencyRegister outcomePaymentOrderPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency outcomePaymentOrderCurrency = (Currency)objects[6];
            PaymentRegister outcomePaymentOrderPaymentRegister = (PaymentRegister)objects[7];
            Organization outcomePaymentOrderPaymentRegisterOrganization = (Organization)objects[8];
            Organization outcomePaymentOrderOrganization = (Organization)objects[9];
            User outcomePaymentOrderColleague = (User)objects[10];

            IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[11];

            Client incomePaymentOrderClient = (Client)objects[12];
            Organization incomePaymentOrderOrganization = (Organization)objects[13];
            Currency incomePaymentOrderCurrency = (Currency)objects[14];
            PaymentRegister incomePaymentOrderPaymentRegister = (PaymentRegister)objects[15];
            Organization incomePaymentOrderPaymentRegisterOrganization = (Organization)objects[16];
            PaymentCurrencyRegister incomePaymentOrderPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[17];
            Currency incomePaymentOrderPaymentCurrencyRegisterCurrency = (Currency)objects[18];
            User incomePaymentOrderUser = (User)objects[19];
            PaymentMovementOperation incomePaymentOrderPaymentMovementOperation = (PaymentMovementOperation)objects[20];
            PaymentMovement incomePaymentOrderPaymentMovement = (PaymentMovement)objects[21];
            User incomePaymentOrderColleague = (User)objects[22];

            if (outcomePaymentOrder != null) {
                if (outcomePaymentOrderPaymentMovementOperation != null) outcomePaymentOrderPaymentMovementOperation.PaymentMovement = outcomePaymentOrderPaymentMovement;

                outcomePaymentOrderPaymentRegister.Organization = outcomePaymentOrderPaymentRegisterOrganization;

                outcomePaymentOrderPaymentCurrencyRegister.PaymentRegister = outcomePaymentOrderPaymentRegister;
                outcomePaymentOrderPaymentCurrencyRegister.Currency = outcomePaymentOrderCurrency;

                outcomePaymentOrder.User = outcomePaymentOrderUser;
                outcomePaymentOrder.Colleague = outcomePaymentOrderColleague;
                outcomePaymentOrder.PaymentMovementOperation = outcomePaymentOrderPaymentMovementOperation;
                outcomePaymentOrder.Organization = outcomePaymentOrderOrganization;
                outcomePaymentOrder.PaymentCurrencyRegister = outcomePaymentOrderPaymentCurrencyRegister;
            }

            if (incomePaymentOrder != null) {
                if (incomePaymentOrderPaymentCurrencyRegister != null) {
                    incomePaymentOrderPaymentCurrencyRegister.Currency = incomePaymentOrderPaymentCurrencyRegisterCurrency;

                    incomePaymentOrderPaymentRegister.PaymentCurrencyRegisters.Add(incomePaymentOrderPaymentCurrencyRegister);
                }

                if (incomePaymentOrderPaymentMovementOperation != null) {
                    incomePaymentOrderPaymentMovementOperation.PaymentMovement = incomePaymentOrderPaymentMovement;

                    incomePaymentOrder.PaymentMovementOperation = incomePaymentOrderPaymentMovementOperation;
                }

                incomePaymentOrderPaymentRegister.Organization = incomePaymentOrderPaymentRegisterOrganization;

                incomePaymentOrder.Client = incomePaymentOrderClient;
                incomePaymentOrder.Organization = incomePaymentOrderOrganization;
                incomePaymentOrder.Currency = incomePaymentOrderCurrency;
                incomePaymentOrder.PaymentRegister = incomePaymentOrderPaymentRegister;
                incomePaymentOrder.User = incomePaymentOrderUser;
                incomePaymentOrder.Colleague = incomePaymentOrderColleague;
            }

            assignedPaymentOrder.AssignedOutcomePaymentOrder = outcomePaymentOrder;
            assignedPaymentOrder.AssignedIncomePaymentOrder = incomePaymentOrder;

            if (assignedPaymentOrder.RootOutcomePaymentOrderId != null)
                toReturn.First(o => o.Id.Equals(assignedPaymentOrder.RootOutcomePaymentOrderId.Value)).AssignedPaymentOrders.Add(assignedPaymentOrder);

            return assignedPaymentOrder;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [AssignedPaymentOrder] " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].AssignedOutcomePaymentOrderID " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderUser] " +
            "ON [OutcomePaymentOrderUser].ID = [OutcomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] AS [OutcomePaymentOrderPaymentMovementOperation] " +
            "ON [OutcomePaymentOrder].ID = [OutcomePaymentOrderPaymentMovementOperation].OutcomePaymentOrderID " +
            "LEFT JOIN (" +
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
            ") AS [OutcomePaymentOrderPaymentMovement] " +
            "ON [OutcomePaymentOrderPaymentMovement].ID = [OutcomePaymentOrderPaymentMovementOperation].PaymentMovementID " +
            "LEFT JOIN [PaymentCurrencyRegister] AS [OutcomePaymentOrderPaymentCurrencyRegister] " +
            "ON [OutcomePaymentOrderPaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [OutcomePaymentOrderPaymentCurrencyRegisterCurrency] " +
            "ON [OutcomePaymentOrderPaymentCurrencyRegisterCurrency].ID = [OutcomePaymentOrderPaymentCurrencyRegister].CurrencyID " +
            "AND [OutcomePaymentOrderPaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] AS [OutcomePaymentOrderPaymentRegister] " +
            "ON [OutcomePaymentOrderPaymentRegister].ID = [OutcomePaymentOrderPaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [OutcomePaymentOrderPaymentRegisterOrganization] " +
            "ON [OutcomePaymentOrderPaymentRegisterOrganization].ID = [OutcomePaymentOrderPaymentRegister].OrganizationID " +
            "AND [OutcomePaymentOrderPaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [OutcomePaymentOrderOrganization] " +
            "ON [OutcomePaymentOrderOrganization].ID = [OutcomePaymentOrder].OrganizationID " +
            "AND [OutcomePaymentOrderOrganization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderColleague] " +
            "ON [OutcomePaymentOrderColleague].ID = [OutcomePaymentOrder].ColleagueID " +
            "LEFT JOIN [IncomePaymentOrder] " +
            "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].AssignedIncomePaymentOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [IncomePaymentOrder].ClientID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
            "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
            "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "AND [PaymentCurrencyRegister].CurrencyID = [Currency].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [PaymentCurrencyRegisterCurrency] " +
            "ON [PaymentCurrencyRegisterCurrency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [PaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [IncomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [IncomePaymentOrder].ID = [PaymentMovementOperation].IncomePaymentOrderID " +
            "LEFT JOIN (" +
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
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [IncomePaymentOrder].ColleagueID " +
            "WHERE [AssignedPaymentOrder].RootOutcomePaymentOrderID IN @Ids " +
            "AND [AssignedPaymentOrder].Deleted = 0",
            assignedPaymentOrderTypes,
            assignedPaymentOrderMapper,
            new {
                Ids = toReturn.Select(o => o.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        Type[] rootPaymentOrderTypes = {
            typeof(AssignedPaymentOrder),
            typeof(OutcomePaymentOrder),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(Organization),
            typeof(User),
            typeof(OutcomePaymentOrderConsumablesOrder),
            typeof(ConsumablesOrder),
            typeof(ConsumablesOrderItem),
            typeof(ConsumableProductCategory),
            typeof(ConsumableProduct),
            typeof(SupplyOrganization),
            typeof(IncomePaymentOrder),
            typeof(Client),
            typeof(Organization),
            typeof(Currency),
            typeof(PaymentRegister),
            typeof(Organization),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(User),
            typeof(PaymentMovementOperation),
            typeof(PaymentMovement),
            typeof(User),
            typeof(IncomePaymentOrderSale),
            typeof(Sale),
            typeof(SaleNumber),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(MeasureUnit),
            typeof(CompanyCarFueling)
        };

        Func<object[], AssignedPaymentOrder> rootPaymentOrderMapper = objects => {
            AssignedPaymentOrder assignedPaymentOrder = (AssignedPaymentOrder)objects[0];
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[1];
            User outcomePaymentOrderUser = (User)objects[2];
            PaymentMovementOperation outcomePaymentOrderPaymentMovementOperation = (PaymentMovementOperation)objects[3];
            PaymentMovement outcomePaymentOrderPaymentMovement = (PaymentMovement)objects[4];
            PaymentCurrencyRegister outcomePaymentOrderPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[5];
            Currency outcomePaymentOrderCurrency = (Currency)objects[6];
            PaymentRegister outcomePaymentOrderPaymentRegister = (PaymentRegister)objects[7];
            Organization outcomePaymentOrderPaymentRegisterOrganization = (Organization)objects[8];
            Organization outcomePaymentOrderOrganization = (Organization)objects[9];
            User outcomePaymentOrderColleague = (User)objects[10];
            OutcomePaymentOrderConsumablesOrder outcomePaymentOrderConsumablesOrder = (OutcomePaymentOrderConsumablesOrder)objects[11];
            ConsumablesOrder consumablesOrder = (ConsumablesOrder)objects[12];
            ConsumablesOrderItem consumablesOrderItem = (ConsumablesOrderItem)objects[13];
            ConsumableProductCategory consumableProductCategory = (ConsumableProductCategory)objects[14];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[15];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[16];
            IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[17];
            Client incomePaymentOrderClient = (Client)objects[18];
            Organization incomePaymentOrderOrganization = (Organization)objects[19];
            Currency incomePaymentOrderCurrency = (Currency)objects[20];
            PaymentRegister incomePaymentOrderPaymentRegister = (PaymentRegister)objects[21];
            Organization incomePaymentOrderPaymentRegisterOrganization = (Organization)objects[22];
            PaymentCurrencyRegister incomePaymentOrderPaymentCurrencyRegister = (PaymentCurrencyRegister)objects[23];
            Currency incomePaymentOrderPaymentCurrencyRegisterCurrency = (Currency)objects[24];
            User incomePaymentOrderUser = (User)objects[25];
            PaymentMovementOperation incomePaymentOrderPaymentMovementOperation = (PaymentMovementOperation)objects[26];
            PaymentMovement incomePaymentOrderPaymentMovement = (PaymentMovement)objects[27];
            User incomePaymentOrderColleague = (User)objects[28];
            IncomePaymentOrderSale incomePaymentOrderSale = (IncomePaymentOrderSale)objects[29];
            Sale sale = (Sale)objects[30];
            SaleNumber saleNumber = (SaleNumber)objects[31];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[32];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[33];
            MeasureUnit measureUnit = (MeasureUnit)objects[34];
            CompanyCarFueling companyCarFueling = (CompanyCarFueling)objects[35];

            if (assignedPaymentOrder.AssignedOutcomePaymentOrderId == null) return assignedPaymentOrder;

            OutcomePaymentOrder fromList = toReturn.First(o => o.Id.Equals(assignedPaymentOrder.AssignedOutcomePaymentOrderId.Value));

            if (fromList.RootAssignedPaymentOrder == null) {
                if (outcomePaymentOrder != null) {
                    if (outcomePaymentOrderPaymentMovementOperation != null) outcomePaymentOrderPaymentMovementOperation.PaymentMovement = outcomePaymentOrderPaymentMovement;

                    if (outcomePaymentOrderConsumablesOrder != null) {
                        if (consumablesOrderItem != null) {
                            if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;

                            consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);
                        }

                        outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                        outcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                    }

                    if (companyCarFueling != null) outcomePaymentOrder.CompanyCarFuelings.Add(companyCarFueling);

                    outcomePaymentOrderPaymentRegister.Organization = outcomePaymentOrderPaymentRegisterOrganization;

                    outcomePaymentOrderPaymentCurrencyRegister.PaymentRegister = outcomePaymentOrderPaymentRegister;
                    outcomePaymentOrderPaymentCurrencyRegister.Currency = outcomePaymentOrderCurrency;

                    outcomePaymentOrder.User = outcomePaymentOrderUser;
                    outcomePaymentOrder.Colleague = outcomePaymentOrderColleague;
                    outcomePaymentOrder.PaymentMovementOperation = outcomePaymentOrderPaymentMovementOperation;
                    outcomePaymentOrder.Organization = outcomePaymentOrderOrganization;
                    outcomePaymentOrder.PaymentCurrencyRegister = outcomePaymentOrderPaymentCurrencyRegister;
                }

                if (incomePaymentOrder != null) {
                    if (incomePaymentOrderPaymentCurrencyRegister != null) {
                        incomePaymentOrderPaymentCurrencyRegister.Currency = incomePaymentOrderPaymentCurrencyRegisterCurrency;

                        incomePaymentOrderPaymentRegister.PaymentCurrencyRegisters.Add(incomePaymentOrderPaymentCurrencyRegister);
                    }

                    if (incomePaymentOrderPaymentMovementOperation != null) {
                        incomePaymentOrderPaymentMovementOperation.PaymentMovement = incomePaymentOrderPaymentMovement;

                        incomePaymentOrder.PaymentMovementOperation = incomePaymentOrderPaymentMovementOperation;
                    }

                    if (incomePaymentOrderSale != null) {
                        sale.SaleNumber = saleNumber;
                        sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                        sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                        incomePaymentOrderSale.Sale = sale;

                        incomePaymentOrder.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
                    }

                    incomePaymentOrderPaymentRegister.Organization = incomePaymentOrderPaymentRegisterOrganization;

                    incomePaymentOrder.Client = incomePaymentOrderClient;
                    incomePaymentOrder.Organization = incomePaymentOrderOrganization;
                    incomePaymentOrder.Currency = incomePaymentOrderCurrency;
                    incomePaymentOrder.PaymentRegister = incomePaymentOrderPaymentRegister;
                    incomePaymentOrder.User = incomePaymentOrderUser;
                    incomePaymentOrder.Colleague = incomePaymentOrderColleague;
                }

                assignedPaymentOrder.AssignedOutcomePaymentOrder = outcomePaymentOrder;
                assignedPaymentOrder.AssignedIncomePaymentOrder = incomePaymentOrder;

                fromList.RootAssignedPaymentOrder = assignedPaymentOrder;
            } else {
                if (outcomePaymentOrder != null && outcomePaymentOrderConsumablesOrder != null) {
                    if (fromList.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Any(o =>
                            o.Id.Equals(outcomePaymentOrderConsumablesOrder.Id))) {
                        if (consumablesOrderItem != null) {
                            OutcomePaymentOrderConsumablesOrder orderFromList =
                                fromList.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.First(o =>
                                    o.Id.Equals(outcomePaymentOrderConsumablesOrder.Id));

                            if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;

                            orderFromList.ConsumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);
                        }
                    } else {
                        if (consumablesOrderItem != null) {
                            if (consumableProduct != null) consumableProduct.MeasureUnit = measureUnit;

                            consumablesOrderItem.ConsumableProduct = consumableProduct;
                            consumablesOrderItem.ConsumableProductCategory = consumableProductCategory;
                            consumablesOrderItem.ConsumableProductOrganization = consumableProductOrganization;

                            consumablesOrder.ConsumablesOrderItems.Add(consumablesOrderItem);
                        }

                        outcomePaymentOrderConsumablesOrder.ConsumablesOrder = consumablesOrder;

                        fromList.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.OutcomePaymentOrderConsumablesOrders.Add(outcomePaymentOrderConsumablesOrder);
                    }

                    if (companyCarFueling != null &&
                        !fromList.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.CompanyCarFuelings.Any(f => f.Id.Equals(companyCarFueling.Id)))
                        fromList.RootAssignedPaymentOrder.AssignedOutcomePaymentOrder.CompanyCarFuelings.Add(companyCarFueling);
                }

                if (incomePaymentOrder == null || incomePaymentOrderSale == null) return assignedPaymentOrder;

                if (fromList.RootAssignedPaymentOrder.AssignedIncomePaymentOrder.IncomePaymentOrderSales.Any(s => s.Id.Equals(incomePaymentOrderSale.Id)))
                    return assignedPaymentOrder;

                sale.SaleNumber = saleNumber;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                sale.BaseSalePaymentStatus = baseSalePaymentStatus;

                incomePaymentOrderSale.Sale = sale;

                fromList.RootAssignedPaymentOrder.AssignedIncomePaymentOrder.IncomePaymentOrderSales.Add(incomePaymentOrderSale);
            }

            return assignedPaymentOrder;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [AssignedPaymentOrder] " +
            "LEFT JOIN [OutcomePaymentOrder] " +
            "ON [OutcomePaymentOrder].ID = [AssignedPaymentOrder].RootOutcomePaymentOrderID " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderUser] " +
            "ON [OutcomePaymentOrderUser].ID = [OutcomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] AS [OutcomePaymentOrderPaymentMovementOperation] " +
            "ON [OutcomePaymentOrder].ID = [OutcomePaymentOrderPaymentMovementOperation].OutcomePaymentOrderID " +
            "LEFT JOIN (" +
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
            ") AS [OutcomePaymentOrderPaymentMovement] " +
            "ON [OutcomePaymentOrderPaymentMovement].ID = [OutcomePaymentOrderPaymentMovementOperation].PaymentMovementID " +
            "LEFT JOIN [PaymentCurrencyRegister] AS [OutcomePaymentOrderPaymentCurrencyRegister] " +
            "ON [OutcomePaymentOrderPaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
            "LEFT JOIN [views].[CurrencyView] AS [OutcomePaymentOrderPaymentCurrencyRegisterCurrency] " +
            "ON [OutcomePaymentOrderPaymentCurrencyRegisterCurrency].ID = [OutcomePaymentOrderPaymentCurrencyRegister].CurrencyID " +
            "AND [OutcomePaymentOrderPaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] AS [OutcomePaymentOrderPaymentRegister] " +
            "ON [OutcomePaymentOrderPaymentRegister].ID = [OutcomePaymentOrderPaymentCurrencyRegister].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [OutcomePaymentOrderPaymentRegisterOrganization] " +
            "ON [OutcomePaymentOrderPaymentRegisterOrganization].ID = [OutcomePaymentOrderPaymentRegister].OrganizationID " +
            "AND [OutcomePaymentOrderPaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [OutcomePaymentOrderOrganization] " +
            "ON [OutcomePaymentOrderOrganization].ID = [OutcomePaymentOrder].OrganizationID " +
            "AND [OutcomePaymentOrderOrganization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [OutcomePaymentOrderColleague] " +
            "ON [OutcomePaymentOrderColleague].ID = [OutcomePaymentOrder].ColleagueID " +
            "LEFT JOIN [OutcomePaymentOrderConsumablesOrder] " +
            "ON [OutcomePaymentOrderConsumablesOrder].OutcomePaymentOrderID = [OutcomePaymentOrder].ID " +
            "LEFT JOIN [ConsumablesOrder] " +
            "ON [ConsumablesOrder].ID = [OutcomePaymentOrderConsumablesOrder].ConsumablesOrderID " +
            "LEFT JOIN [ConsumablesOrderItem] " +
            "ON [ConsumablesOrderItem].ConsumablesOrderID = [ConsumablesOrder].ID " +
            "AND [ConsumablesOrderItem].Deleted = 0 " +
            "LEFT JOIN (" +
            "SELECT [ConsumableProductCategory].ID " +
            ", [ConsumableProductCategory].[Created] " +
            ", [ConsumableProductCategory].[Deleted] " +
            ", [ConsumableProductCategory].[NetUID] " +
            ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
            ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] "
            +
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
            "LEFT JOIN [IncomePaymentOrder] " +
            "ON [IncomePaymentOrder].ID = [AssignedPaymentOrder].RootIncomePaymentOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [IncomePaymentOrder].ClientID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [IncomePaymentOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].ID = [IncomePaymentOrder].PaymentRegisterID " +
            "LEFT JOIN [views].[OrganizationView] AS [PaymentRegisterOrganization] " +
            "ON [PaymentRegisterOrganization].ID = [PaymentRegister].OrganizationID " +
            "AND [PaymentRegisterOrganization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [PaymentCurrencyRegisterCurrency] " +
            "ON [PaymentCurrencyRegisterCurrency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [PaymentCurrencyRegisterCurrency].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [IncomePaymentOrder].UserID " +
            "LEFT JOIN [PaymentMovementOperation] " +
            "ON [IncomePaymentOrder].ID = [PaymentMovementOperation].IncomePaymentOrderID " +
            "LEFT JOIN (" +
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
            "LEFT JOIN [User] AS [Colleague] " +
            "ON [Colleague].ID = [IncomePaymentOrder].ColleagueID " +
            "LEFT JOIN [IncomePaymentOrderSale] " +
            "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [IncomePaymentOrderSale].SaleID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON [BaseSalePaymentStatus].ID = [Sale].BaseSalePaymentStatusID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [CompanyCarFueling] " +
            "ON [OutcomePaymentOrder].ID = [CompanyCarFueling].OutcomePaymentOrderID " +
            "WHERE [AssignedPaymentOrder].AssignedOutcomePaymentOrderID IN @Ids " +
            "AND [AssignedPaymentOrder].Deleted = 0",
            rootPaymentOrderTypes,
            rootPaymentOrderMapper,
            new {
                Ids = toReturn.Select(o => o.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        var joinProps = new { Ids = toReturn.Select(o => o.Id), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        Type[] fuelingsTypes = {
            typeof(CompanyCarFueling),
            typeof(CompanyCar),
            typeof(User),
            typeof(User),
            typeof(SupplyOrganization),
            typeof(PaymentCostMovementOperation),
            typeof(PaymentCostMovement),
            typeof(User)
        };

        Func<object[], CompanyCarFueling> fuelingsMapper = objects => {
            CompanyCarFueling companyCarFueling = (CompanyCarFueling)objects[0];
            CompanyCar companyCar = (CompanyCar)objects[1];
            User createdBy = (User)objects[2];
            User updatedBy = (User)objects[3];
            SupplyOrganization consumableProductOrganization = (SupplyOrganization)objects[4];
            PaymentCostMovementOperation paymentCostMovementOperation = (PaymentCostMovementOperation)objects[5];
            PaymentCostMovement paymentCostMovement = (PaymentCostMovement)objects[6];
            User user = (User)objects[7];

            companyCar.CreatedBy = createdBy;
            companyCar.UpdatedBy = updatedBy;

            paymentCostMovementOperation.PaymentCostMovement = paymentCostMovement;

            companyCarFueling.CompanyCar = companyCar;
            companyCarFueling.ConsumableProductOrganization = consumableProductOrganization;
            companyCarFueling.PaymentCostMovementOperation = paymentCostMovementOperation;
            companyCarFueling.User = user;

            OutcomePaymentOrder fromList = toReturn.First(o => o.Id.Equals(companyCarFueling.OutcomePaymentOrderId));

            fromList.CompanyCarFuelings.Add(companyCarFueling);

            fromList.AddedFuelAmount = Math.Round(fromList.AddedFuelAmount + companyCarFueling.FuelAmount, 2);

            fromList.Amount = Math.Round(fromList.Amount + companyCarFueling.TotalPriceWithVat, 2);

            return companyCarFueling;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [CompanyCarFueling] " +
            "LEFT JOIN [CompanyCar] " +
            "ON [CompanyCar].ID = [CompanyCarFueling].CompanyCarID " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [CompanyCar].CreatedByID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [UpdatedBy].ID = [CompanyCar].UpdatedByID " +
            "LEFT JOIN [SupplyOrganization] AS [ConsumableProductOrganization] " +
            "ON [ConsumableProductOrganization].ID = [CompanyCarFueling].ConsumableProductOrganizationID " +
            "LEFT JOIN [PaymentCostMovementOperation] " +
            "ON [PaymentCostMovementOperation].CompanyCarFuelingID = [CompanyCarFueling].ID " +
            "LEFT JOIN (" +
            "SELECT [PaymentCostMovement].ID " +
            ", [PaymentCostMovement].[Created] " +
            ", [PaymentCostMovement].[Deleted] " +
            ", [PaymentCostMovement].[NetUID] " +
            ", (CASE WHEN [PaymentCostMovementTranslation].[OperationName] IS NOT NULL THEN [PaymentCostMovementTranslation].[OperationName] ELSE [PaymentCostMovement].[OperationName] END) AS [OperationName] "
            +
            ", [PaymentCostMovement].[Updated] " +
            "FROM [PaymentCostMovement] " +
            "LEFT JOIN [PaymentCostMovementTranslation] " +
            "ON [PaymentCostMovementTranslation].PaymentCostMovementID = [PaymentCostMovement].ID " +
            "AND [PaymentCostMovementTranslation].CultureCode = @Culture " +
            ") AS [PaymentCostMovement] " +
            "ON [PaymentCostMovement].ID = [PaymentCostMovementOperation].PaymentCostMovementID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [CompanyCarFueling].UserID " +
            "WHERE [CompanyCarFueling].Deleted = 0 " +
            "AND [CompanyCarFueling].OutcomePaymentOrderID IN @Ids",
            fuelingsTypes,
            fuelingsMapper,
            joinProps
        );

        Type[] roadListsTypes = {
            typeof(CompanyCarRoadList),
            typeof(CompanyCar),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User)
        };

        Func<object[], CompanyCarRoadList> roadListsMapper = objects => {
            CompanyCarRoadList roadList = (CompanyCarRoadList)objects[0];
            CompanyCar companyCar = (CompanyCar)objects[1];
            User responsible = (User)objects[2];
            User createdBy = (User)objects[3];
            User updatedBy = (User)objects[4];
            User carCreatedBy = (User)objects[5];
            User carUpdatedBy = (User)objects[6];

            companyCar.CreatedBy = carCreatedBy;
            companyCar.UpdatedBy = carUpdatedBy;

            roadList.CompanyCar = companyCar;
            roadList.Responsible = responsible;
            roadList.CreatedBy = createdBy;
            roadList.UpdatedBy = updatedBy;

            OutcomePaymentOrder fromList = toReturn.First(o => o.Id.Equals(roadList.OutcomePaymentOrderId));

            fromList.CompanyCarRoadLists.Add(roadList);

            fromList.SpentFuelAmount = Math.Round(fromList.AddedFuelAmount + roadList.FuelAmount, 2);

            return roadList;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [CompanyCarRoadList] " +
            "LEFT JOIN [CompanyCar] " +
            "ON [CompanyCar].ID = [CompanyCarRoadList].CompanyCarID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [CompanyCarRoadList].ResponsibleID " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [CompanyCarRoadList].CreatedByID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [UpdatedBy].ID = [CompanyCarRoadList].UpdatedByID " +
            "LEFT JOIN [User] AS [CarCreatedBy] " +
            "ON [CarCreatedBy].ID = [CompanyCar].CreatedByID " +
            "LEFT JOIN [User] AS [CarUpdatedBy] " +
            "ON [CarUpdatedBy].ID = [CompanyCar].UpdatedByID " +
            "WHERE [CompanyCarRoadList].Deleted = 0 " +
            "AND [CompanyCarRoadList].OutcomePaymentOrderID IN @Ids",
            roadListsTypes,
            roadListsMapper,
            joinProps
        );
        return new Tuple<IEnumerable<OutcomePaymentOrder>, decimal, decimal>(toReturn, positiveDifferenceAmount, negativeDifferenceAmount);
    }

    public TotalDashboardItem GetTotalsAmountByDayAndCurrentMonth() {
        TotalDashboardItem toReturn = new();
        _connection.Query<TotalItem, TotalItem, TotalItem>(
            "DECLARE @OUTCOME_LIST_TABLE TABLE ( " +
            "[Amount] money, " +
            "[Created] datetime, " +
            "[IsVatIncome] bit " +
            ") " +
            ";WITH LIST_OUTCOMES_CTE AS ( " +
            "SELECT CONVERT(money, " +
            "dbo.GetExchangedToEuroValue([OutcomePaymentOrder].[Amount], [PaymentCurrencyRegister].[CurrencyID], [OutcomePaymentOrder].[Created])) AS [Amount], " +
            "[OutcomePaymentOrder].[Created], " +
            "[SupplyPaymentTask].[IsAccounting] AS [IsVatIncome] " +
            "FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].[ID] = [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
            "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
            "ON [OutcomePaymentOrderSupplyPaymentTask].[OutcomePaymentOrderID] = [OutcomePaymentOrder].[ID] " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].[ID] = [OutcomePaymentOrderSupplyPaymentTask].[SupplyPaymentTaskID] " +
            "WHERE [OutcomePaymentOrder].[Deleted] = 0 " +
            "AND [SupplyPaymentTask].[Deleted] = 0 " +
            "AND [PaymentCurrencyRegister].[Deleted] = 0 " +
            ") " +
            "INSERT INTO @OUTCOME_LIST_TABLE([Amount], [Created],[IsVatIncome]) " +
            "SELECT [Amount] " +
            ",[Created] " +
            ",[IsVatIncome] " +
            "FROM [LIST_OUTCOMES_CTE] " +
            "SELECT ( " +
            "SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) > CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 1 " +
            ")       AS [ValueByDay] " +
            ", ABS( " +
            "(SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) > CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 1) - " +
            "(SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) >= DATEADD(DAY, -1, CONVERT(date, GETUTCDATE())) " +
            "AND DATEADD(HOUR, @QtyHour, [Created]) < CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 1) " +
            ")   AS [IncreaseByDay] " +
            ", CASE " +
            "WHEN (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) > CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 1) > " +
            "(SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) >= DATEADD(DAY, -1, CONVERT(date, GETUTCDATE())) " +
            "AND DATEADD(HOUR, @QtyHour, [Created]) < CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 1) " +
            "THEN 1 " +
            "ELSE 0 " +
            "END AS [IsIncreaseByDay] " +
            ", ( " +
            "SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(GETUTCDATE()) " +
            "AND [IsVatIncome] = 1 " +
            ")       AS [ValueByMonth] " +
            ", ABS( " +
            "(SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(GETUTCDATE()) " +
            "AND [IsVatIncome] = 1) - (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(DATEADD(MONTH, -1, GETUTCDATE())) " +
            "AND [IsVatIncome] = 1) " +
            ")   AS [IncreaseByMonth] " +
            ", CASE " +
            "WHEN (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(GETUTCDATE()) " +
            "AND [IsVatIncome] = 1) > (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(DATEADD(MONTH, -1, GETUTCDATE())) " +
            "AND [IsVatIncome] = 1) " +
            "THEN 1 " +
            "ELSE 0 " +
            "END AS [IsIncreaseByMonth] " +
            ", ( " +
            "SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) > CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 0 " +
            ")       AS [ValueByDay] " +
            ", ABS( " +
            "(SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) > CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 0) - (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) >= DATEADD(DAY, -1, CONVERT(date, GETUTCDATE())) " +
            "AND DATEADD(HOUR, @QtyHour, [Created]) < CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 0) " +
            ")   AS [IncreaseByDay] " +
            ", CASE " +
            "WHEN (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) > CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 0) > (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE DATEADD(HOUR, @QtyHour, [Created]) >= DATEADD(DAY, -1, CONVERT(date, GETUTCDATE())) " +
            "AND DATEADD(HOUR, @QtyHour, [Created]) < CONVERT(date, GETUTCDATE()) " +
            "AND [IsVatIncome] = 0) " +
            "THEN 1 " +
            "ELSE 0 " +
            "END AS [IsIncreaseByDay] " +
            ", ( " +
            "SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(GETUTCDATE()) " +
            "AND [IsVatIncome] = 0 " +
            ")       AS [ValueByMonth] " +
            ", ABS( " +
            "(SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(GETUTCDATE()) " +
            "AND [IsVatIncome] = 0) - (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(DATEADD(MONTH, -1, GETUTCDATE())) " +
            "AND [IsVatIncome] = 0) " +
            ")   AS [IncreaseByMonth] " +
            ", CASE " +
            "WHEN (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(GETUTCDATE()) " +
            "AND [IsVatIncome] = 0) > (SELECT CASE " +
            "WHEN CONVERT(money, SUM([Amount])) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM([Amount])) " +
            "END " +
            "FROM @OUTCOME_LIST_TABLE " +
            "WHERE MONTH(DATEADD(HOUR, @QtyHour, [Created])) = MONTH(DATEADD(MONTH, -1, GETUTCDATE())) " +
            "AND [IsVatIncome] = 0) " +
            "THEN 1 " +
            "ELSE 0 " +
            "END AS [IsIncreaseByMonth] "
            , (vatTotalItem, notVatTotalItem) => {
                if (vatTotalItem != null) {
                    vatTotalItem.ValueByDay = decimal.Round(vatTotalItem.ValueByDay, 2, MidpointRounding.AwayFromZero);
                    vatTotalItem.IncreaseByDay = decimal.Round(vatTotalItem.IncreaseByDay, 2, MidpointRounding.AwayFromZero);
                    vatTotalItem.ValueByMonth = decimal.Round(vatTotalItem.ValueByMonth, 2, MidpointRounding.AwayFromZero);
                    vatTotalItem.IncreaseByMonth = decimal.Round(vatTotalItem.IncreaseByMonth, 2, MidpointRounding.AwayFromZero);
                }

                if (notVatTotalItem != null) {
                    notVatTotalItem.ValueByDay = decimal.Round(notVatTotalItem.ValueByDay, 2, MidpointRounding.AwayFromZero);
                    notVatTotalItem.IncreaseByDay = decimal.Round(notVatTotalItem.IncreaseByDay, 2, MidpointRounding.AwayFromZero);
                    notVatTotalItem.ValueByMonth = decimal.Round(notVatTotalItem.ValueByMonth, 2, MidpointRounding.AwayFromZero);
                    notVatTotalItem.IncreaseByMonth = decimal.Round(notVatTotalItem.IncreaseByMonth, 2, MidpointRounding.AwayFromZero);
                }

                toReturn.VatItem = vatTotalItem;
                toReturn.NotVatItem = notVatTotalItem;

                return vatTotalItem;
            }, new {
                QtyHour = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl") ? 3 : 2
            }, splitOn: "ValueByDay");

        return toReturn;
    }
}