using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.HelperServices;

public sealed class CustomServiceRepository : ICustomServiceRepository {
    private readonly IDbConnection _connection;

    public CustomServiceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(CustomService customService) {
        return _connection.Query<long>(
                "INSERT INTO CustomService (ExciseDutyOrganizationID, SupplyCustomType, FromDate, Number, GrossPrice, CustomOrganizationID, SupplyOrderID, IsActive, UserId, " +
                "SupplyPaymentTaskID, NetPrice, Vat, Name, VatPercent, ServiceNumber, SupplyOrganizationAgreementId, Updated, " +
                "AccountingGrossPrice, AccountingNetPrice, AccountingVat, AccountingPaymentTaskId, AccountingVatPercent, " +
                "[AccountingSupplyCostsWithinCountry], [SupplyInformationTaskID], [ExchangeRate], [AccountingExchangeRate], [IsIncludeAccountingValue], " +
                "[ActProvidingServiceDocumentID], [SupplyServiceAccountDocumentID]) " +
                "VALUES(@ExciseDutyOrganizationID, @SupplyCustomType, @FromDate, @Number, @GrossPrice, @CustomOrganizationID, @SupplyOrderID, @IsActive, @UserId, " +
                "@SupplyPaymentTaskID, @NetPrice, @Vat, @Name, @VatPercent, @ServiceNumber, @SupplyOrganizationAgreementId, getutcdate(), " +
                "@AccountingGrossPrice, @AccountingNetPrice, @AccountingVat, @AccountingPaymentTaskId, @AccountingVatPercent, " +
                "@AccountingSupplyCostsWithinCountry, @SupplyInformationTaskId, @ExchangeRate, @AccountingExchangeRate, @IsIncludeAccountingValue, " +
                "@ActProvidingServiceDocumentId, @SupplyServiceAccountDocumentId); " +
                "SELECT SCOPE_IDENTITY()",
                customService
            )
            .Single();
    }

    public void Update(CustomService customService) {
        _connection.Execute(
            "UPDATE CustomService " +
            "SET ExciseDutyOrganizationID = @ExciseDutyOrganizationID" +
            ", SupplyCustomType = @SupplyCustomType" +
            ", FromDate = @FromDate" +
            ", Number = @Number" +
            ", GrossPrice = @GrossPrice" +
            ", CustomOrganizationID = @CustomOrganizationID" +
            ", SupplyOrderID = @SupplyOrderID" +
            ", IsActive = @IsActive" +
            ", UserID = @UserID" +
            ", SupplyPaymentTaskID = @SupplyPaymentTaskID" +
            ", NetPrice = @NetPrice" +
            ", Vat = @Vat" +
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
            customService
        );
    }

    public void Add(IEnumerable<CustomService> customServices) {
        _connection.Execute(
            "INSERT INTO CustomService (ExciseDutyOrganizationID, SupplyCustomType, FromDate, Number, GrossPrice, CustomOrganizationID, SupplyOrderID, IsActive, UserId, " +
            "SupplyPaymentTaskID, NetPrice, Vat, Name, VatPercent, ServiceNumber, Updated, AccountingGrossPrice, AccountingNetPrice, AccountingVat, " +
            "AccountingPaymentTaskId, AccountingVatPercent, [AccountingSupplyCostsWithinCountry], [SupplyInformationTaskID], " +
            "[ExchangeRate], [AccountingExchangeRate], [IsIncludeAccountingValue], [ActProvidingServiceDocumentID], [SupplyServiceAccountDocumentID]) " +
            "VALUES(@ExciseDutyOrganizationID, @SupplyCustomType, @FromDate, @Number, @GrossPrice, @CustomOrganizationID, @SupplyOrderID, @IsActive, @UserId, " +
            "@SupplyPaymentTaskID, @NetPrice, @Vat, @Name, @VatPercent, @ServiceNumber, getutcdate(), @AccountingGrossPrice, @AccountingNetPrice, " +
            "@AccountingVat, @AccountingPaymentTaskId, @AccountingVatPercent, @AccountingSupplyCostsWithinCountry, @SupplyInformationTaskId, " +
            "@ExchangeRate, @AccountingExchangeRate, @IsIncludeAccountingValue, @ActProvidingServiceDocumentId, @SupplyServiceAccountDocumentId)",
            customServices
        );
    }

    public List<CustomService> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId) {
        List<CustomService> toReturn = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY ID) AS RowNumber " +
            ",ID " +
            "FROM " +
            "( " +
            "SELECT DISTINCT [CustomService].ID " +
            "FROM [CustomService] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [CustomService].SupplyOrderID ";

        if (clientNetId.HasValue)
            sqlExpression += "LEFT JOIN [Client] " +
                             "ON [SupplyOrder].ClientID = [Client].ID ";

        sqlExpression += "WHERE [CustomService].Deleted = 0 " +
                         "AND [CustomService].Created >= @From " +
                         "AND [CustomService].Created <= @To " +
                         "AND [CustomService].Number like '%' + @Value + '%' ";

        if (clientNetId.HasValue) sqlExpression += "AND [Client].NetUID = @ClientNetId ";

        sqlExpression +=
            ") [Distincts] " +
            ") " +
            "SELECT * " +
            "FROM [CustomService] " +
            "LEFT JOIN [SupplyOrganization] AS [ExciseDutyOrganization] " +
            "ON [ExciseDutyOrganization].ID = [CustomService].ExciseDutyOrganizationID " +
            "LEFT JOIN [SupplyOrganization] AS [CustomOrganization] " +
            "ON [CustomOrganization].ID = [CustomService].CustomOrganizationID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [CustomService].SupplyPaymentTaskID " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].CustomServiceID = [CustomService].ID " +
            "AND [InvoiceDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [CustomService].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "WHERE [CustomService].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ")";

        Type[] types = {
            typeof(CustomService),
            typeof(SupplyOrganization),
            typeof(SupplyOrganization),
            typeof(SupplyPaymentTask),
            typeof(InvoiceDocument),
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(Client)
        };

        Func<object[], CustomService> mapper = objects => {
            CustomService customService = (CustomService)objects[0];
            SupplyOrganization exciseDutyOrganization = (SupplyOrganization)objects[1];
            SupplyOrganization customOrganization = (SupplyOrganization)objects[2];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[3];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[4];
            SupplyOrder supplyOrder = (SupplyOrder)objects[5];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[6];
            Client client = (Client)objects[7];

            if (!toReturn.Any(s => s.Id.Equals(customService.Id))) {
                if (supplyOrder != null) {
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                    supplyOrder.Client = client;

                    customService.SupplyOrder = supplyOrder;
                }

                if (invoiceDocument != null) customService.InvoiceDocuments.Add(invoiceDocument);

                customService.ExciseDutyOrganization = exciseDutyOrganization;
                customService.CustomOrganization = customOrganization;
                customService.SupplyPaymentTask = supplyPaymentTask;

                toReturn.Add(customService);
            } else {
                CustomService fromList = toReturn.First(s => s.Id.Equals(customService.Id));

                if (invoiceDocument != null && !fromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id))) fromList.InvoiceDocuments.Add(invoiceDocument);
            }

            return customService;
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

    public CustomService GetByIdWithoutIncludes(long id) {
        return _connection.Query<CustomService>(
            "SELECT * FROM [CustomService] " +
            "WHERE [CustomService].[ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }
}