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

public sealed class PlaneDeliveryServiceRepository : IPlaneDeliveryServiceRepository {
    private readonly IDbConnection _connection;

    public PlaneDeliveryServiceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(PlaneDeliveryService planeDeliveryService) {
        return _connection.Query<long>(
                "INSERT INTO PlaneDeliveryService (PlaneDeliveryOrganizationID, SupplyPaymentTaskID, UserID, IsActive, FromDate, GrossPrice, NetPrice, Vat, Number, Name, VatPercent, " +
                "ServiceNumber, SupplyOrganizationAgreementId, Updated, AccountingGrossPrice, AccountingNetPrice, AccountingVat, AccountingPaymentTaskId, " +
                "AccountingVatPercent, [AccountingSupplyCostsWithinCountry], [SupplyInformationTaskID], " +
                "[ExchangeRate], [AccountingExchangeRate], [IsIncludeAccountingValue], [ActProvidingServiceDocumentID], [SupplyServiceAccountDocumentID]) " +
                "VALUES(@PlaneDeliveryOrganizationID, @SupplyPaymentTaskID, @UserID, @IsActive, @FromDate, @GrossPrice, @NetPrice, @Vat, @Number, @Name, @VatPercent, " +
                "@ServiceNumber, @SupplyOrganizationAgreementId, getutcdate(), @AccountingGrossPrice, @AccountingNetPrice, @AccountingVat, @AccountingPaymentTaskId, " +
                "@AccountingVatPercent, @AccountingSupplyCostsWithinCountry, @SupplyInformationTaskId, " +
                "@ExchangeRate, @AccountingExchangeRate, @IsIncludeAccountingValue, @ActProvidingServiceDocumentId, @SupplyServiceAccountDocumentId); " +
                "SELECT SCOPE_IDENTITY()",
                planeDeliveryService
            )
            .Single();
    }

    public List<PlaneDeliveryService> GetAll() {
        return _connection.Query<PlaneDeliveryService>(
                "SELECT * FROM PlaneDeliveryService WHERE Deleted = 0"
            )
            .ToList();
    }

