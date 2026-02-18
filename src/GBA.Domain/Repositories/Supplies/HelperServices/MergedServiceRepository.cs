using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.HelperServices;

public sealed class MergedServiceRepository : IMergedServiceRepository {
    private readonly IDbConnection _connection;

    public MergedServiceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(MergedService mergedService) {
        return _connection.Query<long>(
                "INSERT INTO [MergedService] " +
                "(IsActive, FromDate, GrossPrice, NetPrice, Vat, VatPercent, Number, ServiceNumber, Name, UserId, SupplyPaymentTaskId, SupplyOrganizationAgreementId, " +
                "SupplyOrganizationId, SupplyOrderId, SupplyOrderUkraineId, Updated, AccountingGrossPrice, AccountingNetPrice, AccountingVat, AccountingPaymentTaskId, AccountingVatPercent, " +
                "DeliveryProductProtocolID, IsAutoCalculatedValue, [AccountingSupplyCostsWithinCountry], [SupplyInformationTaskID], " +
                "[ExchangeRate], [AccountingExchangeRate], [IsIncludeAccountingValue], [ActProvidingServiceDocumentID], [SupplyServiceAccountDocumentID], [ConsumableProductID], " +
                "[ActProvidingServiceID], [AccountingActProvidingServiceID]) " +
                "VALUES " +
                "(@IsActive, @FromDate, @GrossPrice, @NetPrice, @Vat, @VatPercent, @Number, @ServiceNumber, @Name, @UserId, @SupplyPaymentTaskId, @SupplyOrganizationAgreementId, " +
                "@SupplyOrganizationId, @SupplyOrderId, @SupplyOrderUkraineId, GETUTCDATE(), @AccountingGrossPrice, @AccountingNetPrice, @AccountingVat, @AccountingPaymentTaskId, @AccountingVatPercent, " +
                "@DeliveryProductProtocolID, @IsAutoCalculatedValue, @AccountingSupplyCostsWithinCountry, @SupplyInformationTaskId, " +
                "@ExchangeRate, @AccountingExchangeRate, @IsIncludeAccountingValue, @ActProvidingServiceDocumentId, @SupplyServiceAccountDocumentId, @ConsumableProductId, " +
                "@ActProvidingServiceId, @AccountingActProvidingServiceId); " +
                "SELECT SCOPE_IDENTITY()",
                mergedService
            )
            .Single();
    }

    public void Add(IEnumerable<MergedService> mergedServices) {
        _connection.Execute(
            "INSERT INTO [MergedService] " +
            "(IsActive, FromDate, GrossPrice, NetPrice, Vat, VatPercent, Number, ServiceNumber, Name, UserId, SupplyPaymentTaskId, SupplyOrganizationAgreementId, " +
            "SupplyOrganizationId, SupplyOrderId, SupplyOrderUkraineId, Updated, AccountingGrossPrice, AccountingNetPrice, AccountingVat, AccountingPaymentTaskId, AccountingVatPercent, " +
            "DeliveryProductProtocolID, IsAutoCalculatedValue, [AccountingSupplyCostsWithinCountry], [SupplyInformationTaskID], " +
            "[ExchangeRate], [AccountingExchangeRate], [IsIncludeAccountingValue], [ActProvidingServiceDocumentID], [SupplyServiceAccountDocumentID], [ConsumableProductID], " +
            "[ActProvidingServiceID], [AccountingActProvidingServiceID]) " +
            "VALUES " +
            "(@IsActive, @FromDate, @GrossPrice, @NetPrice, @Vat, @VatPercent, @Number, @ServiceNumber, @Name, @UserId, @SupplyPaymentTaskId, @SupplyOrganizationAgreementId, " +
            "@SupplyOrganizationId, @SupplyOrderId, @SupplyOrderUkraineId, GETUTCDATE(), @AccountingGrossPrice, @AccountingNetPrice, @AccountingVat, @AccountingPaymentTaskId, @AccountingVatPercent, " +
            "@DeliveryProductProtocolID, @IsAutoCalculatedValue, @AccountingSupplyCostsWithinCountry, @SupplyInformationTaskId, " +
            "@ExchangeRate, @AccountingExchangeRate, @IsIncludeAccountingValue, @ActProvidingServiceDocumentId, @SupplyServiceAccountDocumentId, @ConsumableProductId, " +
            "@ActProvidingServiceId, @AccountingActProvidingServiceId)",
            mergedServices
        );
    }

