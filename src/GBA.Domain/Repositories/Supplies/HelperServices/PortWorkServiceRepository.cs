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

public sealed class PortWorkServiceRepository : IPortWorkServiceRepository {
    private readonly IDbConnection _connection;

    public PortWorkServiceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(PortWorkService brokerService) {
        return _connection.Query<long>(
                "INSERT INTO PortWorkService (FromDate, GrossPrice, PortWorkOrganizationID, IsActive, UserId, SupplyPaymentTaskID, NetPrice, Vat, Number, Name, VatPercent, " +
                "ServiceNumber, SupplyOrganizationAgreementId, Updated, AccountingGrossPrice, AccountingNetPrice, AccountingVat, AccountingPaymentTaskId, " +
                "AccountingVatPercent, [AccountingSupplyCostsWithinCountry], [SupplyInformationTaskID], [ExchangeRate], [AccountingExchangeRate], [IsIncludeAccountingValue], " +
                "[ActProvidingServiceDocumentID], [SupplyServiceAccountDocumentID]) " +
                "VALUES(@FromDate, @GrossPrice, @PortWorkOrganizationID, @IsActive, @UserId, @SupplyPaymentTaskID, @NetPrice, @Vat, @Number, @Name, @VatPercent, " +
                "@ServiceNumber, @SupplyOrganizationAgreementId, getutcdate(), @AccountingGrossPrice, @AccountingNetPrice, @AccountingVat, " +
                "@AccountingPaymentTaskId, @AccountingVatPercent, @AccountingSupplyCostsWithinCountry, @SupplyInformationTaskId, " +
                "@ExchangeRate, @AccountingExchangeRate, @IsIncludeAccountingValue, @ActProvidingServiceDocumentId, @SupplyServiceAccountDocumentId); " +
                "SELECT SCOPE_IDENTITY()",
                brokerService
            )
            .Single();
    }

    public List<PortWorkService> GetAllRanged(DateTime from, DateTime to) {
        return _connection.Query<PortWorkService, User, SupplyPaymentTask, User, PortWorkService>(
                "SELECT * FROM PortWorkService " +
                "LEFT JOIN [User] AS [PortWorkServiceUser] " +
                "ON [PortWorkServiceUser].ID = PortWorkService.UserID " +
                "LEFT JOIN SupplyPaymentTask " +
                "ON SupplyPaymentTask.ID = PortWorkService.SupplyPaymentTaskID " +
                "LEFT JOIN [User] AS [PortWorkServiceSupplyPaymentTaskUser] " +
                "ON [PortWorkServiceSupplyPaymentTaskUser].ID = SupplyPaymentTask.UserID " +
                "WHERE PortWorkService.LoadDate >= @From " +
                "AND PortWorkService.LoadDate <= @To " +
                "AND PortWorkService.Deleted = 0",
                (brokerService, containerServiceUser, supplyPaymentTask, supplyPaymentTaskUser) => {
                    if (containerServiceUser != null) brokerService.User = containerServiceUser;

                    if (supplyPaymentTask != null) {
                        supplyPaymentTask.User = supplyPaymentTaskUser;
                        brokerService.SupplyPaymentTask = supplyPaymentTask;
                    }

                    return brokerService;
                },
                new { From = from, To = to }
            )
            .ToList();
    }

