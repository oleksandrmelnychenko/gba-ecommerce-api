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

public sealed class PortCustomAgencyServiceRepository : IPortCustomAgencyServiceRepository {
    private readonly IDbConnection _connection;

    public PortCustomAgencyServiceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(PortCustomAgencyService portCustomAgencyService) {
        return _connection.Query<long>(
                "INSERT INTO [PortCustomAgencyService] (PortCustomAgencyOrganizationId, IsActive, UserId, SupplyPaymentTaskId, FromDate, GrossPrice, NetPrice, Vat, Number, Name, " +
                "VatPercent, ServiceNumber, SupplyOrganizationAgreementId, Updated, AccountingGrossPrice, AccountingNetPrice, AccountingVat, " +
                "AccountingPaymentTaskId, AccountingVatPercent, [AccountingSupplyCostsWithinCountry], [SupplyInformationTaskID], " +
                "[ExchangeRate], [AccountingExchangeRate], [IsIncludeAccountingValue], [ActProvidingServiceDocumentID], [SupplyServiceAccountDocumentID]) " +
                "VALUES (@PortCustomAgencyOrganizationId, @IsActive, @UserId, @SupplyPaymentTaskId, @FromDate, @GrossPrice, @NetPrice, @Vat, @Number, @Name, @VatPercent, " +
                "@ServiceNumber, @SupplyOrganizationAgreementId, getutcdate(), @AccountingGrossPrice, @AccountingNetPrice, @AccountingVat, @AccountingPaymentTaskId, " +
                "@AccountingVatPercent, @AccountingSupplyCostsWithinCountry, @SupplyInformationTaskId, @ExchangeRate, @AccountingExchangeRate, @IsIncludeAccountingValue, " +
                "@ActProvidingServiceDocumentId, @SupplyServiceAccountDocumentId); " +
                "SELECT SCOPE_IDENTITY()",
                portCustomAgencyService
            )
            .Single();
    }

    public void Update(PortCustomAgencyService portCustomAgencyService) {
        _connection.Query<long>(
            "UPDATE [PortCustomAgencyService] " +
            "SET PortCustomAgencyOrganizationId = @PortCustomAgencyOrganizationId" +
            ", IsActive = @IsActive" +
            ", UserId = @UserId" +
            ", SupplyPaymentTaskId = @SupplyPaymentTaskId" +
            ", FromDate = @FromDate" +
            ", GrossPrice = @GrossPrice" +
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
            "WHERE NetUID = @NetUid",
            portCustomAgencyService
        );
    }

    public PortCustomAgencyService GetById(long id) {
        PortCustomAgencyService portCustomAgencyServiceToReturn = null;

        _connection.Query<PortCustomAgencyService, SupplyOrganization, User, SupplyPaymentTask, User, InvoiceDocument, PortCustomAgencyService>(
            "SELECT * FROM [PortCustomAgencyService] " +
            "LEFT JOIN [SupplyOrganization] AS [PortCustomAgencyOrganization] " +
            "ON [PortCustomAgencyOrganization].ID = [PortCustomAgencyService].PortCustomAgencyOrganizationID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [PortCustomAgencyService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [PortCustomAgencyService].SupplyPaymentTaskID " +
            "LEFT JOIN [User] AS [SupplyPaymentTaskUser] " +
            "ON [SupplyPaymentTaskUser].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
            "WHERE [PortCustomAgencyService].ID = @Id ",
            (agencyService, agencyOrganization, serviceUser, paymentTask, paymentTaskUser, document) => {
                if (portCustomAgencyServiceToReturn != null) {
                    if (document != null && !portCustomAgencyServiceToReturn.InvoiceDocuments.Any(d => d.Id.Equals(document.Id)))
                        portCustomAgencyServiceToReturn.InvoiceDocuments.Add(document);
                } else {
                    if (paymentTask != null) {
                        paymentTask.User = paymentTaskUser;

                        agencyService.SupplyPaymentTask = paymentTask;
                    }

                    if (document != null) agencyService.InvoiceDocuments.Add(document);

                    agencyService.User = serviceUser;
                    agencyService.PortCustomAgencyOrganization = agencyOrganization;

                    portCustomAgencyServiceToReturn = agencyService;
                }

                return agencyService;
            },
            new { Id = id }
        );

        return portCustomAgencyServiceToReturn;
    }

    public List<PortCustomAgencyService> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId) {
        List<PortCustomAgencyService> toReturn = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY ID) AS RowNumber " +
            ",ID " +
            "FROM " +
            "( " +
            "SELECT DISTINCT [PortCustomAgencyService].ID " +
            "FROM [PortCustomAgencyService] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].PortCustomAgencyServiceID = [PortCustomAgencyService].ID ";

        if (clientNetId.HasValue)
            sqlExpression += "LEFT JOIN [Client] " +
                             "ON [SupplyOrder].ClientID = [Client].ID ";

        sqlExpression += "WHERE [PortCustomAgencyService].Deleted = 0 " +
                         "AND [PortCustomAgencyService].Created >= @From " +
                         "AND [PortCustomAgencyService].Created <= @To " +
                         "AND [PortCustomAgencyService].Number like '%' + @Value + '%' ";

        if (clientNetId.HasValue) sqlExpression += "AND [Client].NetUID = @ClientNetId ";

        sqlExpression +=
            ") [Distincts] " +
            ") " +
            "SELECT * " +
            "FROM [PortCustomAgencyService] " +
            "LEFT JOIN [SupplyOrganization] AS [PortCustomAgencyOrganization] " +
            "ON [PortCustomAgencyOrganization].ID = [PortCustomAgencyService].PortCustomAgencyOrganizationID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [PortCustomAgencyService].SupplyPaymentTaskID " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
            "AND [InvoiceDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "WHERE [PortCustomAgencyService].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ")";

        Type[] types = {
            typeof(PortCustomAgencyService),
            typeof(SupplyOrganization),
            typeof(SupplyPaymentTask),
            typeof(InvoiceDocument),
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(Client)
        };

        Func<object[], PortCustomAgencyService> mapper = objects => {
            PortCustomAgencyService portCustomAgencyService = (PortCustomAgencyService)objects[0];
            SupplyOrganization portCustomAgencyOrganization = (SupplyOrganization)objects[1];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[3];
            SupplyOrder supplyOrder = (SupplyOrder)objects[4];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[5];
            Client client = (Client)objects[6];

            if (!toReturn.Any(s => s.Id.Equals(portCustomAgencyService.Id))) {
                if (supplyOrder != null) {
                    supplyOrder.Client = client;
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                    portCustomAgencyService.SupplyOrders.Add(supplyOrder);
                }

                if (invoiceDocument != null) portCustomAgencyService.InvoiceDocuments.Add(invoiceDocument);

                portCustomAgencyService.PortCustomAgencyOrganization = portCustomAgencyOrganization;
                portCustomAgencyService.SupplyPaymentTask = supplyPaymentTask;

                toReturn.Add(portCustomAgencyService);
            } else {
                PortCustomAgencyService fromList = toReturn.First(s => s.Id.Equals(portCustomAgencyService.Id));

                if (invoiceDocument != null && !fromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id))) fromList.InvoiceDocuments.Add(invoiceDocument);
            }

            return portCustomAgencyService;
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

    public PortCustomAgencyService GetByIdWithoutIncludes(long id) {
        return _connection.Query<PortCustomAgencyService>(
            "SELECT * FROM [PortCustomAgencyService] " +
            "WHERE [PortCustomAgencyService].[ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }
}