    public void Update(MergedService mergedService) {
        _connection.Execute(
            "UPDATE [MergedService] " +
            "SET IsActive = @IsActive, FromDate = @FromDate, GrossPrice = @GrossPrice, NetPrice = @NetPrice, Vat = @Vat, VatPercent = @VatPercent, Number = @Number, " +
            "Name = @Name, UserId = @UserId, SupplyPaymentTaskId = @SupplyPaymentTaskId, SupplyOrderUkraineId = @SupplyOrderUkraineId, SupplyOrderId = @SupplyOrderId, " +
            "SupplyOrganizationAgreementId = @SupplyOrganizationAgreementId, SupplyOrganizationId = @SupplyOrganizationId, Updated = GETUTCDATE(), " +
            "AccountingGrossPrice = @AccountingGrossPrice, AccountingNetPrice = @AccountingNetPrice, AccountingVat = @AccountingVat, AccountingPaymentTaskId = @AccountingPaymentTaskId, " +
            "AccountingVatPercent = @AccountingVatPercent, DeliveryProductProtocolID = @DeliveryProductProtocolID," +
            "IsCalculatedValue = @IsCalculatedValue, IsAutoCalculatedValue = @IsAutoCalculatedValue, SupplyExtraChargeType = @SupplyExtraChargeType, " +
            "[AccountingSupplyCostsWithinCountry] = @AccountingSupplyCostsWithinCountry, [SupplyInformationTaskID] = @SupplyInformationTaskId, " +
            "[ExchangeRate] = @ExchangeRate, [AccountingExchangeRate] = @AccountingExchangeRate, [IsIncludeAccountingValue] = @IsIncludeAccountingValue," +
            "[ActProvidingServiceDocumentID] = @ActProvidingServiceDocumentId, [SupplyServiceAccountDocumentID] = @SupplyServiceAccountDocumentId, " +
            "[ConsumableProductID] = @ConsumableProductId, [ActProvidingServiceID] = @ActProvidingServiceId, " +
            "[AccountingActProvidingServiceID] = @AccountingActProvidingServiceId " +
            "WHERE ID = @Id",
            mergedService
        );
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [MergedService] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public List<MergedService> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId) {
        List<MergedService> toReturn = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY ID) AS RowNumber " +
            ",ID " +
            "FROM " +
            "( " +
            "SELECT DISTINCT [MergedService].ID " +
            "FROM [MergedService] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [MergedService].SupplyOrderID ";

        if (clientNetId.HasValue)
            sqlExpression += "LEFT JOIN [Client] " +
                             "ON [SupplyOrder].ClientID = [Client].ID ";

        sqlExpression += "WHERE [MergedService].Deleted = 0 " +
                         "AND [MergedService].Created >= @From " +
                         "AND [MergedService].Created <= @To " +
                         "AND [MergedService].Number like '%' + @Value + '%' ";

        if (clientNetId.HasValue) sqlExpression += "AND [Client].NetUID = @ClientNetId ";

