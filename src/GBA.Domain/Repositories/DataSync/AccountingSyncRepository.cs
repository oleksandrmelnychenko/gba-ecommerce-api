using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.Repositories.DataSync.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.DataSync;

public sealed class AccountingSyncRepository : IAccountingSyncRepository {
    private readonly IDbConnection _amgSyncConnection;
    private readonly IDbConnection _oneCConnection;

    private readonly IDbConnection _remoteSyncConnection;

    private readonly string FROM_ONE_C = "Ввід боргів з 1С";

    public AccountingSyncRepository(
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        IDbConnection amgSyncConnection) {
        _oneCConnection = oneCConnection;

        _remoteSyncConnection = remoteSyncConnection;
        _amgSyncConnection = amgSyncConnection;
    }

    public IEnumerable<Currency> GetAllCurrencies() {
        return _remoteSyncConnection.Query<Currency>(
            "SELECT * " +
            "FROM [Currency] " +
            "WHERE [Currency].Deleted = 0"
        );
    }

    public void Update(Currency currency) {
        _remoteSyncConnection.Execute(
            "UPDATE [Currency] " +
            "SET CodeOneC = @CodeOneC, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            currency
        );
    }

    public IEnumerable<SyncExchangeRate> GetAllSyncExchangeRates() {
        return _oneCConnection.Query<SyncExchangeRate>(
            "SELECT " +
            "DATEADD(YEAR, -2000, T1._Period) [Date], " +
            "T2._Code [CurrencyCode], " +
            "T1._Fld11007 / T1._Fld11008 [RateExchange] " +
            "FROM dbo._InfoRg11005 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference17 T2 WITH(NOLOCK) " +
            "ON T1._Fld11006RRef = T2._IDRRef " +
            "WHERE T2._Description NOT LIKE '%НБУ%' " +
            "ORDER BY (T1._Period) "
        );
    }

    public IEnumerable<SyncExchangeRate> GetAmgAllSyncExchangeRates() {
        return _amgSyncConnection.Query<SyncExchangeRate>(
            "SELECT " +
            "DATEADD(YEAR, -2000, T1._Period) [Date], " +
            "T2._Code [CurrencyCode], " +
            "T1._Fld13114 / T1._Fld13115 [RateExchange] " +
            "FROM dbo._InfoRg13112 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference35 T2 WITH(NOLOCK) " +
            "ON T1._Fld13113RRef = T2._IDRRef " +
            "WHERE T2._Description LIKE '%НБУ%' " +
            "ORDER BY (T1._Period) "
        );
    }

    public IEnumerable<SyncCrossExchangeRate> GetAllSyncCrossExchangeRates() {
        return _oneCConnection.Query<SyncCrossExchangeRate>(
            "SELECT " +
            "DATEADD(YEAR, -2000, T1._Period) [Date], " +
            "T2._Code [FromCurrencyCode], " +
            "T3._Code [ToCurrencyCode], " +
            "T1._Fld15584 / T1._Fld15596 " +
            "FROM dbo._InfoRg15581 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference17 T2 WITH(NOLOCK) " +
            "ON T1._Fld15582RRef = T2._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 T3 WITH(NOLOCK) " +
            "ON T1._Fld15583RRef = T3._IDRRef " +
            "ORDER BY (T1._Period) "
        );
    }

    public IEnumerable<SyncCrossExchangeRate> GetAmgAllSyncCrossExchangeRates() {
        return _amgSyncConnection.Query<SyncCrossExchangeRate>(
            "SELECT " +
            "DATEADD(YEAR, -2000, T1._Period) [Date], " +
            "T2._Code [FromCurrencyCode], " +
            "T3._Code [ToCurrencyCode], " +
            "T1._Fld14718 / T1._Fld14719 " +
            "FROM dbo._InfoRg14715 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference35 T2 WITH(NOLOCK) " +
            "ON T1._Fld14716RRef = T2._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference35 T3 WITH(NOLOCK) " +
            "ON T1._Fld14717RRef = T3._IDRRef " +
            "ORDER BY (T1._Period) "
        );
    }

    public IEnumerable<ExchangeRate> GetAllUahExchangeRates() {
        return _remoteSyncConnection.Query<ExchangeRate, Currency, ExchangeRate>(
            "SELECT * " +
            "FROM [ExchangeRate] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[Code] = [ExchangeRate].Code " +
            "AND [Currency].Deleted = 0 " +
            "WHERE [ExchangeRate].Culture = N'uk' " +
            "AND [ExchangeRate].Deleted = 0",
            (rate, currency) => {
                rate.AssignedCurrency = currency;

                return rate;
            }
        );
    }

    public IEnumerable<GovExchangeRate> GetAllGovExchangeRates() {
        return _remoteSyncConnection.Query<GovExchangeRate, Currency, GovExchangeRate>(
            "SELECT * " +
            "FROM [GovExchangeRate] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[Code] = [GovExchangeRate].Code " +
            "AND [Currency].Deleted = 0 " +
            "WHERE [GovExchangeRate].Culture = N'uk' " +
            "AND [GovExchangeRate].Deleted = 0",
            (rate, currency) => {
                rate.AssignedCurrency = currency;

                return rate;
            }
        );
    }

    public void CleanExchangeRateHistory() {
        _remoteSyncConnection.Execute(
            "DELETE FROM [ExchangeRateHistory] " +
            "WHERE ExchangeRateID IN ( " +
            "SELECT [ExchangeRate].ID " +
            "FROM [ExchangeRate] " +
            "WHERE [ExchangeRate].Deleted = 0 " +
            "AND [ExchangeRate].Culture = N'uk' " +
            ") "
        );
    }

    public void CleanGovExchangeRateHistory() {
        _remoteSyncConnection.Execute(
            "DELETE FROM [GovExchangeRateHistory] " +
            "WHERE GovExchangeRateID IN ( " +
            "SELECT [GovExchangeRate].ID " +
            "FROM [GovExchangeRate] " +
            "WHERE [GovExchangeRate].Deleted = 0 " +
            "AND [GovExchangeRate].Culture = N'uk' " +
            ") "
        );
    }

    public void CleanCrossExchangeRateHistory() {
        _remoteSyncConnection.Execute(
            "DELETE FROM [CrossExchangeRateHistory] " +
            "WHERE CrossExchangeRateID IN ( " +
            "SELECT [CrossExchangeRate].ID " +
            "FROM [CrossExchangeRate] " +
            "WHERE [CrossExchangeRate].Deleted = 0 " +
            "AND [CrossExchangeRate].Culture = N'uk' " +
            ") "
        );
    }

    public void CleanGovCrossExchangeRateHistory() {
        _remoteSyncConnection.Execute(
            "DELETE FROM [GovCrossExchangeRateHistory] " +
            "WHERE GovCrossExchangeRateID IN ( " +
            "SELECT [GovCrossExchangeRate].ID " +
            "FROM [GovCrossExchangeRate] " +
            "WHERE [GovCrossExchangeRate].Deleted = 0 " +
            "AND [GovCrossExchangeRate].Culture = N'uk' " +
            ") "
        );
    }

