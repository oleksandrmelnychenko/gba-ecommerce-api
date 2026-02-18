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

public sealed class VehicleDeliveryServiceRepository : IVehicleDeliveryServiceRepository {
    private readonly IDbConnection _connection;

    public VehicleDeliveryServiceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(VehicleDeliveryService vehicleDeliveryService) {
        return _connection.Query<long>(
                "INSERT INTO [VehicleDeliveryService] (VehicleDeliveryOrganizationId, IsActive, UserId, SupplyPaymentTaskId, FromDate, GrossPrice, NetPrice, Vat, " +
                "Number, IsSealAndSignatureVerified, Name, VatPercent, ServiceNumber, SupplyOrganizationAgreementId, Updated, AccountingGrossPrice, AccountingNetPrice, AccountingVat, " +
                "AccountingPaymentTaskId, AccountingVatPercent, [AccountingSupplyCostsWithinCountry], [SupplyInformationTaskID], " +
                "[ExchangeRate], [AccountingExchangeRate], [IsIncludeAccountingValue], [ActProvidingServiceDocumentID], [SupplyServiceAccountDocumentID]) " +
                "VALUES (@VehicleDeliveryOrganizationId, @IsActive, @UserId, @SupplyPaymentTaskId, @FromDate, @GrossPrice, @NetPrice, @Vat, @Number, @IsSealAndSignatureVerified, " +
                "@Name, @VatPercent, @ServiceNumber, @SupplyOrganizationAgreementId, getutcdate(), @AccountingGrossPrice, @AccountingNetPrice, @AccountingVat, @AccountingPaymentTaskId, " +
                "@AccountingVatPercent, @AccountingSupplyCostsWithinCountry, @SupplyInformationTaskId, @ExchangeRate, @AccountingExchangeRate, @IsIncludeAccountingValue, " +
                "@ActProvidingServiceDocumentId, @SupplyServiceAccountDocumentId); " +
                "SELECT SCOPE_IDENTITY()",
                vehicleDeliveryService
            )
            .Single();
    }

    public void Update(VehicleDeliveryService vehicleDeliveryService) {
        _connection.Query<long>(
            "UPDATE [VehicleDeliveryService] " +
            "SET VehicleDeliveryOrganizationId = @VehicleDeliveryOrganizationId, IsActive = @IsActive, UserId = @UserId, SupplyPaymentTaskId = @SupplyPaymentTaskId, " +
            "FromDate = @FromDate, GrossPrice = @GrossPrice, NetPrice = @NetPrice, Vat = @Vat, Number = @Number, IsSealAndSignatureVerified = @IsSealAndSignatureVerified, " +
            "Name = @Name, VatPercent = @VatPercent, Updated = getutcdate(), " +
            "AccountingGrossPrice = @AccountingGrossPrice, AccountingNetPrice = @AccountingNetPrice, AccountingVat = @AccountingVat, AccountingPaymentTaskId = @AccountingPaymentTaskId, " +
            "AccountingVatPercent = @AccountingVatPercent, SupplyOrganizationAgreementId = @SupplyOrganizationAgreementId, " +
            "[AccountingSupplyCostsWithinCountry] = @AccountingSupplyCostsWithinCountry, [SupplyInformationTaskID] = @SupplyInformationTaskId, " +
            "[ExchangeRate] = @ExchangeRate, [AccountingExchangeRate] = @AccountingExchangeRate, [IsIncludeAccountingValue] = @IsIncludeAccountingValue, " +
            "[ActProvidingServiceDocumentID] = @ActProvidingServiceDocumentId, [SupplyServiceAccountDocumentID] = @SupplyServiceAccountDocumentId " +
            "WHERE NetUID = @NetUid",
            vehicleDeliveryService
        );
    }

