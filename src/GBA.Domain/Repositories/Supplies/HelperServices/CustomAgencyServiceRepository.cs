using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.HelperServices;

public sealed class CustomAgencyServiceRepository : ICustomAgencyServiceRepository {
    private readonly IDbConnection _connection;

    public CustomAgencyServiceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(CustomAgencyService customAgencyService) {
        return _connection.Query<long>(
                "INSERT INTO CustomAgencyService (CustomAgencyOrganizationID, SupplyPaymentTaskID, UserID, IsActive, FromDate, GrossPrice, NetPrice, Vat, Number, Name, VatPercent, " +
                "ServiceNumber, SupplyOrganizationAgreementId, Updated, AccountingGrossPrice, AccountingNetPrice, AccountingVat, AccountingPaymentTaskId, AccountingVatPercent, " +
                "[AccountingSupplyCostsWithinCountry], [SupplyInformationTaskID], [ExchangeRate], [AccountingExchangeRate], [IsIncludeAccountingValue], " +
                "[ActProvidingServiceDocumentID], [SupplyServiceAccountDocumentID]) " +
                "VALUES(@CustomAgencyOrganizationID, @SupplyPaymentTaskID, @UserID, @IsActive, @FromDate, @GrossPrice, @NetPrice, @Vat, @Number, @Name, @VatPercent, " +
                "@ServiceNumber, @SupplyOrganizationAgreementId, getutcdate(), @AccountingGrossPrice, @AccountingNetPrice, @AccountingVat, @AccountingPaymentTaskId, @AccountingVatPercent, " +
                "@AccountingSupplyCostsWithinCountry, @SupplyInformationTaskId, @ExchangeRate, @AccountingExchangeRate, @IsIncludeAccountingValue, " +
                "@ActProvidingServiceDocumentId, @SupplyServiceAccountDocumentId); " +
                "SELECT SCOPE_IDENTITY()",
                customAgencyService
            )
            .Single();
    }

    public List<CustomAgencyService> GetAll() {
        return _connection.Query<CustomAgencyService>(
                "SELECT * FROM CustomAgencyService WHERE Deleted = 0"
            )
            .ToList();
    }