    public List<PortWorkService> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId) {
        List<PortWorkService> toReturn = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY ID) AS RowNumber " +
            ",ID " +
            "FROM " +
            "( " +
            "SELECT DISTINCT [PortWorkService].ID " +
            "FROM [PortWorkService] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].PortWorkServiceID = [PortWorkService].ID ";

        if (clientNetId.HasValue)
            sqlExpression += "LEFT JOIN [Client] " +
                             "ON [SupplyOrder].ClientID = [Client].ID ";

        sqlExpression += "WHERE [PortWorkService].Deleted = 0 " +
                         "AND [PortWorkService].Created >= @From " +
                         "AND [PortWorkService].Created <= @To " +
                         "AND [PortWorkService].Number like '%' + @Value + '%' ";

        if (clientNetId.HasValue) sqlExpression += "AND [Client].NetUID = @ClientNetId ";

        sqlExpression +=
            ") [Distincts] " +
            ") " +
            "SELECT * " +
            "FROM [PortWorkService] " +
            "LEFT JOIN [SupplyOrganization] AS [PortWorkOrganization] " +
            "ON [PortWorkOrganization].ID = [PortWorkService].PortWorkOrganizationID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [PortWorkService].SupplyPaymentTaskID " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].PortWorkServiceID = [PortWorkService].ID " +
            "AND [InvoiceDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].PortWorkServiceID = [PortWorkService].ID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "WHERE [PortWorkService].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ")";

        Type[] types = {
            typeof(PortWorkService),
            typeof(SupplyOrganization),
            typeof(SupplyPaymentTask),
            typeof(InvoiceDocument),
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(Client)
        };

        Func<object[], PortWorkService> mapper = objects => {
            PortWorkService portWorkService = (PortWorkService)objects[0];
            SupplyOrganization portWorkOrganization = (SupplyOrganization)objects[1];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[3];
            SupplyOrder supplyOrder = (SupplyOrder)objects[4];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[5];
            Client client = (Client)objects[6];

            if (!toReturn.Any(s => s.Id.Equals(portWorkService.Id))) {
                if (supplyOrder != null) {
                    supplyOrder.Client = client;
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                    portWorkService.SupplyOrders.Add(supplyOrder);
                }

                if (invoiceDocument != null) portWorkService.InvoiceDocuments.Add(invoiceDocument);

                portWorkService.PortWorkOrganization = portWorkOrganization;
                portWorkService.SupplyPaymentTask = supplyPaymentTask;

                toReturn.Add(portWorkService);
            } else {
                PortWorkService fromList = toReturn.First(s => s.Id.Equals(portWorkService.Id));

                if (invoiceDocument != null && !fromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id))) fromList.InvoiceDocuments.Add(invoiceDocument);
            }

            return portWorkService;
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

    public PortWorkService GetById(long id) {
        PortWorkService portWorkServiceToReturn = null;

        string sqlExpression = "SELECT * FROM PortWorkService " +
                               "LEFT JOIN [User] AS [PortWorkServiceUser] " +
                               "ON [PortWorkServiceUser].ID = PortWorkService.UserID " +
                               "LEFT JOIN SupplyPaymentTask AS [PortWorkServiceSupplyPaymentTask] " +
                               "ON [PortWorkServiceSupplyPaymentTask].ID = PortWorkService.SupplyPaymentTaskID " +
                               "LEFT JOIN [User] AS [PortWorkServiceSupplyPaymentTaskUser] " +
                               "ON [PortWorkServiceSupplyPaymentTaskUser].ID = [PortWorkServiceSupplyPaymentTask].UserID " +
                               "LEFT JOIN [SupplyOrganization] AS PortWorkOrganization " +
                               "ON PortWorkOrganization.ID = PortWorkService.PortWorkOrganizationID " +
                               "LEFT OUTER JOIN InvoiceDocument " +
                               "ON InvoiceDocument.PortWorkServiceID = PortWorkService.ID AND InvoiceDocument.Deleted = 0 " +
                               "WHERE PortWorkService.ID = @Id";

        Type[] types = {
            typeof(PortWorkService),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyOrganization),
            typeof(InvoiceDocument)
        };

        Func<object[], PortWorkService> mapper = objects => {
            PortWorkService portWorkService = (PortWorkService)objects[0];
            User portWorkServiceUser = (User)objects[1];
            SupplyPaymentTask portWorkServiceSupplyPaymentTask = (SupplyPaymentTask)objects[2];
            User portWorkServiceSupplyPaymentTaskUser = (User)objects[3];
            SupplyOrganization portWorkOrganization = (SupplyOrganization)objects[4];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[5];

            if (portWorkServiceUser != null) portWorkService.User = portWorkServiceUser;

            if (portWorkOrganization != null) portWorkService.PortWorkOrganization = portWorkOrganization;

            if (portWorkServiceSupplyPaymentTask != null) {
                portWorkServiceSupplyPaymentTask.User = portWorkServiceSupplyPaymentTaskUser;
                portWorkService.SupplyPaymentTask = portWorkServiceSupplyPaymentTask;
            }

            if (invoiceDocument != null) portWorkService.InvoiceDocuments.Add(invoiceDocument);

            if (portWorkServiceToReturn != null) {
                if (invoiceDocument != null && !portWorkServiceToReturn.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                    portWorkServiceToReturn.InvoiceDocuments.Add(invoiceDocument);
            } else {
                portWorkServiceToReturn = portWorkService;
            }

            return portWorkService;
        };

        var props = new { Id = id };

        _connection.Query(sqlExpression, types, mapper, props);

        return portWorkServiceToReturn;
    }

    public PortWorkService GetByIdWithoutIncludes(long id) {
        return _connection.Query<PortWorkService>(
            "SELECT * FROM [PortWorkService] " +
            "WHERE [PortWorkService].[ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }

    public PortWorkService GetByNetId(Guid netId) {
        PortWorkService portWorkServiceToReturn = null;

        string sqlExpression = "SELECT * FROM PortWorkService " +
                               "LEFT JOIN [User] AS [PortWorkServiceUser] " +
                               "ON [PortWorkServiceUser].ID = PortWorkService.UserID " +
                               "LEFT JOIN SupplyPaymentTask AS [PortWorkServiceSupplyPaymentTask] " +
                               "ON [PortWorkServiceSupplyPaymentTask].ID = PortWorkService.SupplyPaymentTaskID " +
                               "LEFT JOIN [User] AS [PortWorkServiceSupplyPaymentTaskUser] " +
                               "ON [PortWorkServiceSupplyPaymentTaskUser].ID = [PortWorkServiceSupplyPaymentTask].UserID " +
                               "LEFT JOIN [SupplyOrganization] AS PortWorkOrganization " +
                               "ON PortWorkOrganization.ID = PortWorkService.PortWorkOrganizationID " +
                               "LEFT OUTER JOIN InvoiceDocument " +
                               "ON InvoiceDocument.PortWorkServiceID = PortWorkService.ID AND InvoiceDocument.Deleted = 0 " +
                               "WHERE PortWorkService.NetUID = @NetId";

        Type[] types = {
            typeof(PortWorkService),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyOrganization),
            typeof(InvoiceDocument)
        };

        Func<object[], PortWorkService> mapper = objects => {
            PortWorkService portWorkService = (PortWorkService)objects[0];
            User portWorkServiceUser = (User)objects[1];
            SupplyPaymentTask portWorkServiceSupplyPaymentTask = (SupplyPaymentTask)objects[2];
            User portWorkServiceSupplyPaymentTaskUser = (User)objects[3];
            SupplyOrganization portWorkOrganization = (SupplyOrganization)objects[4];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[5];

            if (portWorkServiceUser != null) portWorkService.User = portWorkServiceUser;

            if (portWorkOrganization != null) portWorkService.PortWorkOrganization = portWorkOrganization;

            if (portWorkServiceSupplyPaymentTask != null) {
                portWorkServiceSupplyPaymentTask.User = portWorkServiceSupplyPaymentTaskUser;
                portWorkService.SupplyPaymentTask = portWorkServiceSupplyPaymentTask;
            }

            if (invoiceDocument != null) portWorkService.InvoiceDocuments.Add(invoiceDocument);

            if (portWorkServiceToReturn != null) {
                if (invoiceDocument != null && !portWorkServiceToReturn.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                    portWorkServiceToReturn.InvoiceDocuments.Add(invoiceDocument);
            } else {
                portWorkServiceToReturn = portWorkService;
            }

            return portWorkService;
        };

        var props = new { NetId = netId };

        _connection.Query(sqlExpression, types, mapper, props);

        return portWorkServiceToReturn;
    }

    public void Update(PortWorkService brokerService) {
        _connection.Execute(
            "UPDATE PortWorkService " +
            "SET FromDate = @FromDate" +
            ", GrossPrice = @GrossPrice" +
            ", PortWorkOrganizationID = @PortWorkOrganizationID" +
            ", IsActive = @IsActive" +
            ", UserID = @UserID" +
            ", SupplyPaymentTaskID = @SupplyPaymentTaskID" +
            ", NetPrice = @NetPrice" +
            ", Vat = @Vat" +
            ", Number = @Number" +
            ", Name = @Name" +
            ", VatPercent = @VatPercent" +
            ", Updated = getutcdate()" +
            ", AccountingGrossPrice = @AccountingGrossPrice" +
            ", AccountingNetPrice = @AccountingNetPrice" +
            ", AccountingVat = @AccountingVat" +
            ", AccountingPaymentTaskId = @AccountingPaymentTaskId" +
            ", AccountingVatPercent = @AccountingVatPercent" +
            ", SupplyOrganizationAgreementId = @SupplyOrganizationAgreementId " +
            ", [AccountingSupplyCostsWithinCountry] = @AccountingSupplyCostsWithinCountry " +
            ", [SupplyInformationTaskID] = @SupplyInformationTaskId " +
            ", [ExchangeRate] = @ExchangeRate " +
            ", [AccountingExchangeRate] = @AccountingExchangeRate " +
            ", [IsIncludeAccountingValue] = @IsIncludeAccountingValue " +
            ", [ActProvidingServiceDocumentID] = @ActProvidingServiceDocumentId " +
            ", [SupplyServiceAccountDocumentID] = @SupplyServiceAccountDocumentId " +
            "WHERE NetUID = @NetUID",
            brokerService
        );
    }

    public void UpdateSupplyPaymentTaskId(IEnumerable<PortWorkService> containerServices) {
        _connection.Execute(
            "UPDATE [PortWorkService] " +
            "SET SupplyPaymentTaskID = @SupplyPaymentTaskId " +
            "WHERE ID = @Id",
            containerServices
        );
    }
}