    public List<PlaneDeliveryService> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId) {
        List<PlaneDeliveryService> toReturn = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY ID) AS RowNumber " +
            ",ID " +
            "FROM " +
            "( " +
            "SELECT DISTINCT [PlaneDeliveryService].ID " +
            "FROM [PlaneDeliveryService] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].PlaneDeliveryServiceID = [PlaneDeliveryService].ID ";

        if (clientNetId.HasValue)
            sqlExpression += "LEFT JOIN [Client] " +
                             "ON [SupplyOrder].ClientID = [Client].ID ";

        sqlExpression += "WHERE [PlaneDeliveryService].Deleted = 0 " +
                         "AND [PlaneDeliveryService].Created >= @From " +
                         "AND [PlaneDeliveryService].Created <= @To " +
                         "AND [PlaneDeliveryService].Number like '%' + @Value + '%' ";

        if (clientNetId.HasValue) sqlExpression += "AND [Client].NetUID = @ClientNetId ";

        sqlExpression +=
            ") [Distincts] " +
            ") " +
            "SELECT * " +
            "FROM [PlaneDeliveryService] " +
            "LEFT JOIN [SupplyOrganization] AS [PlaneDeliveryOrganization] " +
            "ON [PlaneDeliveryOrganization].ID = [PlaneDeliveryService].PlaneDeliveryOrganizationID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [PlaneDeliveryService].SupplyPaymentTaskID " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
            "AND [InvoiceDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "WHERE [PlaneDeliveryService].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ")";

        Type[] types = {
            typeof(PlaneDeliveryService),
            typeof(SupplyOrganization),
            typeof(SupplyPaymentTask),
            typeof(InvoiceDocument),
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(Client)
        };

        Func<object[], PlaneDeliveryService> mapper = objects => {
            PlaneDeliveryService planeDeliveryService = (PlaneDeliveryService)objects[0];
            SupplyOrganization planeDeliveryOrganization = (SupplyOrganization)objects[1];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[3];
            SupplyOrder supplyOrder = (SupplyOrder)objects[4];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[5];
            Client client = (Client)objects[6];

            if (!toReturn.Any(s => s.Id.Equals(planeDeliveryService.Id))) {
                if (supplyOrder != null) {
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                    supplyOrder.Client = client;

                    planeDeliveryService.SupplyOrders.Add(supplyOrder);
                }

                if (invoiceDocument != null) planeDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                planeDeliveryService.SupplyPaymentTask = supplyPaymentTask;
                planeDeliveryService.PlaneDeliveryOrganization = planeDeliveryOrganization;

                toReturn.Add(planeDeliveryService);
            } else {
                PlaneDeliveryService fromList = toReturn.First(s => s.Id.Equals(planeDeliveryService.Id));

                if (invoiceDocument != null && !fromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id))) fromList.InvoiceDocuments.Add(invoiceDocument);
            }

            return planeDeliveryService;
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

    public PlaneDeliveryService GetById(long id) {
        PlaneDeliveryService planeDeliveryServiceToReturn = null;

        string sqlExpression = "SELECT * FROM PlaneDeliveryService " +
                               "LEFT OUTER JOIN [SupplyOrganization] AS PlaneDeliveryOrganization " +
                               "ON PlaneDeliveryOrganization.ID = PlaneDeliveryService.PlaneDeliveryOrganizationID " +
                               "LEFT OUTER JOIN [User] AS [PlaneDeliveryService.User] " +
                               "ON [PlaneDeliveryService.User].ID = PlaneDeliveryService.UserID " +
                               "LEFT OUTER JOIN SupplyPaymentTask " +
                               "ON SupplyPaymentTask.ID = PlaneDeliveryService.SupplyPaymentTaskID AND SupplyPaymentTask.Deleted = 0 " +
                               "LEFT OUTER JOIN [User] AS [SupplyPaymentTask.User] " +
                               "ON [SupplyPaymentTask.User].ID = SupplyPaymentTask.UserID " +
                               "LEFT OUTER JOIN InvoiceDocument " +
                               "ON InvoiceDocument.PlaneDeliveryServiceID = PlaneDeliveryService.ID AND InvoiceDocument.Deleted = 0 " +
                               "WHERE PlaneDeliveryService.ID = @Id";

        Type[] types = {
            typeof(PlaneDeliveryService),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(InvoiceDocument)
        };

        Func<object[], PlaneDeliveryService> mapper = objects => {
            PlaneDeliveryService planeDeliveryService = (PlaneDeliveryService)objects[0];
            SupplyOrganization planeDeliveryOrganization = (SupplyOrganization)objects[1];
            User planeDeliveryServiceUser = (User)objects[2];
            SupplyPaymentTask paymentTask = (SupplyPaymentTask)objects[3];
            User paymentTaskUser = (User)objects[4];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[5];

            if (planeDeliveryServiceUser != null) planeDeliveryService.User = planeDeliveryServiceUser;

            if (planeDeliveryOrganization != null) planeDeliveryService.PlaneDeliveryOrganization = planeDeliveryOrganization;

            if (paymentTask != null) {
                paymentTask.User = paymentTaskUser;
                planeDeliveryService.SupplyPaymentTask = paymentTask;
            }

            if (invoiceDocument != null) planeDeliveryService.InvoiceDocuments.Add(invoiceDocument);

            if (planeDeliveryServiceToReturn != null) {
                if (invoiceDocument != null && !planeDeliveryServiceToReturn.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                    planeDeliveryServiceToReturn.InvoiceDocuments.Add(invoiceDocument);
            } else {
                planeDeliveryServiceToReturn = planeDeliveryService;
            }

            return planeDeliveryService;
        };

        var props = new { Id = id };

        _connection.Query(sqlExpression, types, mapper, props);

        return planeDeliveryServiceToReturn;
    }

    public PlaneDeliveryService GetByNetId(Guid netId) {
        PlaneDeliveryService planeDeliveryServiceToReturn = null;

        string sqlExpression = "SELECT * FROM PlaneDeliveryService " +
                               "LEFT OUTER JOIN [SupplyOrganization] AS PlaneDeliveryOrganization " +
                               "ON PlaneDeliveryOrganization.ID = PlaneDeliveryService.PlaneDeliveryOrganizationID " +
                               "LEFT OUTER JOIN [User] AS [PlaneDeliveryService.User] " +
                               "ON [PlaneDeliveryService.User].ID = PlaneDeliveryService.UserID " +
                               "LEFT OUTER JOIN SupplyPaymentTask " +
                               "ON SupplyPaymentTask.ID = PlaneDeliveryService.SupplyPaymentTaskID AND SupplyPaymentTask.Deleted = 0 " +
                               "LEFT OUTER JOIN [User] AS [SupplyPaymentTask.User] " +
                               "ON [SupplyPaymentTask.User].ID = SupplyPaymentTask.UserID " +
                               "LEFT OUTER JOIN InvoiceDocument " +
                               "ON InvoiceDocument.PlaneDeliveryServiceID = PlaneDeliveryService.ID AND InvoiceDocument.Deleted = 0 " +
                               "WHERE PlaneDeliveryService.NetUID = @NetId";

        Type[] types = {
            typeof(PlaneDeliveryService),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(InvoiceDocument)
        };

        Func<object[], PlaneDeliveryService> mapper = objects => {
            PlaneDeliveryService planeDeliveryService = (PlaneDeliveryService)objects[0];
            SupplyOrganization planeDeliveryOrganization = (SupplyOrganization)objects[1];
            User planeDeliveryServiceUser = (User)objects[2];
            SupplyPaymentTask paymentTask = (SupplyPaymentTask)objects[3];
            User paymentTaskUser = (User)objects[5];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];

            if (planeDeliveryServiceUser != null) planeDeliveryService.User = planeDeliveryServiceUser;

            if (planeDeliveryOrganization != null) planeDeliveryService.PlaneDeliveryOrganization = planeDeliveryOrganization;

            if (paymentTask != null) {
                paymentTask.User = paymentTaskUser;
                planeDeliveryService.SupplyPaymentTask = paymentTask;
            }

            if (invoiceDocument != null) planeDeliveryService.InvoiceDocuments.Add(invoiceDocument);

            if (planeDeliveryServiceToReturn != null) {
                if (invoiceDocument != null && !planeDeliveryServiceToReturn.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                    planeDeliveryServiceToReturn.InvoiceDocuments.Add(invoiceDocument);
            } else {
                planeDeliveryServiceToReturn = planeDeliveryService;
            }

            return planeDeliveryService;
        };

        var props = new { NetId = netId };

        _connection.Query(sqlExpression, types, mapper, props);

        return planeDeliveryServiceToReturn;
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE PlaneDeliveryService SET Deleted = 1 WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void Update(PlaneDeliveryService planeDeliveryService) {
        _connection.Execute(
            "UPDATE PlaneDeliveryService " +
            "SET PlaneDeliveryOrganizationID = @PlaneDeliveryOrganizationID, SupplyPaymentTaskID = @SupplyPaymentTaskID, Number = @Number, Name = @Name, VatPercent = @VatPercent, " +
            "UserID = @UserID, IsActive = @IsActive, FromDate = @FromDate, GrossPrice = @GrossPrice, NetPrice = @NetPrice, Vat = @Vat, Updated = getutcdate(), " +
            "AccountingGrossPrice = @AccountingGrossPrice, AccountingNetPrice = @AccountingNetPrice, AccountingVat = @AccountingVat, AccountingPaymentTaskId = @AccountingPaymentTaskId, " +
            "AccountingVatPercent = @AccountingVatPercent, SupplyOrganizationAgreementId = @SupplyOrganizationAgreementId, " +
            "[AccountingSupplyCostsWithinCountry] = @AccountingSupplyCostsWithinCountry, [SupplyInformationTaskID] = @SupplyInformationTaskId, " +
            "[ExchangeRate] = @ExchangeRate, [AccountingExchangeRate] = @AccountingExchangeRate, [IsIncludeAccountingValue] = @IsIncludeAccountingValue, " +
            "[ActProvidingServiceDocumentID] = @ActProvidingServiceDocumentId, [SupplyServiceAccountDocumentID] = @SupplyServiceAccountDocumentId " +
            "WHERE NetUID = @NetUID",
            planeDeliveryService
        );
    }

    public PlaneDeliveryService GetByIdWithoutIncludes(long id) {
        return _connection.Query<PlaneDeliveryService>(
            "SELECT * FROM [PlaneDeliveryService] " +
            "WHERE [PlaneDeliveryService].[ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }
}