        sqlExpression +=
            ") [Distincts] " +
            ") " +
            "SELECT * " +
            "FROM [MergedService] " +
            "LEFT JOIN [SupplyOrganization]  " +
            "ON [SupplyOrganization].ID = [MergedService].SupplyOrganizationID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [MergedService].SupplyPaymentTaskID " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].MergedServiceID = [MergedService].ID " +
            "AND [InvoiceDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [MergedService].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "WHERE [MergedService].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ")";

        Type[] types = {
            typeof(MergedService),
            typeof(SupplyOrganization),
            typeof(SupplyPaymentTask),
            typeof(InvoiceDocument),
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(Client)
        };

        Func<object[], MergedService> mapper = objects => {
            MergedService mergedService = (MergedService)objects[0];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[1];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[3];
            SupplyOrder supplyOrder = (SupplyOrder)objects[4];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[5];
            Client client = (Client)objects[6];

            if (!toReturn.Any(s => s.Id.Equals(mergedService.Id))) {
                if (supplyOrder != null) {
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                    supplyOrder.Client = client;

                    mergedService.SupplyOrder = supplyOrder;
                }

                if (invoiceDocument != null) mergedService.InvoiceDocuments.Add(invoiceDocument);

                mergedService.SupplyPaymentTask = supplyPaymentTask;
                mergedService.SupplyOrganization = supplyOrganization;

                toReturn.Add(mergedService);
            } else {
                MergedService fromList = toReturn.First(s => s.Id.Equals(mergedService.Id));

                if (invoiceDocument != null && !fromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id))) fromList.InvoiceDocuments.Add(invoiceDocument);
            }

            return mergedService;
        };

        var props = new { Value = value, Limit = limit, Offset = offset, From = from, To = to, ClientNetId = clientNetId };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            props
        );

        return toReturn;
    }

    public List<MergedService> GetAllForExport(DateTime from, DateTime to) {
        List<MergedService> toReturn = new();

        string sqlExpression = @"
;WITH [Search_CTE] 
AS (
    SELECT ROW_NUMBER() OVER (ORDER BY ID) AS RowNumber, ID
    FROM (
        SELECT DISTINCT [MergedService].ID
        FROM [MergedService]
        LEFT JOIN [SupplyOrder]
            ON [SupplyOrder].ID = [MergedService].SupplyOrderID
        WHERE [MergedService].Deleted = 0
          AND [MergedService].Created >= @From
          AND [MergedService].Created <= @To
          AND [MergedService].SupplyOrderID IS NOT NULL
    ) [Distincts]
)
SELECT 
    [MergedService].*,
    [SupplyOrganization].*,
    [SupplyOrganizationAgreement].*,
    [Currency].*,
    [SupplyPaymentTask].*,
    [InvoiceDocument].*,
    [SupplyOrder].*,
    [SupplyOrderNumber].*,
    [Client].*,
    [PackingListPackageOrderItemSupplyService].*,
    [dbo].GetGovExchangedToEuroValue(
        [PackingListPackageOrderItemSupplyService].[NetValue],
        [Currency].[ID],
        [PackingListPackageOrderItemSupplyService].[ExchangeRateDate]
    ) AS [NetValueEur],
    [dbo].GetGovExchangedToUahValue(
        [PackingListPackageOrderItemSupplyService].[NetValue],
        [Currency].[ID],
        [PackingListPackageOrderItemSupplyService].[ExchangeRateDate]
    ) AS [NetValueUah],
    [dbo].GetGovExchangedToEuroValue(
        [PackingListPackageOrderItemSupplyService].[GeneralValue],
        [Currency].[ID],
        [PackingListPackageOrderItemSupplyService].[ExchangeRateDate]
    ) AS [GeneralValueEur],
    [dbo].GetGovExchangedToUahValue(
        [PackingListPackageOrderItemSupplyService].[GeneralValue],
        [Currency].[ID],
        [PackingListPackageOrderItemSupplyService].[ExchangeRateDate]
    ) AS [GeneralValueUah],
    [dbo].GetGovExchangedToEuroValue(
        [PackingListPackageOrderItemSupplyService].[ManagementValue],
        [Currency].[ID],
        [PackingListPackageOrderItemSupplyService].[ExchangeRateDate]
    ) AS [ManagementValueEur],
    [dbo].GetGovExchangedToUahValue(
        [PackingListPackageOrderItemSupplyService].[ManagementValue],
        [Currency].[ID],
        [PackingListPackageOrderItemSupplyService].[ExchangeRateDate]
    ) AS [ManagementValueUah],
    [PackingListPackageOrderItem].*,
    [SupplyInvoiceOrderItem].*,
    [Product].*,
    [MeasureUnit].*
FROM [MergedService]
LEFT JOIN [SupplyOrganization]
    ON [SupplyOrganization].ID = [MergedService].SupplyOrganizationID
LEFT JOIN [SupplyOrganizationAgreement]
    ON [SupplyOrganizationAgreement].ID =  [MergedService].SupplyOrganizationAgreementID
LEFT JOIN [SupplyPaymentTask]
    ON [SupplyPaymentTask].ID = [MergedService].SupplyPaymentTaskID
LEFT JOIN [InvoiceDocument]
    ON [InvoiceDocument].MergedServiceID = [MergedService].ID
    AND [InvoiceDocument].Deleted = 0
LEFT JOIN [SupplyOrder]
    ON [SupplyOrder].ID = [MergedService].SupplyOrderID
LEFT JOIN [SupplyOrderNumber]
    ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID
LEFT JOIN [Client]
    ON [Client].ID = [SupplyOrder].ClientID
LEFT JOIN [PackingListPackageOrderItemSupplyService]
    ON [PackingListPackageOrderItemSupplyService].[MergedServiceID] = [MergedService].[ID]
    AND [PackingListPackageOrderItemSupplyService].[Deleted] = 0
LEFT JOIN [Currency]
    ON [Currency].[ID] = [PackingListPackageOrderItemSupplyService].[CurrencyID]
LEFT JOIN [PackingListPackageOrderItem]
    ON [PackingListPackageOrderItem].[ID] = [PackingListPackageOrderItemSupplyService].PackingListPackageOrderItemID
    AND [PackingListPackageOrderItem].Deleted = 0
LEFT JOIN [SupplyInvoiceOrderItem]
    ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID
LEFT JOIN [Product]
    ON [Product].ID = [SupplyInvoiceOrderItem].ProductID
LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit]
    ON [MeasureUnit].ID = [Product].MeasureUnitID
    AND [MeasureUnit].CultureCode = @Culture
