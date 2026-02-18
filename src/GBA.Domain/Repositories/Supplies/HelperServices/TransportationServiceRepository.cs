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

public sealed class TransportationServiceRepository : ITransportationServiceRepository {
    private readonly IDbConnection _connection;

    public TransportationServiceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(TransportationService transportationService) {
        return _connection.Query<long>(
                "INSERT INTO TransportationService (FromDate, GrossPrice, TransportationOrganizationID, IsActive, UserId, SupplyPaymentTaskID, NetPrice, Vat, Number, " +
                "IsSealAndSignatureVerified, Name, VatPercent, ServiceNumber, SupplyOrganizationAgreementId, Updated, AccountingGrossPrice, AccountingNetPrice, " +
                "AccountingVat, AccountingPaymentTaskId, AccountingVatPercent, [AccountingSupplyCostsWithinCountry], [SupplyInformationTaskID], " +
                "[ExchangeRate], [AccountingExchangeRate], [IsIncludeAccountingValue], [ActProvidingServiceDocumentID], [SupplyServiceAccountDocumentID]) " +
                "VALUES(@FromDate, @GrossPrice, @TransportationOrganizationID, @IsActive, @UserId, @SupplyPaymentTaskID, @NetPrice, @Vat, @Number, " +
                "@IsSealAndSignatureVerified, @Name, @VatPercent, @ServiceNumber, @SupplyOrganizationAgreementId, getutcdate(), @AccountingGrossPrice, " +
                "@AccountingNetPrice, @AccountingVat, @AccountingPaymentTaskId, @AccountingVatPercent, @AccountingSupplyCostsWithinCountry, @SupplyInformationTaskId, " +
                "@ExchangeRate, @AccountingExchangeRate, @IsIncludeAccountingValue, @ActProvidingServiceDocumentId, @SupplyServiceAccountDocumentId); " +
                "SELECT SCOPE_IDENTITY()",
                transportationService
            )
            .Single();
    }

    public List<TransportationService> GetAllRanged(DateTime from, DateTime to) {
        return _connection.Query<TransportationService, User, SupplyPaymentTask, User, TransportationService>(
                "SELECT * FROM TransportationService " +
                "LEFT JOIN [User] AS [TransportationServiceUser] " +
                "ON [TransportationServiceUser].ID = TransportationService.UserID " +
                "LEFT JOIN SupplyPaymentTask " +
                "ON SupplyPaymentTask.ID = TransportationService.SupplyPaymentTaskID " +
                "LEFT JOIN [User] AS [TransportationServiceSupplyPaymentTaskUser] " +
                "ON [TransportationServiceSupplyPaymentTaskUser].ID = SupplyPaymentTask.UserID " +
                "WHERE TransportationService.LoadDate >= @From " +
                "AND TransportationService.LoadDate <= @To " +
                "AND TransportationService.Deleted = 0",
                (transportationService, containerServiceUser, supplyPaymentTask, supplyPaymentTaskUser) => {
                    if (containerServiceUser != null) transportationService.User = containerServiceUser;

                    if (supplyPaymentTask != null) {
                        supplyPaymentTask.User = supplyPaymentTaskUser;
                        transportationService.SupplyPaymentTask = supplyPaymentTask;
                    }

                    return transportationService;
                },
                new { From = from, To = to }
            )
            .ToList();
    }