    public void Add(ExchangeRateHistory history) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [ExchangeRateHistory] " +
            "([Amount], [ExchangeRateID], [UpdatedByID], [Created], [Updated]) " +
            "VALUES " +
            "(@Amount, @ExchangeRateId, @UpdatedById, @Created, @Updated)",
            history
        );
    }

    public long Add(SupplyOrder supplyOrder) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO SupplyOrder (IsOrderArrived, OrderArrivedDate, VechicalArrived, PlaneArrived, ShipArrived, CompleteDate, OrderShippedDate, " +
            "IsOrderShipped, IsCompleted, TransportationType, GrossPrice, NetPrice, ClientID, OrganizationID, Qty, SupplyOrderNumberID, SupplyProFormID, " +
            "PortWorkServiceID, TransportationServiceID, DateFrom, CustomAgencyServiceId, PortCustomAgencyServiceId, PlaneDeliveryServiceId, " +
            "VehicleDeliveryServiceId, IsDocumentSet, IsPlaced, Comment, ClientAgreementId, IsGrossPricesCalculated, IsPartiallyPlaced, IsFullyPlaced, " +
            "IsOrderInsidePoland, AdditionalAmount, AdditionalPercent, AdditionalPaymentCurrencyId, AdditionalPaymentFromDate, Updated) " +
            "VALUES(@IsOrderArrived, @OrderArrivedDate, @VechicalArrived, @PlaneArrived, @ShipArrived, @CompleteDate, @OrderShippedDate, @IsOrderShipped, " +
            "@IsCompleted, @TransportationType, @GrossPrice, @NetPrice, @ClientID, @OrganizationID, @Qty, @SupplyOrderNumberID, @SupplyProFormID, " +
            "@PortWorkServiceID, @TransportationServiceID, @DateFrom, @CustomAgencyServiceId, @PortCustomAgencyServiceId, @PlaneDeliveryServiceId, " +
            "@VehicleDeliveryServiceId, @IsDocumentSet, 0, @Comment, @ClientAgreementId, 0, 0, 0, @IsOrderInsidePoland, 0.00, 0.00, NULL, NULL, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            supplyOrder
        ).Single();
    }

    public long Add(SupplyInvoice supplyInvoice) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO SupplyInvoice (SupplyOrderID, Number, NetPrice, IsShipped, DateFrom, PaymentTo, ServiceNumber, Comment, IsPartiallyPlaced, " +
                "IsFullyPlaced, Updated, DeliveryAmount, DiscountAmount) " +
                "VALUES(@SupplyOrderID, @Number, @NetPrice, @IsShipped, @DateFrom, @PaymentTo, @ServiceNumber, @Comment, 0, 0, getutcdate(), @DeliveryAmount, " +
                "@DiscountAmount); " +
                "SELECT SCOPE_IDENTITY()",
                supplyInvoice
            )
            .Single();
    }

    public long Add(PackingList packingList) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO [PackingList] " +
                "(MarkNumber, InvNo, PlNo, RefNo, No, FromDate, SupplyInvoiceId, IsDocumentsAdded, ExtraCharge, ContainerServiceId, Comment, IsPlaced, " +
                "IsVatOneApplied, IsVatTwoApplied, VatOnePercent, VatTwoPercent, Updated, AccountingExtraCharge) " +
                "VALUES " +
                "(@MarkNumber, @InvNo, @PlNo, @RefNo, @No, @FromDate, @SupplyInvoiceId, @IsDocumentsAdded, @ExtraCharge, @ContainerServiceId, @Comment, 0, 0, 0, 0, 0, getutcdate(), " +
                "@AccountingExtraCharge); " +
                "SELECT SCOPE_IDENTITY()",
                packingList
            )
            .Single();
    }

    public long Add(SupplyOrderNumber supplyOrderNumber) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO SupplyOrderNumber (Number, Updated) " +
                "VALUES(@Number, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                supplyOrderNumber
            )
            .Single();
    }

    public long Add(SupplyProForm supplyProform) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO SupplyProForm (NetPrice, Number, IsSkipped, DateFrom, ServiceNumber, Updated) " +
                "VALUES(@NetPrice, @Number, @IsSkipped, @DateFrom, @ServiceNumber, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                supplyProform
            )
            .Single();
    }

    public long Add(SupplyOrderItem supplyOrderItem) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO SupplyOrderItem ([Description], ItemNo, ProductID, Qty, StockNo, SupplyOrderID, TotalAmount, UnitPrice, GrossWeight, NetWeight, " +
            "IsPacked, Updated) " +
            "VALUES(@Description, @ItemNo, @ProductID, @Qty, @StockNo, @SupplyOrderID, @TotalAmount, @UnitPrice, @GrossWeight, @NetWeight, @IsPacked, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            supplyOrderItem
        ).FirstOrDefault();
    }

    public long Add(SupplyInvoiceOrderItem supplyInvoiceOrderItem) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [SupplyInvoiceOrderItem] (Qty, SupplyOrderItemId, SupplyInvoiceId, UnitPrice, Updated, [RowNumber], [ProductIsImported], [ProductID]) " +
            "VALUES (@Qty, @SupplyOrderItemId, @SupplyInvoiceId, @UnitPrice, getutcdate(), @RowNumber, @ProductIsImported, @ProductId); " +
            "SELECT SCOPE_IDENTITY()",
            supplyInvoiceOrderItem
        ).Single();
    }

    public long Add(SupplyOrderPaymentDeliveryProtocol protocol) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO SupplyOrderPaymentDeliveryProtocol (SupplyPaymentTaskID, UserID, SupplyInvoiceID, SupplyOrderPaymentDeliveryProtocolKeyID, [Value], SupplyProFormID, Discount, Updated, IsAccounting) " +
                "VALUES(@SupplyPaymentTaskID, @UserID, @SupplyInvoiceID, @SupplyOrderPaymentDeliveryProtocolKeyID, @Value, @SupplyProFormID, @Discount, getutcdate(), @IsAccounting); " +
                "SELECT SCOPE_IDENTITY()",
                protocol
            )
            .Single();
    }

    public long Add(PackingListPackageOrderItem packingListPackageOrderItem) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [PackingListPackageOrderItem] (Qty, SupplyInvoiceOrderItemId, PackingListPackageId, PackingListId, IsPlaced, IsErrorInPlaced, " +
            "IsReadyToPlaced, UnitPrice, GrossWeight, NetWeight, UploadedQty, Placement, UnitPriceEur, GrossUnitPriceEur, ContainerUnitPriceEur, ExchangeRateAmount, " +
            "VatPercent, PlacedQty, Updated, AccountingGrossUnitPriceEur, AccountingContainerUnitPriceEur, [AccountingGeneralGrossUnitPriceEur], [ExchangeRateAmountUahToEur], " +
            "[DeliveryPerItem], [ProductIsImported]) " +
            "VALUES (@Qty, @SupplyInvoiceOrderItemId, @PackingListPackageId, @PackingListId, 0, 0, 0, @UnitPrice, @GrossWeight, @NetWeight, @UploadedQty, @Placement, " +
            "@UnitPriceEur, @GrossUnitPriceEur, @ContainerUnitPriceEur, @ExchangeRateAmount, @VatPercent, 0, getutcdate(), @AccountingGrossUnitPriceEur, " +
            "@AccountingContainerUnitPriceEur, @AccountingGeneralGrossUnitPriceEur, @ExchangeRateAmountUahToEur, @DeliveryPerItem, @ProductIsImported); " +
            "SELECT SCOPE_IDENTITY()",
            packingListPackageOrderItem).Single();
    }

    public long Add(SupplyOrderPaymentDeliveryProtocolKey key) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO SupplyOrderPaymentDeliveryProtocolKey([Key], Updated) VALUES(@Key, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                key
            )
            .Single();
    }

    public long Add(SupplyPaymentTask supplyPaymentTask) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO SupplyPaymentTask(Comment, UserID, PayToDate, TaskAssignedTo, TaskStatus, TaskStatusUpdated, NetPrice, GrossPrice, Updated, IsAccounting, IsImportedFromOneC) " +
            "VALUES(@Comment, @UserID, @PayToDate, @TaskAssignedTo, @TaskStatus, @TaskStatusUpdated, @NetPrice, @GrossPrice, getutcdate(), @IsAccounting, @IsImportedFromOneC); " +
            "SELECT SCOPE_IDENTITY()",
            supplyPaymentTask
        ).Single();
    }

    public long Add(SupplyInformationDeliveryProtocolKey key) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO SupplyInformationDeliveryProtocolKey ([Key], KeyAssignedTo, IsDefault, TransportationType, Updated) " +
                "VALUES(@Key, @KeyAssignedTo, @IsDefault, @TransportationType, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                key
            )
            .Single();
    }

    public long Add(SupplyInformationDeliveryProtocolKeyTranslation keyTranslation) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO SupplyInformationDeliveryProtocolKeyTranslation (CultureCode, Key, Updated) VALUES(@CultureCode, @Key, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            keyTranslation
        ).Single();
    }

    public long Add(SupplyInformationDeliveryProtocol protocol) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO SupplyInformationDeliveryProtocol " +
                "(SupplyProFormID, SupplyOrderID, UserID, SupplyInvoiceID, SupplyInformationDeliveryProtocolKeyID, [Value], IsDefault, Updated) " +
                "VALUES " +
                "(@SupplyProFormID, @SupplyOrderID, @UserID, @SupplyInvoiceID, @SupplyInformationDeliveryProtocolKeyID, @Value, @IsDefault, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                protocol
            )
            .Single();
    }

    public SupplyOrderPaymentDeliveryProtocolKey GetProtocolPaymentByKey(string debtsFromOneCKey) {
        return _remoteSyncConnection.Query<SupplyOrderPaymentDeliveryProtocolKey>(
                "SELECT * FROM SupplyOrderPaymentDeliveryProtocolKey " +
                "WHERE [Key] = @DebtsFromOneCKey ",
                new { DebtsFromOneCKey = debtsFromOneCKey }
            )
            .FirstOrDefault();
    }

    public SupplyInformationDeliveryProtocolKey GetInformationProtocolByKey(string debtsFromOneCKey) {
        return _remoteSyncConnection.Query<SupplyInformationDeliveryProtocolKey>(
                "SELECT * FROM SupplyInformationDeliveryProtocolKey " +
                "WHERE [Key] = @DebtsFromOneCKey ",
                new { DebtsFromOneCKey = debtsFromOneCKey }
            )
            .FirstOrDefault();
    }


    public void Add(GovExchangeRateHistory history) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [GovExchangeRateHistory] " +
            "([Amount], [GovExchangeRateID], [UpdatedByID], [Created], [Updated]) " +
            "VALUES " +
            "(@Amount, @GovExchangeRateId, @UpdatedById, @Created, @Updated)",
            history
        );
    }

    public void Add(ExchangeRate exchangeRate) {
        _remoteSyncConnection.Execute(
            "INSERT INTO ExchangeRate (Culture, Amount, Currency, Updated, CurrencyID) " +
            "VALUES (@Culture, @Amount, @Currency, getutcdate(), @CurrencyId); ",
            exchangeRate
        );
    }

    public void Update(IEnumerable<ExchangeRate> exchangeRates) {
        _remoteSyncConnection.Execute(
            "UPDATE [ExchangeRate] " +
            "SET [Amount] = @Amount, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            exchangeRates
        );
    }

    public void Update(IEnumerable<GovExchangeRate> exchangeRates) {
        _remoteSyncConnection.Execute(
            "UPDATE [GovExchangeRate] " +
            "SET [Amount] = @Amount, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            exchangeRates
        );
    }

    public IEnumerable<Client> GetAllClients() {
        List<Client> clients = new();

        _remoteSyncConnection.Query<Client, ClientAgreement, Agreement, Organization, Currency, ClientInRole, ClientType, Client>(
            "SELECT * " +
            "FROM [Client] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "AND [ClientAgreement].Deleted = 0 " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].[ClientID] = [Client].[ID] " +
            "LEFT JOIN [ClientType] " +
            "ON [ClientInRole].[ClientTypeID] = [ClientType].[ID] " +
            "WHERE [Client].Deleted = 0 " +
            "AND ([Client].SourceAmgCode <> 0 " +
            "OR [Client].SourceFenixCode <> 0) " +
            "AND [Agreement].ID IS NOT NULL",
            (client, clientAgreement, agreement, organization, currency, clientInRole, clientType) => {
                if (clientAgreement == null || agreement == null || organization == null || currency == null) return client;

                if (clients.Any(c => c.Id.Equals(client.Id)))
                    client = clients.First(c => c.Id.Equals(client.Id));
                else
                    clients.Add(client);

                agreement.Currency = currency;
                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                client.ClientAgreements.Add(clientAgreement);

                clientInRole.ClientType = clientType;
                client.ClientInRole = clientInRole;

                return client;
            }
        );

        return clients;
    }

    public IEnumerable<SyncAccounting> GetSyncAccountingFiltered(
        long clientCode,
        string agreementName,
        string organizationName,
        string currencyCode) {
        return _oneCConnection.Query<SyncAccounting>(
            "SELECT " +
            "DATEADD(YEAR, -2000, T1.[Period]) [Date], " +
            "CASE WHEN T1.RecorderTRef = 0x000000AF THEN T2._Number WHEN T1.RecorderTRef = 0x000000FF " +
            "THEN T3._Number WHEN T1.RecorderTRef = 0x00000123 THEN T4._Number WHEN T1.RecorderTRef = 0x0000011B " +
            "THEN T5._Number WHEN T1.RecorderTRef = 0x0000010D THEN T6._Number WHEN T1.RecorderTRef = 0x0000011F " +
            "THEN T7._Number WHEN T1.RecorderTRef = 0x00000113 THEN T8._Number WHEN T1.RecorderTRef = 0x00000116 " +
            "THEN T9._Number WHEN T1.RecorderTRef = 0x000000FD THEN T10._Number WHEN T1.RecorderTRef = 0x000000F7 " +
            "THEN T11._Number WHEN T1.RecorderTRef = 0x0000010F THEN T12._Number WHEN T1.RecorderTRef = 0x000000A3 " +
            "THEN T13._Number WHEN T1.RecorderTRef = 0x00000114 THEN T14._Number WHEN T1.RecorderTRef = 0x00003CF2 " +
            "THEN T15._Number WHEN T1.RecorderTRef = 0x000000DE THEN T16._Number WHEN T1.RecorderTRef = 0x000000B0 " +
            "THEN T17._Number WHEN T1.RecorderTRef = 0x000000E1 THEN T18._Number WHEN T1.RecorderTRef = 0x00000112 " +
            "THEN T19._Number WHEN T1.RecorderTRef = 0x0000010C THEN T20._Number WHEN T1.RecorderTRef = 0x000000EF " +
            "THEN T21._Number WHEN T1.RecorderTRef = 0x000000B5 THEN T22._Number WHEN T1.RecorderTRef = 0x00000115 " +
            "THEN T23._Number WHEN T1.RecorderTRef = 0x0000010E THEN T24._Number WHEN T1.RecorderTRef = 0x000000F8 " +
            "THEN T25._Number WHEN T1.RecorderTRef = 0x000000F6 THEN T26._Number WHEN T1.RecorderTRef = 0x000000B1 " +
            "THEN T27._Number WHEN T1.RecorderTRef = 0x000000C0 THEN T28._Number WHEN T1.RecorderTRef = 0x00000122 " +
            "THEN T29._Number WHEN T1.RecorderTRef = 0x000000A2 THEN T30._Number WHEN T1.RecorderTRef = 0x000000DD " +
            "THEN T31._Number ELSE CAST(NULL AS NVARCHAR(11)) END [Number], " +
            "T1.Fld14891FinalBalance_ [Value] " +
            "FROM (SELECT " +
            "T2.[Period] AS [Period], " +
            "T2.Fld12424RRef AS Fld12424RRef, " +
            "T2.Fld12422_TYPE AS Fld12422_TYPE, " +
            "T2.Fld12422_RTRef AS Fld12422_RTRef, " +
            "T2.Fld12422_RRRef AS Fld12422_RRRef, " +
            "T2.Fld12423RRef AS Fld12423RRef, " +
            "T2.Fld12421RRef AS Fld12421RRef, " +
            "T2.RecorderRRef AS RecorderRRef, " +
            "T2.RecorderTRef AS RecorderTRef, " +
            "CAST(T2.Fld14891Balance_ + T2.Fld14891Receipt_ - T2.Fld14891Expense_ AS NUMERIC(35, 8)) AS Fld14891FinalBalance_ " +
            "FROM (SELECT " +
            "T3._Period AS [Period], " +
            "T3._Fld12424RRef AS Fld12424RRef, " +
            "T3._Fld12422_TYPE AS Fld12422_TYPE, " +
            "T3._Fld12422_RTRef AS Fld12422_RTRef, " +
            "T3._Fld12422_RRRef AS Fld12422_RRRef, " +
            "T3._Fld12423RRef AS Fld12423RRef, " +
            "T3._Fld12421RRef AS Fld12421RRef, " +
            "T3._RecorderRRef AS RecorderRRef, " +
            "T3._RecorderTRef AS RecorderTRef, " +
            "CAST(0.0 AS NUMERIC(15, 8)) AS Fld14891Balance_, " +
            "CAST(CASE WHEN T3._RecordKind = 0.0 THEN T3._Fld12425 ELSE 0.0 END AS NUMERIC(27, 8)) AS Fld14891Receipt_, " +
            "CAST(CASE WHEN T3._RecordKind = 0.0 THEN 0.0 ELSE T3._Fld12425 END AS NUMERIC(27, 8)) AS Fld14891Expense_ " +
            "FROM dbo._AccumRg12420 T3 WITH(NOLOCK) " +
            "WHERE T3._Active = 0x01) T2) T1 " +
            "LEFT OUTER JOIN dbo._Document175 T2 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x000000AF AND T1.RecorderRRef = T2._IDRRef " +
            "LEFT OUTER JOIN dbo._Document255 T3 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x000000FF AND T1.RecorderRRef = T3._IDRRef " +
            "LEFT OUTER JOIN dbo._Document291 T4 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x00000123 AND T1.RecorderRRef = T4._IDRRef " +
            "LEFT OUTER JOIN dbo._Document283 T5 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x0000011B AND T1.RecorderRRef = T5._IDRRef " +
            "LEFT OUTER JOIN dbo._Document269 T6 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x0000010D AND T1.RecorderRRef = T6._IDRRef " +
            "LEFT OUTER JOIN dbo._Document287 T7 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x0000011F AND T1.RecorderRRef = T7._IDRRef " +
            "LEFT OUTER JOIN dbo._Document275 T8 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x00000113 AND T1.RecorderRRef = T8._IDRRef " +
            "LEFT OUTER JOIN dbo._Document278 T9 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x00000116 AND T1.RecorderRRef = T9._IDRRef " +
            "LEFT OUTER JOIN dbo._Document253 T10 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x000000FD AND T1.RecorderRRef = T10._IDRRef " +
            "LEFT OUTER JOIN dbo._Document247 T11 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x000000F7 AND T1.RecorderRRef = T11._IDRRef " +
            "LEFT OUTER JOIN dbo._Document271 T12 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x0000010F AND T1.RecorderRRef = T12._IDRRef " +
            "LEFT OUTER JOIN dbo._Document163 T13 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x000000A3 AND T1.RecorderRRef = T13._IDRRef " +
            "LEFT OUTER JOIN dbo._Document276 T14 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x00000114 AND T1.RecorderRRef = T14._IDRRef " +
            "LEFT OUTER JOIN dbo._Document15602 T15 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x00003CF2 AND T1.RecorderRRef = T15._IDRRef " +
            "LEFT OUTER JOIN dbo._Document222 T16 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x000000DE AND T1.RecorderRRef = T16._IDRRef " +
            "LEFT OUTER JOIN dbo._Document176 T17 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x000000B0 AND T1.RecorderRRef = T17._IDRRef " +
            "LEFT OUTER JOIN dbo._Document225 T18 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x000000E1 AND T1.RecorderRRef = T18._IDRRef " +
            "LEFT OUTER JOIN dbo._Document274 T19 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x00000112 AND T1.RecorderRRef = T19._IDRRef " +
            "LEFT OUTER JOIN dbo._Document268 T20 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x0000010C AND T1.RecorderRRef = T20._IDRRef " +
            "LEFT OUTER JOIN dbo._Document239 T21 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x000000EF AND T1.RecorderRRef = T21._IDRRef " +
            "LEFT OUTER JOIN dbo._Document181 T22 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x000000B5 AND T1.RecorderRRef = T22._IDRRef " +
            "LEFT OUTER JOIN dbo._Document277 T23 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x00000115 AND T1.RecorderRRef = T23._IDRRef " +
            "LEFT OUTER JOIN dbo._Document270 T24 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x0000010E AND T1.RecorderRRef = T24._IDRRef " +
            "LEFT OUTER JOIN dbo._Document248 T25 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x000000F8 AND T1.RecorderRRef = T25._IDRRef " +
            "LEFT OUTER JOIN dbo._Document246 T26 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x000000F6 AND T1.RecorderRRef = T26._IDRRef " +
            "LEFT OUTER JOIN dbo._Document177 T27 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x000000B1 AND T1.RecorderRRef = T27._IDRRef " +
            "LEFT OUTER JOIN dbo._Document192 T28 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x000000C0 AND T1.RecorderRRef = T28._IDRRef " +
            "LEFT OUTER JOIN dbo._Document290 T29 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x00000122 AND T1.RecorderRRef = T29._IDRRef " +
            "LEFT OUTER JOIN dbo._Document162 T30 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x000000A2 AND T1.RecorderRRef = T30._IDRRef " +
            "LEFT OUTER JOIN dbo._Document221 T31 WITH(NOLOCK) " +
            "ON T1.RecorderTRef = 0x000000DD AND T1.RecorderRRef = T31._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference47 T32 WITH(NOLOCK) " +
            "ON T1.Fld12421RRef = T32._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T33 WITH(NOLOCK) " +
            "ON T1.Fld12423RRef = T33._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference68 T34 WITH(NOLOCK) " +
            "ON T1.Fld12424RRef = T34._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference17 T35 WITH(NOLOCK) " +
            "ON T32._Fld988RRef = T35._IDRRef " +
            "WHERE T32.[_Description] = @AgreementName " +
            "AND T33.[_Description] = @OrganizationName " +
            "AND T34._Code = @ClientCode " +
            "AND T35._Code LIKE @CurrencyCode " +
            "ORDER BY [Date] DESC ",
            new {
                ClientCode = clientCode,
                AgreementName = agreementName,
                OrganizationName = organizationName,
                CurrencyCode = currencyCode
            }
        );
    }

    public IEnumerable<SyncAccounting> GetAmgSyncAccountingFiltered(
        long clientCode,
        string agreementName,
        string organizationName,
        string currencyCode) {
        return _amgSyncConnection.Query<SyncAccounting>(
            "SELECT " +
            "T1.Fld14891FinalBalance_ [Value], " +
            "CASE " +
            "WHEN (CASE " +
            "WHEN T1.RecorderTRef = 0x00000159 " +
            "THEN T4._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x000000DC " +
            "THEN T5._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x000000E0 " +
            "THEN T6._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x00000147 " +
            "THEN T7._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000014A " +
            "THEN T8._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000010D " +
            "THEN T9._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x00000140 " +
            "THEN T10._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000014C " +
            "THEN T11._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000013F " +
            "THEN T12._Date_Time " +
            "ELSE T13._Date_Time " +
            "END END END END END END END END " +
            "END ) = N'2001-01-01 00:00:00.000' " +
            "THEN NULL " +
            "ELSE DATEADD(YEAR, -2000, (CASE " +
            "WHEN T1.RecorderTRef = 0x00000159 " +
            "THEN T4._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x000000DC " +
            "THEN T5._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x000000E0 " +
            "THEN T6._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x00000147 " +
            "THEN T7._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000014A " +
            "THEN T8._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000010D " +
            "THEN T9._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x00000140 " +
            "THEN T10._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000014C " +
            "THEN T11._Date_Time " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000013F " +
            "THEN T12._Date_Time " +
            "ELSE T13._Date_Time " +
            "END END END END END END END END END)) " +
            "END [Date], " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x00000159 " +
            "THEN T4._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x000000DC " +
            "THEN T5._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x000000E0 " +
            "THEN T6._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x00000147 " +
            "THEN T7._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000014A " +
            "THEN T8._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000010D " +
            "THEN T9._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x00000140 " +
            "THEN T10._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000014C " +
            "THEN T11._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x0000013F " +
            "THEN T12._Number " +
            "ELSE " +
            "CASE " +
            "WHEN T1.RecorderTRef = 0x000000DB " +
            "THEN T13._Number " +
            "ELSE '' " +
            "END " +
            "END " +
            "END " +
            "END " +
            "END " +
            "END " +
            "END " +
            "END " +
            "END " +
            "END [Number] " +
            "FROM (SELECT " +
            "T2.Fld14890RRef AS Fld14890RRef, " +
            "T2.Fld14888_TYPE AS Fld14888_TYPE, " +
            "T2.Fld14888_RTRef AS Fld14888_RTRef, " +
            "T2.Fld14888_RRRef AS Fld14888_RRRef, " +
            "T2.Fld14889RRef AS Fld14889RRef, " +
            "T2.Fld14887RRef AS Fld14887RRef, " +
            "T2.RecorderRRef AS RecorderRRef, " +
            "T2.RecorderTRef AS RecorderTRef, " +
            "CAST(T2.Fld14891Balance_ + T2.Fld14891Receipt_ - T2.Fld14891Expense_ AS NUMERIC(35, 8)) AS Fld14891FinalBalance_ " +
            "FROM (SELECT " +
            "T3._Fld14890RRef AS Fld14890RRef, " +
            "T3._Fld14888_TYPE AS Fld14888_TYPE, " +
            "T3._Fld14888_RTRef AS Fld14888_RTRef, " +
            "T3._Fld14888_RRRef AS Fld14888_RRRef, " +
            "T3._Fld14889RRef AS Fld14889RRef, " +
            "T3._Fld14887RRef AS Fld14887RRef, " +
            "T3._RecorderRRef AS RecorderRRef, " +
            "T3._RecorderTRef AS RecorderTRef, " +
            "CAST(0.0 AS NUMERIC(15, 8)) AS Fld14891Balance_, " +
            "CAST(CASE WHEN T3._RecordKind = 0.0 THEN T3._Fld14891 ELSE 0.0 END AS NUMERIC(27, 8)) AS Fld14891Receipt_, " +
            "CAST(CASE WHEN T3._RecordKind = 0.0 THEN 0.0 ELSE T3._Fld14891 END AS NUMERIC(27, 8)) AS Fld14891Expense_ " +
            "FROM dbo._AccumRg14886 T3 WITH(NOLOCK) " +
            "WHERE T3._Active = 0x01) T2) T1 " +
            "LEFT OUTER JOIN dbo._Document345 T4 WITH(NOLOCK) " +
            "ON T4._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Document220 T5 WITH(NOLOCK) " +
            "ON T5._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Document224 T6 WITH(NOLOCK) " +
            "ON T6._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Document327 T7 WITH(NOLOCK) " +
            "ON T7._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Document330 T8 WITH(NOLOCK) " +
            "ON T8._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Document269 T9 WITH(NOLOCK) " +
            "ON T9._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Document320 T10 WITH(NOLOCK) " +
            "ON T10._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Document332 T11 WITH(NOLOCK) " +
            "ON T11._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Document319 T12 WITH(NOLOCK) " +
            "ON T12._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Document219 T13 WITH(NOLOCK) " +
            "ON T13._IDRRef = RecorderRRef " +
            "LEFT OUTER JOIN dbo._Reference116 T14 WITH(NOLOCK) " +
            "ON T1.Fld14889RRef = T14._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference66 T15 WITH(NOLOCK) " +
            "ON T1.Fld14887RRef = T15._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference90 T16 WITH(NOLOCK) " +
            "ON T1.Fld14890RRef = T16._IDRRef " +
            "LEFT OUTER JOIN dbo._Reference35 T17 WITH(NOLOCK) " +
            "ON T15._Fld1323RRef = T17._IDRRef " +
            "WHERE T14._Description = @OrganizationName " +
            "AND T15._Description = @AgreementName " +
            "AND T16._Code = @ClientCode " +
            "AND T17._Code LIKE @CurrencyCode " +
            "ORDER BY [Date] DESC ",
            new {
                ClientCode = clientCode,
                AgreementName = agreementName,
                OrganizationName = organizationName,
                CurrencyCode = currencyCode
            }
        );
    }

    public long Add(BaseLifeCycleStatus status) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [BaseLifeCycleStatus] " +
            "([SaleLifeCycleType], [Updated]) " +
            "VALUES " +
            "(@SaleLifeCycleType, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            status
        ).Single();
    }

    public long Add(BaseSalePaymentStatus status) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [BaseSalePaymentStatus] " +
            "([SalePaymentStatusType], [Amount], [Updated]) " +
            "VALUES " +
            "(@SalePaymentStatusType, @Amount, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            status
        ).Single();
    }

    public long Add(SaleNumber number) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [SaleNumber] " +
            "([Value], [OrganizationID], [Updated]) " +
            "VALUES " +
            "(@Value, @OrganizationId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            number
        ).Single();
    }

    public long Add(DeliveryRecipient deliveryRecipient) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [DeliveryRecipient] " +
            "([FullName], [Priority], [ClientID], [MobilePhone], [Updated]) " +
            "VALUES " +
            "(@FullName, @Priority, @ClientId, @MobilePhone, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            deliveryRecipient
        ).Single();
    }

    public long Add(DeliveryRecipientAddress address) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [DeliveryRecipientAddress] " +
            "([Value], [Department], [City], [Priority], [DeliveryRecipientID], [Updated]) " +
            "VALUES " +
            "(@Value, @Department, @City, @Priority, @DeliveryRecipientId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            address
        ).Single();
    }

    public long Add(Debt entity) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [Debt] " +
            "([Total], [Days], [Created], [Updated]) " +
            "VALUES " +
            "(@Total, @Days, @Created, @Updated); " +
            "SELECT SCOPE_IDENTITY()",
            entity
        ).Single();
    }

    public void Add(ClientInDebt entity) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [ClientInDebt] " +
            "([ClientID], [AgreementID], [DebtID], [SaleID], [Created], [Updated]) " +
            "VALUES " +
            "(@ClientId, @AgreementId, @DebtId, @SaleId, @Created, @Updated)",
            entity
        );
    }

    public long Add(Order entity) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [Order] " +
            "( " +
            "[Created] " +
            ",[OrderSource] " +
            ",[Updated] " +
            ",[UserID] " +
            ",[ClientAgreementID] " +
            ",[OrderStatus] " +
            ",[IsMerged] " +
            ",[ClientShoppingCartID] " +
            ") " +
            "VALUES " +
            "(" +
            "@Created " +
            ",@OrderSource " +
            ",@Updated " +
            ",@UserID " +
            ",@ClientAgreementId " +
            ",@OrderStatus " +
            ",@IsMerged " +
            ",@ClientShoppingCartId " +
            "); " +
            "SELECT SCOPE_IDENTITY()",
            entity
        ).Single();
    }

    public long Add(Sale sale) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [Sale] " +
            "( " +
            "[ClientAgreementID] " +
            ",[Created] " +
            ",[Deleted] " +
            ",[OrderID] " +
            ",[Updated] " +
            ",[UserID] " +
            ",[BaseLifeCycleStatusID] " +
            ",[BaseSalePaymentStatusID] " +
            ",[Comment] " +
            ",[SaleNumberID] " +
            ",[DeliveryRecipientID] " +
            ",[DeliveryRecipientAddressID] " +
            ",[TransporterID] " +
            ",[ShiftStatusID] " +
            ",[ParentNetId] " +
            ",[IsMerged] " +
            ",[SaleInvoiceDocumentID] " +
            ",[SaleInvoiceNumberID] " +
            ",[ChangedToInvoice] " +
            ",[OneTimeDiscountComment] " +
            ",[ChangedToInvoiceByID] " +
            ",[ShipmentDate] " +
            ",[CashOnDeliveryAmount] " +
            ",[HasDocuments] " +
            ",[IsCashOnDelivery] " +
            ",[IsPrinted] " +
            ",[TTN] " +
            ",[ShippingAmount] " +
            ",[TaxFreePackListID] " +
            ",[SadID] " +
            ",[IsVatSale] " +
            ",[ShippingAmountEur] " +
            ",[ExpiredDays] " +
            ",[IsLocked] " +
            ",[IsPaymentBillDownloaded] " +
            ",[IsImported]" +
            ") " +
            "VALUES " +
            "(" +
            "@ClientAgreementId " +
            ",@Created " +
            ",@Deleted " +
            ",@OrderID " +
            ",@Updated " +
            ",@UserID " +
            ",@BaseLifeCycleStatusId " +
            ",@BaseSalePaymentStatusId " +
            ",@Comment " +
            ",@SaleNumberId " +
            ",@DeliveryRecipientId " +
            ",@DeliveryRecipientAddressId " +
            ",@TransporterId " +
            ",@ShiftStatusId " +
            ",@ParentNetId " +
            ",@IsMerged " +
            ",@SaleInvoiceDocumentId " +
            ",@SaleInvoiceNumberId " +
            ",@ChangedToInvoice " +
            ",@OneTimeDiscountComment " +
            ",@ChangedToInvoiceById " +
            ",@ShipmentDate " +
            ",@CashOnDeliveryAmount " +
            ",@HasDocuments " +
            ",@IsCashOnDelivery " +
            ",@IsPrinted " +
            ",@TTN " +
            ",@ShippingAmount " +
            ",@TaxFreePackListId " +
            ",@SadID " +
            ",@IsVatSale " +
            ",@ShippingAmountEur " +
            ",@ExpiredDays " +
            ",@IsLocked " +
            ",@IsPaymentBillDownloaded " +
            ",1" +
            "); " +
            "SELECT SCOPE_IDENTITY()",
            sale
        ).Single();
    }

    public decimal GetExchangeRateAmountToEuroByDate(long fromCurrencyId, DateTime fromDate) {
        return _remoteSyncConnection.Query<decimal>(
            "DECLARE @ExchangeRate money; " +
            "DECLARE @CrossExchangeRate money; " +
            "DECLARE @InverseCrossExchangeRate money; " +
            "DECLARE @EuroCurrencyId bigint; " +
            " " +
            "SELECT @EuroCurrencyId = (SELECT TOP(1) [Currency].ID FROM [Currency] WHERE [Currency].Deleted = 0 AND [Currency].Code = 'EUR'); " +
            "SELECT @ExchangeRate = " +
            "( " +
            "SELECT TOP(1) IIF([ExchangeRateHistory].Amount IS NOT NULL, [ExchangeRateHistory].Amount, [ExchangeRate].Amount) " +
            "FROM [ExchangeRate] " +
            "LEFT JOIN [ExchangeRateHistory] " +
            "ON [ExchangeRateHistory].ExchangeRateID = [ExchangeRate].ID " +
            "AND [ExchangeRateHistory].Created <= @FromDate " +
            "WHERE [ExchangeRate].CurrencyID = @FromCurrencyId " +
            "AND [ExchangeRate].Code = 'EUR' " +
            "AND [ExchangeRate].Deleted = 0 " +
            "ORDER BY [ExchangeRateHistory].ID DESC " +
            ") " +
            "SELECT @CrossExchangeRate = " +
            "( " +
            "SELECT TOP(1) IIF([CrossExchangeRateHistory].Amount IS NOT NULL, [CrossExchangeRateHistory].Amount, [CrossExchangeRate].Amount) " +
            "FROM [CrossExchangeRate] " +
            "LEFT JOIN [CrossExchangeRateHistory] " +
            "ON [CrossExchangeRateHistory].CrossExchangeRateID = [CrossExchangeRate].ID " +
            "AND [CrossExchangeRateHistory].Created <= @FromDate " +
            "WHERE [CrossExchangeRate].CurrencyFromID = @FromCurrencyId " +
            "AND [CrossExchangeRate].CurrencyToID = @EuroCurrencyId " +
            "AND [CrossExchangeRate].Deleted = 0 " +
            "ORDER BY [CrossExchangeRateHistory].ID DESC " +
            "); " +
            "SELECT @InverseCrossExchangeRate = " +
            "( " +
            "SELECT TOP(1) IIF([CrossExchangeRateHistory].Amount IS NOT NULL, [CrossExchangeRateHistory].Amount, [CrossExchangeRate].Amount) " +
            "FROM [CrossExchangeRate] " +
            "LEFT JOIN [CrossExchangeRateHistory] " +
            "ON [CrossExchangeRateHistory].CrossExchangeRateID = [CrossExchangeRate].ID " +
            "AND [CrossExchangeRateHistory].Created <= @FromDate " +
            "WHERE [CrossExchangeRate].CurrencyFromID = @EuroCurrencyId " +
            "AND [CrossExchangeRate].CurrencyToID = @FromCurrencyId " +
            "AND [CrossExchangeRate].Deleted = 0 " +
            "ORDER BY [CrossExchangeRateHistory].ID DESC " +
            "); " +
            "SELECT " +
            "CASE " +
            "WHEN (@FromCurrencyId = @EuroCurrencyId) " +
            "THEN 1.00 " +
            "WHEN (@ExchangeRate IS NOT NULL) " +
            "THEN @ExchangeRate " +
            "WHEN (@CrossExchangeRate IS NOT NULL) " +
            "THEN @CrossExchangeRate " +
            "WHEN (@InverseCrossExchangeRate IS NOT NULL) " +
            "THEN 0 - @InverseCrossExchangeRate " +
            "ELSE 1.00 " +
            "END ",
            new {
                FromCurrencyId = fromCurrencyId,
                FromDate = fromDate
            }
        ).Single();
    }

    public void Update(ClientAgreement agreement) {
        _remoteSyncConnection.Execute(
            "UPDATE [ClientAgreement] " +
            "SET CurrentAmount = @CurrentAmount, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            agreement
        );
    }

    public void CleanDebtsAndBalances() {
        _remoteSyncConnection.Execute(
            "DECLARE @Name nvarchar(100) = N'Ввід боргів з 1С'; " +
            "UPDATE [Sale] SET Deleted = 1 WHERE ID IN ( " +
            "SELECT [ClientInDebt].SaleID " +
            "FROM [ClientInDebt] " +
            "LEFT JOIN [Client] " +
            "ON [ClientInDebt].ClientID = Client.ID " +
            "LEFT JOIN [Debt] " +
            "ON [ClientInDebt].DebtID = [Debt].ID " +
            "WHERE [ClientInDebt].Deleted = 0 " +
            "AND [Debt].Total > 0 " +
            "AND ([Client].SourceAmgCode <> 0 " +
            "OR [Client].SourceFenixCode <> 0) " +
            "AND [Client].Deleted = 0 " +
            "); " +
            "DELETE FROM [ClientInDebt] WHERE ID IN ( " +
            "SELECT [ClientInDebt].ID " +
            "FROM [ClientInDebt] " +
            "LEFT JOIN [Client] " +
            "ON [ClientInDebt].ClientID = Client.ID " +
            "LEFT JOIN [Debt] " +
            "ON [ClientInDebt].DebtID = [Debt].ID " +
            "WHERE [ClientInDebt].Deleted = 0 " +
            "AND [Debt].Total > 0 " +
            "AND ([Client].SourceAmgCode <> 0 " +
            "OR [Client].SourceFenixCode <> 0) " +
            "AND [Client].Deleted = 0 " +
            "); " +
            "DELETE FROM [Debt] WHERE ID IN ( " +
            "SELECT [Debt].ID " +
            "FROM [Debt] " +
            "LEFT JOIN [ClientInDebt] " +
            "ON [ClientInDebt].DebtID = [Debt].ID " +
            "WHERE [ClientInDebt].ID IS NULL " +
            "); " +
            "UPDATE [ClientAgreement] SET CurrentAmount = 0 " +
            "FROM [ClientAgreement] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [ClientAgreement].Deleted = 0 " +
            "AND ([Client].SourceAmgCode <> 0 " +
            "OR [Client].SourceFenixCode <> 0) " +
            "AND [Client].Deleted = 0 " +
            "AND CurrentAmount <> 0" +
            "UPDATE [SupplyPaymentTask]" +
            "SET [Deleted] = 1 " +
            "WHERE [Comment] = @Name; " +
            "UPDATE [OutcomePaymentOrderSupplyPaymentTask]" +
            "SET [Deleted] = 1 " +
            "WHERE [SupplyPaymentTaskID] IN ( " +
            "SELECT [SupplyPaymentTask].[ID] FROM [SupplyPaymentTask] " +
            "WHERE [Comment] = @Name " +
            ") " +
            "UPDATE [OutcomePaymentOrder] " +
            "SET [Deleted] = 1 " +
            "WHERE [ClientAgreementID] IN ( " +
            "SELECT [ClientAgreement].[ID] FROM [ClientAgreement] " +
            "WHERE [OriginalClientAmgCode] IS NOT NULL OR [OriginalClientFenixCode] IS NOT NULL) " +
            "UPDATE [IncomePaymentOrder] " +
            "SET [Deleted] = 1 " +
            "WHERE [ClientID] IN ( " +
            "SELECT [Client].[ID] FROM [Client] " +
            "WHERE [SourceAmgCode] IS NOT NULL OR [SourceFenixCode] IS NOT NULL) "
        );
    }

    public void CleanDebtsAndBalancesForSupplier() {
        _remoteSyncConnection.Execute(
            "DECLARE @Name nvarchar(100) = N'Ввід боргів з 1С'; " +
            "DELETE FROM SupplyOrderPaymentDeliveryProtocol " +
            "WHERE SupplyOrderPaymentDeliveryProtocolKeyID = ( " +
            "SELECT TOP 1 SupplyOrderPaymentDeliveryProtocolKey.[ID] " +
            "FROM SupplyOrderPaymentDeliveryProtocolKey " +
            "WHERE [Key] = @Name " +
            ") " +
            "DELETE FROM [ConsignmentItem] " +
            "WHERE [ProductID] = ( " +
            "SELECT Product.ID FROM [Product] " +
            "WHERE [VendorCode] = N'Борг' " +
            ") " +
            "DELETE FROM [ProductSpecification] " +
            "WHERE [Name] = @Name " +
            "DELETE FROM ProductIncomeItem " +
            "WHERE ProductIncomeID IN ( " +
            "SELECT ProductIncome.ID FROM ProductIncome " +
            "WHERE Comment = @Name " +
            ") " +
            "DELETE FROM Consignment " +
            "WHERE StorageID = ( " +
            "SELECT TOP 1 Storage.ID FROM Storage " +
            "WHERE Name = @Name " +
            ") " +
            "DELETE FROM ProductIncome " +
            "WHERE [Comment] = @Name " +
            "DELETE FROM PackingListPackageOrderItem " +
            "WHERE PackingListID IN ( " +
            "SELECT PackingList.ID FROM PackingList " +
            "WHERE [Comment] = @Name " +
            ") " +
            "DELETE FROM PackingList " +
            "WHERE [Comment] = @Name " +
            "DELETE FROM [SupplyInvoiceOrderItem] " +
            "WHERE SupplyInvoiceID IN ( " +
            "SELECT SupplyInvoice.ID FROM SupplyInvoice " +
            "WHERE [Comment] = @Name " +
            ") " +
            "DELETE FROM [SupplyInformationDeliveryProtocol] " +
            "WHERE [SupplyInformationDeliveryProtocolKeyID] = ( " +
            "SELECT TOP 1 [SupplyInformationDeliveryProtocolKey].[ID] " +
            "FROM [SupplyInformationDeliveryProtocolKey] " +
            "WHERE [Key] = @Name " +
            ") " +
            "DELETE FROM [SupplyInvoice] " +
            "WHERE [Comment] = @Name " +
            "DELETE FROM SupplyOrderItem " +
            "WHERE Description = @Name " +
            "DELETE FROM SupplyOrder " +
            "WHERE Comment = @Name " +
            "DELETE FROM [SupplyProForm] " +
            "WHERE Number = @Name " +
            "DELETE FROM [SupplyOrderNumber] " +
            "WHERE Number = @Name "
        );
    }

    public void CleanDebtsAndBalancesForSupplyOrganizations() {
        _remoteSyncConnection.Execute(
            "DECLARE @Name nvarchar(100) = N'Ввід боргів з 1С'; " +
            "UPDATE [SupplyOrganizationAgreement] " +
            "SET [CurrentAmount] = 0, [AccountingCurrentAmount] = 0 " +
            "WHERE [SourceAmgCode] IS NOT NULL " +
            "OR [SourceFenixCode] IS NOT NULL " +
            "DELETE FROM [PaymentCostMovementOperation] " +
            "WHERE [PaymentCostMovementID] = ( " +
            "SELECT TOP 1 [PaymentCostMovement].[ID] FROM [PaymentCostMovement] " +
            "WHERE [OperationName] = @Name " +
            ") " +
            "UPDATE [ConsumablesOrderItem] " +
            "SET [Deleted] = 1 " +
            "WHERE [SupplyOrganizationAgreementID] IN ( " +
            "SELECT [SupplyOrganizationAgreement].[ID] FROM [SupplyOrganizationAgreement] " +
            "WHERE [SourceAmgCode] IS NOT NULL OR [SourceFenixCode] IS NOT NULL " +
            ") " +
            "UPDATE [ConsumablesOrder] " +
            "SET [Deleted] = 1 " +
            "WHERE [Comment] = @Name " +
            "DELETE FROM SupplyOrderPaymentDeliveryProtocol " +
            "WHERE SupplyOrderPaymentDeliveryProtocolKeyID = ( " +
            "SELECT SupplyOrderPaymentDeliveryProtocolKey.[ID] " +
            "FROM SupplyOrderPaymentDeliveryProtocolKey " +
            "WHERE SupplyOrderPaymentDeliveryProtocolKey.[Key] = @Name " +
            ") " +
            "UPDATE [SupplyPaymentTask]" +
            "SET [Deleted] = 1 " +
            "WHERE [Comment] = @Name; " +
            "UPDATE [OutcomePaymentOrderSupplyPaymentTask]" +
            "SET [Deleted] = 1 " +
            "WHERE [SupplyPaymentTaskID] IN ( " +
            "SELECT [SupplyPaymentTask].[ID] FROM [SupplyPaymentTask] " +
            "WHERE [Comment] = @Name " +
            ") " +
            "UPDATE [OutcomePaymentOrder] " +
            "SET [Deleted] = 1 " +
            "WHERE [SupplyOrganizationAgreementID] IN ( " +
            "SELECT [SupplyOrganizationAgreement].[ID] FROM [SupplyOrganizationAgreement] " +
            "WHERE [SourceAmgCode] IS NOT NULL OR [SourceFenixCode] IS NOT NULL) "
        );
    }

    public Product GetDevProduct() {
        return _remoteSyncConnection.Query<Product>(
            "SELECT TOP(1) * " +
            "FROM [Product] " +
            "WHERE [Product].VendorCode = N'Борг' " +
            "AND [Product].Deleted = 1"
        ).SingleOrDefault();
    }

    public MeasureUnit GetMeasureUnit() {
        return _remoteSyncConnection.Query<MeasureUnit>(
            "SELECT TOP(1) * FROM [MeasureUnit] WHERE Deleted = 0"
        ).SingleOrDefault();
    }

    public long Add(MeasureUnit measureUnit) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [MeasureUnit] " +
            "([Name], [Description], [CodeOneC], Updated, Deleted) " +
            "VALUES " +
            "(@Name, @Description, @CodeOneC, GETUTCDATE(), @Deleted); " +
            "SELECT SCOPE_IDENTITY()",
            measureUnit
        ).Single();
    }

    public void Add(MeasureUnitTranslation translation) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [MeasureUnitTranslation] " +
            "([Name], [Description], MeasureUnitID, [CultureCode], Updated) " +
            "VALUES " +
            "(@Name, @Description, @MeasureUnitID, @CultureCode, GETUTCDATE())",
            translation
        );
    }

    public long Add(Product product) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO Product (Description, HasAnalogue, HasComponent, HasImage, IsForSale, IsForWeb, IsForZeroSale, MainOriginalNumber, MeasureUnitID, " +
            "Name, OrderStandard, PackingStandard, Size, UCGFEA, VendorCode, Volume, Weight, [Top], NameUA, NamePL, DescriptionUA, DescriptionPL, NotesPL, NotesUA, " +
            "SynonymsUA, SynonymsPL, SearchSynonymsUA, SearchSynonymsPL, SearchName, SearchNameUA, SearchDescription, SearchDescriptionUA, SearchSize, " +
            "SearchVendorCode, SearchNamePL, SearchDescriptionPL, Updated, Deleted) " +
            "VALUES(@Description, @HasAnalogue, @HasComponent, @HasImage, @IsForSale, @IsForWeb, @IsForZeroSale, @MainOriginalNumber, @MeasureUnitID, " +
            "@Name, @OrderStandard, @PackingStandard, @Size, @UCGFEA, @VendorCode, @Volume, @Weight, @Top, @NameUA, @NamePL, @DescriptionUA, @DescriptionPL, " +
            "@NotesPL, @NotesUA, @SynonymsUA, @SynonymsPL, @SearchSynonymsUA, @SearchSynonymsPL, @SearchName, @SearchNameUA, @SearchDescription, @SearchDescriptionUA, " +
            "@SearchSize, @SearchVendorCode, @SearchNamePL, @SearchDescriptionPL, getutcdate(), @Deleted); " +
            "SELECT SCOPE_IDENTITY()",
            product
        ).Single();
    }

    public void Add(OrderItem orderItem) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [OrderItem] (ClientShoppingCartId, OrderId, UserId, ProductId, Qty, Comment, IsValidForCurrentSale, PricePerItem, OrderedQty, " +
            "FromOfferQty, IsFromOffer, ExchangeRateAmount, OneTimeDiscount, DiscountAmount, PricePerItemWithoutVat, ReturnedQty, AssignedSpecificationId, Updated) " +
            "VALUES(@ClientShoppingCartId, @OrderId, @UserId, @ProductId, @Qty, @Comment, @IsValidForCurrentSale, @PricePerItem, @OrderedQty, " +
            "@FromOfferQty, @IsFromOffer, @ExchangeRateAmount, 0.00, 0.00, 0.00, 0, @AssignedSpecificationId, getutcdate())",
            orderItem
        );
    }

    public GovExchangeRate GetGovByCurrencyIdAndCode(long id, string code, DateTime fromDate) {
        return _remoteSyncConnection.Query<GovExchangeRate>(
            "SELECT TOP(1) " +
            "[GovExchangeRate].ID, " +
            "(CASE " +
            "WHEN [GovExchangeRateHistory].Amount IS NOT NULL " +
            "THEN [GovExchangeRateHistory].Amount " +
            "ELSE [GovExchangeRate].Amount " +
            "END) AS [Amount] " +
            "FROM [GovExchangeRate] " +
            "LEFT JOIN [GovExchangeRateHistory] " +
            "ON [GovExchangeRateHistory].GovExchangeRateID = [GovExchangeRate].ID " +
            "AND [GovExchangeRateHistory].Created <= @FromDate " +
            "WHERE [GovExchangeRate].CurrencyID = @Id " +
            "AND [GovExchangeRate].Code = @Code " +
            "ORDER BY [GovExchangeRateHistory].ID DESC",
            new { Id = id, Code = code, FromDate = fromDate }
        ).FirstOrDefault();
    }

    public ExchangeRate GetByCurrencyIdAndCode(long id, string code, DateTime fromDate) {
        return _remoteSyncConnection.Query<ExchangeRate>(
            "SELECT TOP(1) " +
            "[ExchangeRate].ID, " +
            "(CASE " +
            "WHEN [ExchangeRateHistory].Amount IS NOT NULL " +
            "THEN [ExchangeRateHistory].Amount " +
            "ELSE [ExchangeRate].Amount " +
            "END) AS [Amount] " +
            "FROM [ExchangeRate] " +
            "LEFT JOIN [ExchangeRateHistory] " +
            "ON [ExchangeRateHistory].ExchangeRateID = [ExchangeRate].ID " +
            "AND [ExchangeRateHistory].Created <= @FromDate " +
            "WHERE [ExchangeRate].CurrencyID = @Id " +
            "AND [ExchangeRate].Code = @Code " +
            "ORDER BY [ExchangeRateHistory].ID DESC",
            new { Id = id, Code = code, FromDate = fromDate }
        ).FirstOrDefault();
    }

    public IEnumerable<SupplyOrganization> GetAllSupplyOrganizations() {
        List<SupplyOrganization> toReturn = new();

        _remoteSyncConnection.Query<SupplyOrganization, SupplyOrganizationAgreement, Organization, Currency, SupplyOrganization>(
            "SELECT * " +
            "FROM [SupplyOrganization] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].[SupplyOrganizationID] = [SupplyOrganization].ID " +
            "AND [SupplyOrganizationAgreement].Deleted = 0 " +
            "AND ([SupplyOrganizationAgreement].SourceAmgCode IS NOT NULL " +
            "OR [SupplyOrganizationAgreement].SourceFenixCode IS NOT NULL) " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [SupplyOrganizationAgreement].OrganizationID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "WHERE [SupplyOrganization].Deleted = 0 " +
            "AND ([SupplyOrganization].SourceAmgCode IS NOT NULL " +
            "OR [SupplyOrganization].SourceFenixCode IS NOT NULL) ",
            (supplyOrganization, agreement, organization, currency) => {
                if (agreement == null || organization == null || currency == null) return supplyOrganization;

                if (toReturn.Any(c => c.Id.Equals(supplyOrganization.Id)))
                    supplyOrganization = toReturn.First(c => c.Id.Equals(supplyOrganization.Id));
                else
                    toReturn.Add(supplyOrganization);

                agreement.Currency = currency;
                agreement.Organization = organization;

                supplyOrganization.SupplyOrganizationAgreements.Add(agreement);

                return supplyOrganization;
            }
        );

        return toReturn;
    }

    public ConsumablesStorage GetConsumablesStorageByKey(string debtsFromOneCUkKey) {
        return _remoteSyncConnection.Query<ConsumablesStorage>(
            "SELECT * " +
            "FROM [ConsumablesStorage] " +
            "WHERE [ConsumablesStorage].[Name] = @Name ",
            new { Name = debtsFromOneCUkKey }
        ).FirstOrDefault();
    }

    public Organization GetByName(string defaultOrganizationAmg) {
        return _remoteSyncConnection.Query<Organization>(
            "SELECT * " +
            "FROM [Organization] " +
            "WHERE [Organization].[Name] = @Name ",
            new { Name = defaultOrganizationAmg }
        ).FirstOrDefault();
    }

    public ConsumableProductCategory GetSupplyServiceConsumablesProductCategory() {
        return _remoteSyncConnection.Query<ConsumableProductCategory>(
            "SELECT * " +
            "FROM [ConsumableProductCategory] " +
            "WHERE [ConsumableProductCategory].[IsSupplyServiceCategory] = 1 "
        ).FirstOrDefault();
    }

    public ConsumableProduct GetConsumablesProductByKey(string debtsFromOneCUkKey) {
        return _remoteSyncConnection.Query<ConsumableProduct>(
            "SELECT * " +
            "FROM [ConsumableProduct] " +
            "WHERE [ConsumableProduct].[Name] = @DebtsFromOneCUkKey ",
            new {
                DebtsFromOneCUkKey = debtsFromOneCUkKey
            }
        ).FirstOrDefault();
    }

    public long Add(ConsumablesStorage consumablesStorage) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO [ConsumablesStorage] (Name, Description, ResponsibleUserId, OrganizationId, Updated) " +
                "VALUES (@Name, @Description, @ResponsibleUserId, @OrganizationId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                consumablesStorage
            )
            .Single();
    }

    public long Add(ConsumableProductCategory consumableProductCategory) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO [ConsumableProductCategory] (Name, Description, Updated, [IsSupplyServiceCategory]) " +
                "VALUES (@Name, @Description, getutcdate(), @IsSupplyServiceCategory); " +
                "SELECT SCOPE_IDENTITY()",
                consumableProductCategory
            )
            .Single();
    }

    public long Add(ConsumableProduct consumableProduct) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO [ConsumableProduct] (Name, VendorCode, ConsumableProductCategoryId, MeasureUnitId, Updated) " +
                "VALUES (@Name, @VendorCode, @ConsumableProductCategoryId, @MeasureUnitId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                consumableProduct
            )
            .Single();
    }

    public void Update(SupplyOrganizationAgreement agreement) {
        _remoteSyncConnection.Execute(
            "UPDATE [SupplyOrganizationAgreement] " +
            "SET [Name] = @Name" +
            ", Updated = GETUTCDATE() " +
            ",[SourceAmgCode] = @SourceAmgCode " +
            ",[SourceFenixCode] = @SourceFenixCode " +
            ",[SourceAmgID] = @SourceAmgId " +
            ",[SourceFenixID] = @SourceFenixId " +
            ", [OrganizationID] = @OrganizationId " +
            ", [ExistFrom] = @ExistFrom " +
            ", [CurrencyID] = @CurrencyId " +
            ", [CurrentAmount] = @CurrentAmount " +
            ", [AccountingCurrentAmount] = @AccountingCurrentAmount " +
            "WHERE [SupplyOrganizationAgreement].ID = @Id",
            agreement
        );
    }

    public PaymentCostMovement GetPaymentCostMovementByKey(string debtsFromOneCUkKey) {
        return _remoteSyncConnection.Query<PaymentCostMovement>(
            "SELECT * " +
            "FROM [PaymentCostMovement] " +
            "WHERE [PaymentCostMovement].[OperationName] = @DebtsFromOneCUkKey ",
            new {
                DebtsFromOneCUkKey = debtsFromOneCUkKey
            }
        ).FirstOrDefault();
    }

    public long Add(PaymentCostMovement paymentCostMovement) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO [PaymentCostMovement] (OperationName, Updated) " +
                "VALUES (@OperationName, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                paymentCostMovement
            )
            .Single();
    }

    public long Add(ConsumablesOrder consumablesOrder) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO [ConsumablesOrder] (Number, Comment, OrganizationNumber, OrganizationFromDate, IsPayed, UserId, ConsumablesStorageId, SupplyPaymentTaskId, Updated) " +
                "VALUES (@Number, @Comment, @OrganizationNumber, @OrganizationFromDate, @IsPayed, @UserId, @ConsumablesStorageId, @SupplyPaymentTaskId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                consumablesOrder
            )
            .Single();
    }

    public long Add(ConsumablesOrderItem consumablesOrderItem) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO [ConsumablesOrderItem] " +
                "(TotalPrice, PricePerItem, Qty, ConsumableProductCategoryId, ConsumablesOrderId, ConsumableProductId, ConsumableProductOrganizationId, " +
                "VAT, VatPercent, IsService, SupplyOrganizationAgreementId, Updated) " +
                "VALUES (@TotalPrice, @PricePerItem, @Qty, @ConsumableProductCategoryId, @ConsumablesOrderId, @ConsumableProductId, @ConsumableProductOrganizationId, " +
                "@VAT, @VatPercent, @IsService, @SupplyOrganizationAgreementId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                consumablesOrderItem
            )
            .Single();
    }

    public void Add(PaymentCostMovementOperation paymentCostMovementOperation) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [PaymentCostMovementOperation] " +
            "(PaymentCostMovementId, ConsumablesOrderItemId, DepreciatedConsumableOrderItemId, CompanyCarFuelingId, Updated) " +
            "VALUES (@PaymentCostMovementId, @ConsumablesOrderItemId, @DepreciatedConsumableOrderItemId, @CompanyCarFuelingId, getutcdate())",
            paymentCostMovementOperation
        );
    }

    public Storage GetDevStorage(string name) {
        return _remoteSyncConnection.Query<Storage>(
            "SELECT * FROM [Storage] " +
            "WHERE [Storage].[Name] = @Name "
            , new { Name = name }).FirstOrDefault();
    }

    public long Add(Storage storage) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO Storage (Name, Locale, ForDefective, ForVatProducts, OrganizationID, ForEcommerce, Updated, [Deleted]) " +
            "VALUES(@Name, @Locale, @ForDefective, @ForVatProducts, @OrganizationId, @ForEcommerce, getutcdate(), @Deleted); " +
            "SELECT SCOPE_IDENTITY()",
            storage
        ).Single();
    }

    public long Add(ProductIncome productIncome) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [ProductIncome] (FromDate, Number, UserId, StorageId, ProductIncomeType, Comment, Updated) " +
            "VALUES (@FromDate, @Number, @UserId, @StorageId, @ProductIncomeType, @Comment, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            productIncome
        ).Single();
    }

    public long Add(ProductIncomeItem item) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [ProductIncomeItem] " +
            "(SaleReturnItemId, ProductIncomeId, PackingListPackageOrderItemId, SupplyOrderUkraineItemId, Qty, RemainingQty, ProductCapitalizationItemId, Updated) " +
            "VALUES (@SaleReturnItemId, @ProductIncomeId, @PackingListPackageOrderItemId, @SupplyOrderUkraineItemId, @Qty, @RemainingQty, @ProductCapitalizationItemId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            item
        ).Single();
    }

    public long Add(ProductSpecification productSpecification) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [ProductSpecification] ([Name], SpecificationCode, [Locale], DutyPercent, AddedById, ProductId, IsActive, Updated, " +
            "[CustomsValue], [Duty], [VATValue], [VATPercent]) " +
            "VALUES (@Name, @SpecificationCode, @Locale, @DutyPercent, @AddedById, @ProductId, @IsActive, GETUTCDATE(), " +
            "@CustomsValue, @Duty, @VATValue, @VATPercent); " +
            "SELECT SCOPE_IDENTITY();",
            productSpecification
        ).Single();
    }

    public long Add(ConsignmentItem consignmentItem) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [ConsignmentItem] " +
            "(Qty, RemainingQty, Weight, Price, NetPrice, DutyPercent, ProductId, ConsignmentId, RootConsignmentItemId, ProductIncomeItemId, " +
            "ProductSpecificationId, Updated, AccountingPrice, [ExchangeRate]) " +
            "VALUES " +
            "(@Qty, @RemainingQty, @Weight, @Price, @NetPrice, @DutyPercent, @ProductId, @ConsignmentId, @RootConsignmentItemId, @ProductIncomeItemId, " +
            "@ProductSpecificationId, GETUTCDATE(), @AccountingPrice, @ExchangeRate); " +
            "SELECT SCOPE_IDENTITY()",
            consignmentItem
        ).Single();
    }

    public void Add(ConsignmentItemMovement movement) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [ConsignmentItemMovement] " +
            "(IsIncomeMovement, Qty, RemainingQty, MovementType, ConsignmentItemId, ProductIncomeItemId, DepreciatedOrderItemId, SupplyReturnItemId, OrderItemId, " +
            "ProductTransferItemId, OrderItemBaseShiftStatusId, TaxFreeItemId, SadItemId, Updated, ReSaleItemId) " +
            "VALUES " +
            "(@IsIncomeMovement, @Qty, @RemainingQty, @MovementType, @ConsignmentItemId, @ProductIncomeItemId, @DepreciatedOrderItemId, @SupplyReturnItemId, @OrderItemId, " +
            "@ProductTransferItemId, @OrderItemBaseShiftStatusId, @TaxFreeItemId, @SadItemId, GETUTCDATE(), @ReSaleItemId)",
            movement
        );
    }

    public long Add(Consignment consignment) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [Consignment] (IsVirtual, [FromDate], StorageId, OrganizationId, ProductIncomeId, ProductTransferId, Updated) " +
            "VALUES (@IsVirtual, @FromDate, @StorageId, @OrganizationId, @ProductIncomeId, @ProductTransferId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            consignment
        ).First();
    }

    public PaymentRegister GetDevPaymentRegister(string name) {
        PaymentRegister toReturn = null;

        _remoteSyncConnection.Query<PaymentRegister, PaymentCurrencyRegister, Currency, PaymentRegister>(
            "SELECT * FROM [PaymentRegister] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].[PaymentRegisterID] = [PaymentRegister].[ID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [PaymentCurrencyRegister].[CurrencyID] " +
            "WHERE [PaymentRegister].[Name] = @Name; ",
            (paymentRegister, paymentCurrencyRegister, currency) => {
                if (toReturn == null)
                    toReturn = paymentRegister;

                if (paymentCurrencyRegister != null &&
                    !toReturn.PaymentCurrencyRegisters.Any(x => x.Id == paymentCurrencyRegister.Id)) {
                    paymentCurrencyRegister.Currency = currency;

                    toReturn.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);
                }

                return paymentRegister;
            },
            new { Name = name });

        return toReturn;
    }


    public long Add(PaymentRegister paymentRegister) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [PaymentRegister] (Name, [Type], OrganizationId, AccountNumber, SortCode, IBAN, SwiftCode, BankName, City, FromDate, ToDate, IsActive, " +
            "Updated, [IsMain], IsForRetail, CVV, IsSelected) " +
            "VALUES (@Name, @Type, @OrganizationId, @AccountNumber, @SortCode, @IBAN, @SwiftCode, @BankName, @City, @FromDate, @ToDate, @IsActive, getutcdate(), " +
            "@IsMain, @IsForRetail, @CVV, @IsSelected); " +
            "SELECT SCOPE_IDENTITY()",
            paymentRegister
        ).Single();
    }

    public long Add(PaymentCurrencyRegister paymentCurrencyRegister) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [PaymentCurrencyRegister] (Amount, InitialAmount, PaymentRegisterId, CurrencyId, Updated) " +
            "VALUES (@Amount, @InitialAmount, @PaymentRegisterId, @CurrencyId, getutcdate()); " +
            "SELECT SCOPE_IDENTITY(); ",
            paymentCurrencyRegister
        ).Single();
    }

    public long Add(IncomePaymentOrder incomePaymentOrder) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO [IncomePaymentOrder] (Number, BankAccount, Comment, FromDate, IncomePaymentOrderType, VatPercent, VAT, Amount, ExchangeRate, IsManagementAccounting, " +
                "IsAccounting, Account, ClientId, OrganizationId, CurrencyId, PaymentRegisterId, UserId, ColleagueId, ClientAgreementId, EuroAmount, " +
                "AgreementEuroExchangeRate, OrganizationClientId, OrganizationClientAgreementId, TaxFreeId, SadId, Updated) " +
                "VALUES (@Number, @BankAccount, @Comment, @FromDate, @IncomePaymentOrderType, @VatPercent, @VAT, @Amount, @ExchangeRate, @IsManagementAccounting, " +
                "@IsAccounting, @Account, @ClientId, @OrganizationId, @CurrencyId, @PaymentRegisterId, @UserId, @ColleagueId, @ClientAgreementId, @EuroAmount, " +
                "@AgreementEuroExchangeRate, @OrganizationClientId, @OrganizationClientAgreementId, @TaxFreeId, @SadId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                incomePaymentOrder
            )
            .Single();
    }

    public PaymentMovement GetDevPaymentMovement(string name) {
        return _remoteSyncConnection.Query<PaymentMovement>(
            "SELECT * FROM [PaymentMovement] " +
            "WHERE [PaymentMovement].[OperationName] = @Name; ",
            new { Name = name }).FirstOrDefault();
    }

    public long Add(PaymentMovement paymentMovement) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO [PaymentMovement] (OperationName, Updated) " +
                "VALUES (@OperationName, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                paymentMovement
            )
            .Single();
    }

    public void Add(PaymentMovementOperation paymentMovementOperation) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [PaymentMovementOperation] (PaymentMovementId, IncomePaymentOrderId, OutcomePaymentOrderId, PaymentRegisterTransferId, PaymentRegisterCurrencyExchangeId, Updated) " +
            "VALUES (@PaymentMovementId, @IncomePaymentOrderId, @OutcomePaymentOrderId, @PaymentRegisterTransferId, @PaymentRegisterCurrencyExchangeId, getutcdate())",
            paymentMovementOperation
        );
    }

    public void Add(ClientBalanceMovement movement) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [ClientBalanceMovement] (Amount, ExchangeRateAmount, MovementType, ClientAgreementID, Updated) " +
            "VALUES (@Amount, @ExchangeRateAmount, @MovementType, @ClientAgreementId, GETUTCDATE())",
            movement
        );
    }

    public CrossExchangeRate GetByCurrenciesIds(long currencyFromId, long currencyToId, DateTime fromDate) {
        return _remoteSyncConnection.Query<CrossExchangeRate>(
            "SELECT TOP(1) " +
            "[CrossExchangeRate].ID, " +
            "(CASE " +
            "WHEN [CrossExchangeRateHistory].Amount IS NOT NULL " +
            "THEN [CrossExchangeRateHistory].Amount " +
            "ELSE [CrossExchangeRate].Amount " +
            "END) AS [Amount] " +
            "FROM [CrossExchangeRate] " +
            "LEFT JOIN [CrossExchangeRateHistory] " +
            "ON [CrossExchangeRateHistory].CrossExchangeRateID = [CrossExchangeRate].ID " +
            "AND [CrossExchangeRate].Created <= @FromDate " +
            "WHERE [CrossExchangeRate].CurrencyFromID = @FromId " +
            "AND [CrossExchangeRate].CurrencyToID = @ToId " +
            "AND [CrossExchangeRate].Deleted = 0 " +
            "ORDER BY [CrossExchangeRate].Created DESC",
            new { FromId = currencyFromId, ToId = currencyToId, FromDate = fromDate }
        ).FirstOrDefault();
    }

    public long Add(OutcomePaymentOrder outcomePaymentOrder) {
        return _remoteSyncConnection.Query<long>(
                "INSERT INTO [OutcomePaymentOrder] " +
                "(Number, Comment, FromDate, Amount, Account, UserId, OrganizationId, PaymentCurrencyRegisterId, IsUnderReport, IsUnderReportDone, " +
                "ColleagueId, AdvanceNumber, ConsumableProductOrganizationId, ExchangeRate, AfterExchangeAmount, ClientAgreementId, " +
                "SupplyOrderPolandPaymentDeliveryProtocolId, SupplyOrganizationAgreementId, VAT, VatPercent, OrganizationClientId, OrganizationClientAgreementId, " +
                "TaxFreeId, SadId, IsAccounting, IsManagementAccounting, Updated) " +
                "VALUES (@Number, @Comment, @FromDate, @Amount, @Account, @UserId, @OrganizationId, @PaymentCurrencyRegisterId, @IsUnderReport, @IsUnderReportDone, " +
                "@ColleagueId, @AdvanceNumber, @ConsumableProductOrganizationId, @ExchangeRate, @AfterExchangeAmount, @ClientAgreementId, " +
                "@SupplyOrderPolandPaymentDeliveryProtocolId, @SupplyOrganizationAgreementId, @VAT, @VatPercent, @OrganizationClientId, @OrganizationClientAgreementId, " +
                "@TaxFreeId, @SadId, @IsAccounting, @IsManagementAccounting, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                outcomePaymentOrder
            )
            .Single();
    }

    public CrossExchangeRate GetByCurrenciesIds(long currencyFromId, long currencyToId) {
        return _remoteSyncConnection.Query<CrossExchangeRate>(
            "SELECT * FROM CrossExchangeRate " +
            "WHERE CurrencyFromID = @FromId " +
            "AND CurrencyToID = @ToId " +
            "AND Deleted = 0",
            new { FromId = currencyFromId, ToId = currencyToId }
        ).FirstOrDefault();
    }

    public ExchangeRate GetByCurrencyIdAndCode(long id, string code) {
        return _remoteSyncConnection.Query<ExchangeRate>(
            "SELECT * " +
            "FROM [ExchangeRate] " +
            "WHERE [ExchangeRate].CurrencyID = @Id " +
            "AND [ExchangeRate].Code = @Code ",
            new { Id = id, Code = code }
        ).FirstOrDefault();
    }

    public void Add(OutcomePaymentOrderSupplyPaymentTask task) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [OutcomePaymentOrderSupplyPaymentTask] (Amount, OutcomePaymentOrderId, SupplyPaymentTaskId, Updated) " +
            "VALUES (@Amount, @OutcomePaymentOrderId, @SupplyPaymentTaskId, GETUTCDATE())",
            task
        );
    }

    public IncomePaymentOrder GetLastIncomePaymentOrder() {
        return _remoteSyncConnection.Query<IncomePaymentOrder>(
                "SELECT TOP(1) [IncomePaymentOrder].* " +
                "FROM [IncomePaymentOrder] " +
                "WHERE [IncomePaymentOrder].Deleted = 0 " +
                "AND [IncomePaymentOrder].[Number] NOT LIKE '%' + @FromOneC + '%' " +
                "ORDER BY [IncomePaymentOrder].ID DESC",
                new {
                    FromOneC = FROM_ONE_C
                }
            )
            .SingleOrDefault();
    }

    public OutcomePaymentOrder GetLastOutcomePaymentOrder() {
        return _remoteSyncConnection.Query<OutcomePaymentOrder>(
                "SELECT TOP(1) [OutcomePaymentOrder].* " +
                "FROM [OutcomePaymentOrder] " +
                "WHERE [OutcomePaymentOrder].Deleted = 0 " +
                "AND [OutcomePaymentOrder].[Number] NOT LIKE '%' + @FromOneC + '%' " +
                "ORDER BY [OutcomePaymentOrder].ID DESC",
                new {
                    FromOneC = FROM_ONE_C
                }
            )
            .SingleOrDefault();
    }

    public void Add(GovExchangeRate govExchangeRate) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [GovExchangeRate] (Culture, Amount, Currency, Updated, CurrencyID) " +
            "VALUES (@Culture, @Amount, @Currency, getutcdate(), @CurrencyId); ",
            govExchangeRate
        );
    }

    public void Update(ExchangeRate exchangeRate) {
        _remoteSyncConnection.Execute(
            "UPDATE [ExchangeRate] " +
            "SET [Amount] = @Amount, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            exchangeRate
        );
    }
}