    public List<CustomAgencyService> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId) {
        List<CustomAgencyService> toReturn = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY ID) AS RowNumber " +
            ",ID " +
            "FROM " +
            "( " +
            "SELECT DISTINCT [CustomAgencyService].ID " +
            "FROM [CustomAgencyService] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].CustomAgencyServiceID = [CustomAgencyService].ID ";

        if (clientNetId.HasValue)
            sqlExpression += "LEFT JOIN [Client] " +
                             "ON [SupplyOrder].ClientID = [Client].ID ";

        sqlExpression += "WHERE [CustomAgencyService].Deleted = 0 " +
                         "AND [CustomAgencyService].Created >= @From " +
                         "AND [CustomAgencyService].Created <= @To " +
                         "AND [CustomAgencyService].Number like '%' + @Value + '%' ";

        if (clientNetId.HasValue) sqlExpression += "AND [Client].NetUID = @ClientNetId ";

        sqlExpression +=
            ") [Distincts] " +
            ") " +
            "SELECT * " +
            "FROM [CustomAgencyService] " +
            "LEFT JOIN [SupplyOrganization] AS [CustomAgencyOrganization] " +
            "ON [CustomAgencyOrganization].ID = [CustomAgencyService].CustomAgencyOrganizationID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [CustomAgencyService].SupplyPaymentTaskID " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].CustomAgencyServiceID = [CustomAgencyService].ID " +
            "AND [InvoiceDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].CustomAgencyServiceID = [CustomAgencyService].ID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "WHERE [CustomAgencyService].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ")";

        Type[] types = {
            typeof(CustomAgencyService),
            typeof(SupplyOrganization),
            typeof(SupplyPaymentTask),
            typeof(InvoiceDocument),
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(Client)
        };

        var props = new { Value = value, Limit = limit, Offset = offset, From = from, To = to, ClientNetId = clientNetId };

        Func<object[], CustomAgencyService> mapper = objects => {
            CustomAgencyService customAgencyService = (CustomAgencyService)objects[0];
            SupplyOrganization customAgencyOrganization = (SupplyOrganization)objects[1];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[3];
            SupplyOrder supplyOrder = (SupplyOrder)objects[4];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[5];
            Client client = (Client)objects[6];

            if (!toReturn.Any(s => s.Id.Equals(customAgencyService.Id))) {
                if (supplyOrder != null) {
                    supplyOrder.Client = client;
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                    customAgencyService.SupplyOrders.Add(supplyOrder);
                }

                if (invoiceDocument != null) customAgencyService.InvoiceDocuments.Add(invoiceDocument);

                customAgencyService.CustomAgencyOrganization = customAgencyOrganization;
                customAgencyService.SupplyPaymentTask = supplyPaymentTask;

                toReturn.Add(customAgencyService);
            } else {
                CustomAgencyService fromList = toReturn.First(s => s.Id.Equals(customAgencyService.Id));

                if (invoiceDocument != null && !fromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id))) fromList.InvoiceDocuments.Add(invoiceDocument);
            }

            return customAgencyService;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            props
        );

        return toReturn;
    }

    public CustomAgencyService GetById(long id) {
        CustomAgencyService customAgencyServiceToReturn = null;

        string sqlExpression = "SELECT * FROM CustomAgencyService " +
                               "LEFT OUTER JOIN [SupplyOrganization] AS CustomAgencyOrganization " +
                               "ON CustomAgencyOrganization.ID = CustomAgencyService.CustomAgencyOrganizationID " +
                               "LEFT OUTER JOIN [User] AS [CustomAgencyService.User] " +
                               "ON [CustomAgencyService.User].ID = CustomAgencyService.UserID " +
                               "LEFT OUTER JOIN SupplyPaymentTask " +
                               "ON SupplyPaymentTask.ID = CustomAgencyService.SupplyPaymentTaskID AND SupplyPaymentTask.Deleted = 0 " +
                               "LEFT OUTER JOIN [User] AS [SupplyPaymentTask.User] " +
                               "ON [SupplyPaymentTask.User].ID = SupplyPaymentTask.UserID " +
                               "LEFT OUTER JOIN InvoiceDocument " +
                               "ON InvoiceDocument.CustomAgencyServiceID = CustomAgencyService.ID AND InvoiceDocument.Deleted = 0 " +
                               "WHERE CustomAgencyService.ID = @Id";

        Type[] types = {
            typeof(CustomAgencyService),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(InvoiceDocument)
        };

        Func<object[], CustomAgencyService> mapper = objects => {
            CustomAgencyService customAgencyService = (CustomAgencyService)objects[0];
            SupplyOrganization customAgencyOrganization = (SupplyOrganization)objects[1];
            User customAgencyServiceUser = (User)objects[2];
            SupplyPaymentTask paymentTask = (SupplyPaymentTask)objects[3];
            User paymentTaskUser = (User)objects[4];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[5];

            if (customAgencyServiceUser != null) customAgencyService.User = customAgencyServiceUser;

            if (customAgencyOrganization != null) customAgencyService.CustomAgencyOrganization = customAgencyOrganization;

            if (paymentTask != null) {
                paymentTask.User = paymentTaskUser;
                customAgencyService.SupplyPaymentTask = paymentTask;
            }

            if (invoiceDocument != null) customAgencyService.InvoiceDocuments.Add(invoiceDocument);

            if (customAgencyServiceToReturn != null) {
                if (invoiceDocument != null && !customAgencyServiceToReturn.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                    customAgencyServiceToReturn.InvoiceDocuments.Add(invoiceDocument);
            } else {
                customAgencyServiceToReturn = customAgencyService;
            }

            return customAgencyService;
        };

        var props = new { Id = id };

        _connection.Query(sqlExpression, types, mapper, props);

        return customAgencyServiceToReturn;
    }

    public CustomAgencyService GetByNetId(Guid netId) {
        CustomAgencyService customAgencyServiceToReturn = null;

        string sqlExpression = "SELECT * FROM CustomAgencyService " +
                               "LEFT OUTER JOIN [SupplyOrganization] AS CustomAgencyOrganization " +
                               "ON CustomAgencyOrganization.ID = CustomAgencyService.CustomAgencyOrganizationID " +
                               "LEFT OUTER JOIN [User] AS [CustomAgencyService.User] " +
                               "ON [CustomAgencyService.User].ID = CustomAgencyService.UserID " +
                               "LEFT OUTER JOIN SupplyPaymentTask " +
                               "ON SupplyPaymentTask.ID = CustomAgencyService.SupplyPaymentTaskID AND SupplyPaymentTask.Deleted = 0 " +
                               "LEFT OUTER JOIN [User] AS [SupplyPaymentTask.User] " +
                               "ON [SupplyPaymentTask.User].ID = SupplyPaymentTask.UserID " +
                               "LEFT OUTER JOIN InvoiceDocument " +
                               "ON InvoiceDocument.CustomAgencyServiceID = CustomAgencyService.ID AND InvoiceDocument.Deleted = 0 " +
                               "WHERE CustomAgencyService.NetUID = @NetId";

        Type[] types = {
            typeof(CustomAgencyService),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(InvoiceDocument)
        };

        Func<object[], CustomAgencyService> mapper = objects => {
            CustomAgencyService customAgencyService = (CustomAgencyService)objects[0];
            SupplyOrganization customAgencyOrganization = (SupplyOrganization)objects[1];
            User customAgencyServiceUser = (User)objects[2];
            SupplyPaymentTask paymentTask = (SupplyPaymentTask)objects[3];
            User paymentTaskUser = (User)objects[4];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[5];

            if (customAgencyServiceUser != null) customAgencyService.User = customAgencyServiceUser;

            if (customAgencyOrganization != null) customAgencyService.CustomAgencyOrganization = customAgencyOrganization;

            if (paymentTask != null) {
                paymentTask.User = paymentTaskUser;
                customAgencyService.SupplyPaymentTask = paymentTask;
            }

            if (invoiceDocument != null) customAgencyService.InvoiceDocuments.Add(invoiceDocument);

            if (customAgencyServiceToReturn != null) {
                if (invoiceDocument != null && !customAgencyServiceToReturn.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                    customAgencyServiceToReturn.InvoiceDocuments.Add(invoiceDocument);
            } else {
                customAgencyServiceToReturn = customAgencyService;
            }

            return customAgencyService;
        };

        var props = new { NetId = netId };

        _connection.Query(sqlExpression, types, mapper, props);

        return customAgencyServiceToReturn;
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE CustomAgencyService SET Deleted = 1 WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void Update(CustomAgencyService customAgencyService) {
        _connection.Execute(
            "UPDATE CustomAgencyService " +
            "SET CustomAgencyOrganizationID = @CustomAgencyOrganizationID, SupplyPaymentTaskID = @SupplyPaymentTaskID, Number = @Number, Name = @Name, VatPercent = @VatPercent, " +
            "UserID = @UserID, IsActive = @IsActive, FromDate = @FromDate, GrossPrice = @GrossPrice, NetPrice = @NetPrice, Vat = @Vat, Updated = getutcdate(), " +
            "AccountingGrossPrice = @AccountingGrossPrice, AccountingNetPrice = @AccountingNetPrice, AccountingVat = @AccountingVat, AccountingPaymentTaskId = @AccountingPaymentTaskId, " +
            "AccountingVatPercent = @AccountingVatPercent, SupplyOrganizationAgreementId = @SupplyOrganizationAgreementId, " +
            "[AccountingSupplyCostsWithinCountry] = @AccountingSupplyCostsWithinCountry, [SupplyInformationTaskID] = @SupplyInformationTaskId, " +
            "[ExchangeRate] = @ExchangeRate, [AccountingExchangeRate] = @AccountingExchangeRate, [IsIncludeAccountingValue] = @IsIncludeAccountingValue, " +
            "[ActProvidingServiceDocumentID] = @ActProvidingServiceDocumentId, [SupplyServiceAccountDocumentID] = @SupplyServiceAccountDocumentId " +
            "WHERE NetUID = @NetUID",
            customAgencyService
        );
    }

    public CustomAgencyService GetByIdWithoutIncludes(long id) {
        return _connection.Query<CustomAgencyService>(
            "SELECT * FROM [CustomAgencyService] " +
            "WHERE [CustomAgencyService].[ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }
}