    public List<TransportationService> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId) {
        List<TransportationService> toReturn = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY ID) AS RowNumber " +
            ",ID " +
            "FROM " +
            "( " +
            "SELECT DISTINCT [TransportationService].ID " +
            "FROM [TransportationService] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].TransportationServiceID = [TransportationService].ID ";

        if (clientNetId.HasValue)
            sqlExpression += "LEFT JOIN [Client] " +
                             "ON [SupplyOrder].ClientID = [Client].ID ";

        sqlExpression += "WHERE [TransportationService].Deleted = 0 " +
                         "AND [TransportationService].Created >= @From " +
                         "AND [TransportationService].Created <= @To " +
                         "AND [TransportationService].Number like '%' + @Value + '%' ";

        if (clientNetId.HasValue) sqlExpression += "AND [Client].NetUID = @ClientNetId ";

        sqlExpression +=
            ") [Distincts] " +
            ") " +
            "SELECT * " +
            "FROM [TransportationService] " +
            "LEFT JOIN [SupplyOrganization] AS [TransportationOrganization] " +
            "ON [TransportationOrganization].ID = [TransportationService].TransportationOrganizationID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [TransportationService].SupplyPaymentTaskID " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].TransportationServiceID = [TransportationService].ID " +
            "AND [InvoiceDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].TransportationServiceID = [TransportationService].ID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "WHERE [TransportationService].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ")";

        Type[] types = {
            typeof(TransportationService),
            typeof(SupplyOrganization),
            typeof(SupplyPaymentTask),
            typeof(InvoiceDocument),
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(Client)
        };

        Func<object[], TransportationService> mapper = objects => {
            TransportationService transportationService = (TransportationService)objects[0];
            SupplyOrganization transportationOrganization = (SupplyOrganization)objects[1];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[3];
            SupplyOrder supplyOrder = (SupplyOrder)objects[4];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[5];
            Client client = (Client)objects[6];

            if (!toReturn.Any(s => s.Id.Equals(transportationService.Id))) {
                if (supplyOrder != null) {
                    supplyOrder.Client = client;
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                    transportationService.SupplyOrders.Add(supplyOrder);
                }

                if (invoiceDocument != null) transportationService.InvoiceDocuments.Add(invoiceDocument);

                transportationService.TransportationOrganization = transportationOrganization;
                transportationService.SupplyPaymentTask = supplyPaymentTask;

                toReturn.Add(transportationService);
            } else {
                TransportationService fromList = toReturn.First(s => s.Id.Equals(transportationService.Id));

                if (invoiceDocument != null && !fromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id))) fromList.InvoiceDocuments.Add(invoiceDocument);
            }

            return transportationService;
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

    public TransportationService GetById(long id) {
        TransportationService transportationServiceToReturn = null;

        string sqlExpression = "SELECT * FROM TransportationService " +
                               "LEFT JOIN [User] AS [TransportationServiceUser] " +
                               "ON [TransportationServiceUser].ID = TransportationService.UserID " +
                               "LEFT JOIN SupplyPaymentTask AS [TransportationServiceSupplyPaymentTask] " +
                               "ON [TransportationServiceSupplyPaymentTask].ID = TransportationService.SupplyPaymentTaskID " +
                               "LEFT JOIN [User] AS [TransportationServiceSupplyPaymentTaskUser] " +
                               "ON [TransportationServiceSupplyPaymentTaskUser].ID = [TransportationServiceSupplyPaymentTask].UserID " +
                               "LEFT JOIN [SupplyOrganization] AS TransportationOrganization " +
                               "ON TransportationOrganization.ID = TransportationService.TransportationOrganizationID " +
                               "LEFT OUTER JOIN InvoiceDocument " +
                               "ON InvoiceDocument.TransportationServiceID = TransportationService.ID AND InvoiceDocument.Deleted = 0 " +
                               "WHERE TransportationService.ID = @Id";

        Type[] types = {
            typeof(TransportationService),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyOrganization),
            typeof(InvoiceDocument)
        };

        Func<object[], TransportationService> mapper = objects => {
            TransportationService transportationService = (TransportationService)objects[0];
            User transportationServiceUser = (User)objects[1];
            SupplyPaymentTask transportationServiceSupplyPaymentTask = (SupplyPaymentTask)objects[2];
            User transportationServiceSupplyPaymentTaskUser = (User)objects[3];
            SupplyOrganization transportationOrganization = (SupplyOrganization)objects[5];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];

            if (transportationServiceUser != null) transportationService.User = transportationServiceUser;

            if (transportationOrganization != null) transportationService.TransportationOrganization = transportationOrganization;

            if (transportationServiceSupplyPaymentTask != null) {
                transportationServiceSupplyPaymentTask.User = transportationServiceSupplyPaymentTaskUser;
                transportationService.SupplyPaymentTask = transportationServiceSupplyPaymentTask;
            }

            if (invoiceDocument != null) transportationService.InvoiceDocuments.Add(invoiceDocument);

            if (transportationServiceToReturn != null) {
                if (invoiceDocument != null && !transportationServiceToReturn.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                    transportationServiceToReturn.InvoiceDocuments.Add(invoiceDocument);
            } else {
                transportationServiceToReturn = transportationService;
            }

            return transportationService;
        };

        var props = new { Id = id };

        _connection.Query(sqlExpression, types, mapper, props);

        return transportationServiceToReturn;
    }

    public TransportationService GetByNetId(Guid netId) {
        TransportationService transportationServiceToReturn = null;

        string sqlExpression = "SELECT * FROM TransportationService " +
                               "LEFT JOIN [User] AS [TransportationServiceUser] " +
                               "ON [TransportationServiceUser].ID = TransportationService.UserID " +
                               "LEFT JOIN SupplyPaymentTask AS [TransportationServiceSupplyPaymentTask] " +
                               "ON [TransportationServiceSupplyPaymentTask].ID = TransportationService.SupplyPaymentTaskID " +
                               "LEFT JOIN [User] AS [TransportationServiceSupplyPaymentTaskUser] " +
                               "ON [TransportationServiceSupplyPaymentTaskUser].ID = [TransportationServiceSupplyPaymentTask].UserID " +
                               "LEFT JOIN [SupplyOrganization] AS TransportationOrganization " +
                               "ON TransportationOrganization.ID = TransportationService.TransportationOrganizationID " +
                               "LEFT OUTER JOIN InvoiceDocument " +
                               "ON InvoiceDocument.TransportationServiceID = TransportationService.ID AND InvoiceDocument.Deleted = 0 " +
                               "WHERE TransportationService.NetUID = @NetId";

        Type[] types = {
            typeof(TransportationService),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyOrganization),
            typeof(InvoiceDocument)
        };

        Func<object[], TransportationService> mapper = objects => {
            TransportationService transportationService = (TransportationService)objects[0];
            User transportationServiceUser = (User)objects[1];
            SupplyPaymentTask transportationServiceSupplyPaymentTask = (SupplyPaymentTask)objects[2];
            User transportationServiceSupplyPaymentTaskUser = (User)objects[3];
            SupplyOrganization transportationOrganization = (SupplyOrganization)objects[5];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];

            if (transportationServiceUser != null) transportationService.User = transportationServiceUser;

            if (transportationOrganization != null) transportationService.TransportationOrganization = transportationOrganization;

            if (transportationServiceSupplyPaymentTask != null) {
                transportationServiceSupplyPaymentTask.User = transportationServiceSupplyPaymentTaskUser;
                transportationService.SupplyPaymentTask = transportationServiceSupplyPaymentTask;
            }

            if (invoiceDocument != null) transportationService.InvoiceDocuments.Add(invoiceDocument);

            if (transportationServiceToReturn != null) {
                if (invoiceDocument != null && !transportationServiceToReturn.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                    transportationServiceToReturn.InvoiceDocuments.Add(invoiceDocument);
            } else {
                transportationServiceToReturn = transportationService;
            }

            return transportationService;
        };

        var props = new { NetId = netId };

        _connection.Query(sqlExpression, types, mapper, props);

        return transportationServiceToReturn;
    }

    public void Update(TransportationService transportationService) {
        _connection.Execute(
            "UPDATE TransportationService " +
            "SET FromDate = @FromDate, GrossPrice = @GrossPrice, TransportationOrganizationID = @TransportationOrganizationID, IsActive = @IsActive, " +
            "UserID = @UserID, SupplyPaymentTaskID = @SupplyPaymentTaskID, NetPrice = @NetPrice, Vat = @Vat, Number = @Number, IsSealAndSignatureVerified = @IsSealAndSignatureVerified, " +
            "Name = @Name, VatPercent = @VatPercent, Updated = getutcdate(), AccountingGrossPrice = @AccountingGrossPrice, AccountingNetPrice = @AccountingNetPrice, AccountingVat = @AccountingVat, " +
            "AccountingPaymentTaskId = @AccountingPaymentTaskId, AccountingVatPercent = @AccountingVatPercent, SupplyOrganizationAgreementId = @SupplyOrganizationAgreementId, " +
            "[AccountingSupplyCostsWithinCountry] = @AccountingSupplyCostsWithinCountry, [SupplyInformationTaskID] = @SupplyInformationTaskId, " +
            "[ExchangeRate] = @ExchangeRate, [AccountingExchangeRate] = @AccountingExchangeRate, [IsIncludeAccountingValue] = @IsIncludeAccountingValue, " +
            "[ActProvidingServiceDocumentID] = @ActProvidingServiceDocumentId, [SupplyServiceAccountDocumentID] = @SupplyServiceAccountDocumentId " +
            "WHERE NetUID = @NetUID",
            transportationService
        );
    }

    public TransportationService GetByIdWithoutIncludes(long id) {
        return _connection.Query<TransportationService>(
            "SELECT * FROM [TransportationService] " +
            "WHERE [TransportationService].[ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }
}