    public VehicleDeliveryService GetById(long id) {
        VehicleDeliveryService vehicleDeliveryServiceToReturn = null;

        _connection.Query<VehicleDeliveryService, SupplyOrganization, User, SupplyPaymentTask, User, InvoiceDocument, VehicleDeliveryService>(
            "SELECT * FROM [VehicleDeliveryService] " +
            "LEFT JOIN [SupplyOrganization] AS [VehicleDeliveryOrganization] " +
            "ON [VehicleDeliveryOrganization].ID = [VehicleDeliveryService].VehicleDeliveryOrganizationID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [VehicleDeliveryService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [VehicleDeliveryService].SupplyPaymentTaskID " +
            "LEFT JOIN [User] AS [SupplyPaymentTaskUser] " +
            "ON [SupplyPaymentTaskUser].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
            "WHERE [VehicleDeliveryService].ID = @Id ",
            (vehicleService, vehicleOrganization, serviceUser, paymentTask, paymentTaskUser, document) => {
                if (vehicleDeliveryServiceToReturn != null) {
                    if (document != null && !vehicleDeliveryServiceToReturn.InvoiceDocuments.Any(d => d.Id.Equals(document.Id)))
                        vehicleDeliveryServiceToReturn.InvoiceDocuments.Add(document);
                } else {
                    if (paymentTask != null) {
                        paymentTask.User = paymentTaskUser;

                        vehicleService.SupplyPaymentTask = paymentTask;
                    }

                    if (document != null) vehicleService.InvoiceDocuments.Add(document);

                    vehicleService.User = serviceUser;
                    vehicleService.VehicleDeliveryOrganization = vehicleOrganization;

                    vehicleDeliveryServiceToReturn = vehicleService;
                }

                return vehicleService;
            },
            new { Id = id }
        );

        return vehicleDeliveryServiceToReturn;
    }

    public List<VehicleDeliveryService> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId) {
        List<VehicleDeliveryService> toReturn = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY ID) AS RowNumber " +
            ",ID " +
            "FROM " +
            "( " +
            "SELECT DISTINCT [VehicleDeliveryService].ID " +
            "FROM [VehicleDeliveryService] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].VehicleDeliveryServiceID = [VehicleDeliveryService].ID ";

        if (clientNetId.HasValue)
            sqlExpression += "LEFT JOIN [Client] " +
                             "ON [SupplyOrder].ClientID = [Client].ID ";

        sqlExpression += "WHERE [VehicleDeliveryService].Deleted = 0 " +
                         "AND [VehicleDeliveryService].Created >= @From " +
                         "AND [VehicleDeliveryService].Created <= @To " +
                         "AND [VehicleDeliveryService].Number like '%' + @Value + '%' ";

        if (clientNetId.HasValue) sqlExpression += "AND [Client].NetUID = @ClientNetId ";

        sqlExpression +=
            ") [Distincts] " +
            ") " +
            "SELECT * " +
            "FROM [VehicleDeliveryService] " +
            "LEFT JOIN [SupplyOrganization] AS [VehicleDeliveryOrganization] " +
            "ON [VehicleDeliveryOrganization].ID = [VehicleDeliveryService].VehicleDeliveryOrganizationID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [VehicleDeliveryService].SupplyPaymentTaskID " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
            "AND [InvoiceDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "WHERE [VehicleDeliveryService].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ")";

        Type[] types = {
            typeof(VehicleDeliveryService),
            typeof(SupplyOrganization),
            typeof(SupplyPaymentTask),
            typeof(InvoiceDocument),
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(Client)
        };

        Func<object[], VehicleDeliveryService> mapper = objects => {
            VehicleDeliveryService vehicleDeliveryService = (VehicleDeliveryService)objects[0];
            SupplyOrganization vehicleDeliveryOrganization = (SupplyOrganization)objects[1];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[2];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[3];
            SupplyOrder supplyOrder = (SupplyOrder)objects[4];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[5];
            Client client = (Client)objects[6];

            if (!toReturn.Any(s => s.Id.Equals(vehicleDeliveryService.Id))) {
                if (supplyOrder != null) {
                    supplyOrder.Client = client;
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                    vehicleDeliveryService.SupplyOrders.Add(supplyOrder);
                }

                if (invoiceDocument != null) vehicleDeliveryService.InvoiceDocuments.Add(invoiceDocument);

                vehicleDeliveryService.VehicleDeliveryOrganization = vehicleDeliveryOrganization;
                vehicleDeliveryService.SupplyPaymentTask = supplyPaymentTask;

                toReturn.Add(vehicleDeliveryService);
            } else {
                VehicleDeliveryService fromList = toReturn.First(s => s.Id.Equals(vehicleDeliveryService.Id));

                if (invoiceDocument != null && !fromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id))) fromList.InvoiceDocuments.Add(invoiceDocument);
            }

            return vehicleDeliveryService;
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

    public VehicleDeliveryService GetByIdWithoutIncludes(long id) {
        return _connection.Query<VehicleDeliveryService>(
            "SELECT * FROM [VehicleDeliveryService] " +
            "WHERE [VehicleDeliveryService].[ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }
}