WHERE [MergedService].ID IN (
    SELECT [Search_CTE].ID 
    FROM [Search_CTE]
    -- WHERE [Search_CTE].RowNumber > @Offset 
    -- AND [Search_CTE].RowNumber <= @Limit + @Offset 
)
AND [MergedService].Number <> N'Ввід залишків з 1С.';
";

        Type[] types = {
            typeof(MergedService),
            typeof(SupplyOrganization),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(SupplyPaymentTask),
            typeof(InvoiceDocument),
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(Client),
            typeof(PackingListPackageOrderItemSupplyService),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(Product),
            typeof(MeasureUnit)
        };

        Func<object[], MergedService> mapper = objects => {
            MergedService mergedService = (MergedService)objects[0];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[1];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
            Currency currency = (Currency)objects[3];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[4];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[5];
            SupplyOrder supplyOrder = (SupplyOrder)objects[6];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[7];
            Client client = (Client)objects[8];
            PackingListPackageOrderItemSupplyService packageOrderItemSupplyService = (PackingListPackageOrderItemSupplyService)objects[9];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[10];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[11];
            Product product = (Product)objects[12];
            MeasureUnit measureUnit = (MeasureUnit)objects[13];

            if (!toReturn.Any(s => s.Id.Equals(mergedService.Id))) {
                if (supplyOrder != null) {
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                    supplyOrder.Client = client;

                    mergedService.SupplyOrder = supplyOrder;
                }

                if (invoiceDocument != null) mergedService.InvoiceDocuments.Add(invoiceDocument);

                mergedService.SupplyPaymentTask = supplyPaymentTask;
                mergedService.SupplyOrganization = supplyOrganization;
                supplyOrganizationAgreement.Currency = currency;
                mergedService.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                product.MeasureUnit = measureUnit;
                supplyInvoiceOrderItem.Product = product;
                packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;
                packageOrderItemSupplyService.PackingListPackageOrderItem = packingListPackageOrderItem;
                packageOrderItemSupplyService.Currency = currency;

                mergedService.PackingListPackageOrderItemSupplyServices.Add(packageOrderItemSupplyService);

                toReturn.Add(mergedService);
            } else {
                MergedService fromList = toReturn.First(s => s.Id.Equals(mergedService.Id));

                if (invoiceDocument != null && !fromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id))) fromList.InvoiceDocuments.Add(invoiceDocument);

                if (fromList.PackingListPackageOrderItemSupplyServices.Any(p => p.Id.Equals(packageOrderItemSupplyService.Id))) return mergedService;

                product.MeasureUnit = measureUnit;
                supplyInvoiceOrderItem.Product = product;
                packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;
                packageOrderItemSupplyService.PackingListPackageOrderItem = packingListPackageOrderItem;
                packageOrderItemSupplyService.Currency = currency;

                fromList.PackingListPackageOrderItemSupplyServices.Add(packageOrderItemSupplyService);
            }

            return mergedService;
        };

        var props = new { From = from, To = to, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            props
        );

        return toReturn;
    }

    public MergedService GetByIdWithoutIncludes(long id) {
        return _connection.Query<MergedService>(
            "SELECT * FROM [MergedService] " +
            "WHERE [ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }

    public DeliveryProductProtocol GetDeliveryProductProtocolByNetId(Guid netId) {
        return _connection.Query<DeliveryProductProtocol>(
            "SELECT * " +
            "FROM [MergedService] " +
            "LEFT JOIN [DeliveryProductProtocol] " +
            "ON [DeliveryProductProtocol].[ID] = [MergedService].[DeliveryProductProtocolID] " +
            "WHERE [MergedService].[NetUID] = @NetId; ",
            new { NetId = netId }).FirstOrDefault();
    }

    public void RemoveById(long id) {
        _connection.Execute(
            "UPDATE [MergedService] " +
            "SET [Deleted] = 1 " +
            ", [Updated] = getutcdate() " +
            "WHERE [MergedService].[ID] = @Id; ",
            new { Id = id });
    }

    public MergedService GetWithoutIncludesByNetId(Guid netId) {
        return _connection.Query<MergedService, SupplyOrganizationAgreement, MergedService>(
            "SELECT * FROM [MergedService] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].[ID] = [MergedService].[SupplyOrganizationAgreementID] " +
            "WHERE [MergedService].[NetUID] = @NetId; ",
            (service, agreement) => {
                service.SupplyOrganizationAgreement = agreement;
                return service;
            },
            new { NetId = netId }).FirstOrDefault();
    }

    public long GetDeliveryProductProtocolIdByNetId(Guid netId) {
        return _connection.Query<long>(
            "SELECT [MergedService].[DeliveryProductProtocolID] " +
            "FROM [MergedService] " +
            "WHERE [MergedService].[NetUID] = @NetId; ",
            new { NetId = netId }).Single();
    }

    public void UpdateIsCalculatedValueById(long id, bool isAuto) {
        _connection.Execute(
            "UPDATE [MergedService] " +
            "SET [IsCalculatedValue] = 1 " +
            ", [IsAutoCalculatedValue] = @IsAuto " +
            "WHERE [ID] = @Id; ",
            new { Id = id, IsAuto = isAuto });
    }

    public void UpdateSupplyExtraChargeTypeById(long id, SupplyExtraChargeType type) {
        _connection.Execute(
            "UPDATE [MergedService] " +
            "SET SupplyExtraChargeType = @ExtraChargeType " +
            "WHERE [ID] = @Id; ",
            new { Id = id, ExtraChargeType = type });
    }

    public void ResetIsCalculatedValueById(long id) {
        _connection.Execute(
            "UPDATE [MergedService] " +
            "SET [IsCalculatedValue] = 0 " +
            "WHERE [ID] = @Id; ",
            new { Id = id });
    }
}