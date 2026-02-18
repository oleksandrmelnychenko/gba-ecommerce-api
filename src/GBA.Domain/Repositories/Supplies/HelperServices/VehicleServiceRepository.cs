using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.HelperServices;

public sealed class VehicleServiceRepository : IVehicleServiceRepository {
    private readonly IDbConnection _connection;

    public VehicleServiceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(VehicleService vehicleService) {
        return _connection.Query<long>(
                "INSERT INTO VehicleService (IsCalculatedExtraCharge, SupplyExtraChargeType, GrossWeight, FromDate, VehicleOrganizationID, TermDeliveryInDays, " +
                "IsActive, BillOfLadingDocumentID, UserId, SupplyPaymentTaskId, LoadDate, GrossPrice, NetPrice, Vat, Number, Name, VatPercent, VehicleNumber, ServiceNumber, " +
                "SupplyOrganizationAgreementId, Updated, AccountingGrossPrice, AccountingNetPrice, AccountingVat, AccountingPaymentTaskId, AccountingVatPercent, " +
                "[AccountingSupplyCostsWithinCountry], [SupplyInformationTaskID], [ExchangeRate], [AccountingExchangeRate], [IsIncludeAccountingValue], " +
                "[ActProvidingServiceDocumentID], [SupplyServiceAccountDocumentID]) " +
                "VALUES(@IsCalculatedExtraCharge, @SupplyExtraChargeType, @GrossWeight, @FromDate, @VehicleOrganizationID, @TermDeliveryInDays, @IsActive, " +
                "@BillOfLadingDocumentID, @UserId, @SupplyPaymentTaskId, @LoadDate, @GrossPrice, @NetPrice, @Vat, @Number, @Name, @VatPercent, " +
                "@VehicleNumber, @ServiceNumber, @SupplyOrganizationAgreementId, getutcdate(), @AccountingGrossPrice, @AccountingNetPrice, @AccountingVat, @AccountingPaymentTaskId, " +
                "@AccountingVatPercent, @AccountingSupplyCostsWithinCountry, @SupplyInformationTaskId, @ExchangeRate, @AccountingExchangeRate, @IsIncludeAccountingValue, " +
                "@ActProvidingServiceDocumentId, @SupplyServiceAccountDocumentId); " +
                "SELECT SCOPE_IDENTITY()",
                vehicleService
            )
            .Single();
    }

    public void Update(VehicleService vehicleService) {
        _connection.Execute(
            "UPDATE VehicleService " +
            "SET IsCalculatedExtraCharge = @IsCalculatedExtraCharge, SupplyExtraChargeType = @SupplyExtraChargeType, GrossWeight = @GrossWeight, FromDate = @FromDate, " +
            "VehicleOrganizationID = @VehicleOrganizationID, TermDeliveryInDays = @TermDeliveryInDays, IsActive = @IsActive, VehicleNumber = @VehicleNumber, " +
            "BillOfLadingDocumentID = @BillOfLadingDocumentID, LoadDate = @LoadDate, UserId = @UserId, SupplyPaymentTaskId = @SupplyPaymentTaskId, GrossPrice = @GrossPrice, " +
            "NetPrice = @NetPrice, Vat = @Vat, Number = @Number, Name = @Name, VatPercent = @VatPercent, Updated = getutcdate(), AccountingGrossPrice = @AccountingGrossPrice, " +
            "AccountingNetPrice = @AccountingNetPrice, AccountingVat = @AccountingVat, AccountingPaymentTaskId = @AccountingPaymentTaskId, " +
            "AccountingVatPercent = @AccountingVatPercent, SupplyOrganizationAgreementId = @SupplyOrganizationAgreementId, " +
            "[AccountingSupplyCostsWithinCountry] = @AccountingSupplyCostsWithinCountry, [SupplyInformationTaskID] = @SupplyInformationTaskId, " +
            "[ExchangeRate] = @ExchangeRate, [AccountingExchangeRate] = @AccountingExchangeRate, [IsIncludeAccountingValue] = @IsIncludeAccountingValue, " +
            "[ActProvidingServiceDocumentID] = @ActProvidingServiceDocumentId, [SupplyServiceAccountDocumentID] = @SupplyServiceAccountDocumentId " +
            "WHERE NetUID = @NetUID",
            vehicleService
        );
    }

    public VehicleService GetByNetId(Guid netId) {
        VehicleService vehicleServiceToReturn = null;

        string sqlExpression = "SELECT * FROM VehicleService " +
                               "LEFT OUTER JOIN [SupplyOrganization] AS VehicleOrganization " +
                               "ON VehicleOrganization.ID = VehicleService.VehicleOrganizationID " +
                               "LEFT OUTER JOIN BillOfLadingDocument " +
                               "ON BillOfLadingDocument.ID = VehicleService.BillOfLadingDocumentID AND BillOfLadingDocument.Deleted = 0 " +
                               "LEFT OUTER JOIN [User] AS [VehicleService.User] " +
                               "ON [VehicleService.User].ID = VehicleService.UserID " +
                               "LEFT OUTER JOIN SupplyPaymentTask " +
                               "ON SupplyPaymentTask.ID = VehicleService.SupplyPaymentTaskID AND SupplyPaymentTask.Deleted = 0 " +
                               "LEFT OUTER JOIN [User] AS [SupplyPaymentTask.User]" +
                               "ON [SupplyPaymentTask.User].ID = SupplyPaymentTask.UserID " +
                               "LEFT OUTER JOIN InvoiceDocument " +
                               "ON InvoiceDocument.VehicleServiceID = VehicleService.ID AND InvoiceDocument.Deleted = 0 " +
                               "WHERE VehiclerService.NetUID = @NetId ";

        Type[] types = {
            typeof(VehicleService),
            typeof(SupplyOrganization),
            typeof(BillOfLadingDocument),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(InvoiceDocument)
        };

        Func<object[], VehicleService> mapper = objects => {
            VehicleService vehicleService = (VehicleService)objects[0];
            SupplyOrganization vehicleOrganization = (SupplyOrganization)objects[1];
            BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[2];
            User vehiclerServiceUser = (User)objects[3];
            SupplyPaymentTask paymentTask = (SupplyPaymentTask)objects[4];
            User paymentTaskUser = (User)objects[5];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];

            if (vehiclerServiceUser != null)
                vehicleService.User = vehiclerServiceUser;

            if (billOfLadingDocument != null)
                vehicleService.BillOfLadingDocument = billOfLadingDocument;

            if (vehicleOrganization != null)
                vehicleService.VehicleOrganization = vehicleOrganization;

            if (paymentTask != null) {
                paymentTask.User = paymentTaskUser;
                vehicleService.SupplyPaymentTask = paymentTask;
            }

            if (vehicleServiceToReturn != null) {
                if (invoiceDocument != null && !vehicleServiceToReturn.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                    vehicleServiceToReturn.InvoiceDocuments.Add(invoiceDocument);
            } else {
                if (invoiceDocument != null)
                    vehicleService.InvoiceDocuments.Add(invoiceDocument);

                vehicleServiceToReturn = vehicleService;
            }

            return vehicleService;
        };

        var props = new { NetId = netId };

        _connection.Query(sqlExpression, types, mapper, props);

        return vehicleServiceToReturn;
    }

    public VehicleService GetBySupplyOrderVehiclesServiceNetIdWithoutIncludes(Guid netId) {
        return _connection.Query<VehicleService>(
                "SELECT [VehicleService].* " +
                "FROM [SupplyOrderVehicleService] " +
                "LEFT JOIN [VehicleService] " +
                "ON [VehicleService].ID = [SupplyOrderVehicleService].VehicleServiceID " +
                "WHERE [SupplyOrderVehicleService].NetUID = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public void SetIsExtraChargeCalculatedByNetId(Guid netId, SupplyExtraChargeType extraChargeType) {
        _connection.Execute(
            "UPDATE VehicleService " +
            "SET IsCalculatedExtraCharge = 1, Updated = getutcdate(), SupplyExtraChargeType = @SupplyExtraChargeType " +
            "WHERE ID = (SELECT [SupplyOrderVehicleService].VehicleServiceID FROM [SupplyOrderVehicleService] WHERE [SupplyOrderVehicleService].NetUID = @NetId)",
            new { NetId = netId, SupplyExtraChargeType = extraChargeType }
        );
    }

    public Guid GetSupplyOrderNetIdBySupplyOrderVehicleServiceNetId(Guid netId) {
        return _connection.Query<Guid>(
                "SELECT [SupplyOrder].NetUID " +
                "FROM [SupplyOrderVehicleService] " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyOrderVehicleService].SupplyOrderID " +
                "WHERE [SupplyOrderVehicleService].NetUID = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public List<VehicleService> GetAllRanged(DateTime from, DateTime to) {
        List<VehicleService> vehiclesServicesToReturn = new();

        _connection.Query<VehicleService, User, BillOfLadingDocument, SupplyPaymentTask, User, SupplyOrganization, VehicleService>(
                "SELECT * FROM VehicleService " +
                "LEFT JOIN [User] AS [VehicleServiceUser] " +
                "ON [VehicleServiceUser].ID = VehicleService.UserID " +
                "LEFT JOIN BillOfLadingDocument " +
                "ON BillOfLadingDocument.ID = VehicleService.BillOfLadingDocumentID " +
                "LEFT JOIN SupplyPaymentTask " +
                "ON SupplyPaymentTask.ID = VehicleService.SupplyPaymentTaskID " +
                "LEFT JOIN [User] AS [VehicleServiceSupplyPaymentTaskUser] " +
                "ON [VehicleServiceSupplyPaymentTaskUser].ID = SupplyPaymentTask.UserID " +
                "LEFT JOIN [SupplyOrganization] AS [VehicleOrganization] " +
                "ON [VehicleOrganization].ID = [VehicleService].VehicleOrganizationID " +
                "WHERE VehicleService.LoadDate >= @From " +
                "AND VehicleService.LoadDate <= @To " +
                "AND VehicleService.Deleted = 0",
                (vehicleService, vehicleServiceUser, billOfLadingDocument, supplyPaymentTask, supplyPaymentTaskUser, organization) => {
                    if (supplyPaymentTask != null) {
                        supplyPaymentTask.User = supplyPaymentTaskUser;
                        vehicleService.SupplyPaymentTask = supplyPaymentTask;
                    }

                    vehicleService.User = vehicleServiceUser;
                    vehicleService.BillOfLadingDocument = billOfLadingDocument;
                    vehicleService.VehicleOrganization = organization;

                    if (!vehiclesServicesToReturn.Any(c => c.Id.Equals(vehicleService.Id))) vehiclesServicesToReturn.Add(vehicleService);

                    return vehicleService;
                },
                new { From = from, To = to }
            )
            .ToList();

        return vehiclesServicesToReturn;
    }

    public List<VehicleService> GetAllAvailable() {
        List<VehicleService> toReturn = new();

        Type[] types = {
            typeof(VehicleService),
            typeof(SupplyOrder),
            typeof(PackingList),
            typeof(SupplyInvoice),
            typeof(SupplyOrganization),
            typeof(BillOfLadingDocument),
            typeof(SupplyOrganizationAgreement),
            typeof(Client),
            typeof(SupplyOrderVehicleService)
        };

        Func<object[], VehicleService> mapper = objects => {
            VehicleService service = (VehicleService)objects[0];
            SupplyOrder supplyOrder = (SupplyOrder)objects[1];
            PackingList packingList = (PackingList)objects[2];
            SupplyInvoice invoice = (SupplyInvoice)objects[3];
            SupplyOrganization organization = (SupplyOrganization)objects[4];
            BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[5];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[6];
            Client client = (Client)objects[7];
            SupplyOrderVehicleService supplyOrderVehicleService = (SupplyOrderVehicleService)objects[8];

            if (service != null && !service.IsNew()) {
                if (toReturn.Any(s => s.Id.Equals(service.Id))) {
                    if (packingList != null) {
                        VehicleService fromList = toReturn.First(s => s.Id.Equals(service.Id));

                        if (!fromList.PackingLists.Any(p => p.Id.Equals(packingList.Id))) {
                            packingList.SupplyInvoice = invoice;

                            fromList.PackingLists.Add(packingList);
                        }
                    }

                    supplyOrder.Client = client;

                    supplyOrderVehicleService.SupplyOrder = supplyOrder;

                    if (!service.SupplyOrderVehicleServices.Any(x => x.Id.Equals(supplyOrderVehicleService.Id)))
                        service.SupplyOrderVehicleServices.Add(supplyOrderVehicleService);
                    else
                        supplyOrderVehicleService = service.SupplyOrderVehicleServices.FirstOrDefault(x => x.Id.Equals(supplyOrderVehicleService.Id));

                    service.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                } else {
                    if (packingList != null) {
                        packingList.SupplyInvoice = invoice;

                        service.PackingLists.Add(packingList);
                    }

                    service.VehicleOrganization = organization;
                    service.BillOfLadingDocument = billOfLadingDocument;

                    supplyOrder.Client = client;

                    supplyOrderVehicleService.SupplyOrder = supplyOrder;

                    if (!service.SupplyOrderVehicleServices.Any(x => x.Id.Equals(supplyOrderVehicleService.Id)))
                        service.SupplyOrderVehicleServices.Add(supplyOrderVehicleService);
                    else
                        supplyOrderVehicleService = service.SupplyOrderVehicleServices.FirstOrDefault(x => x.Id.Equals(supplyOrderVehicleService.Id));

                    service.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    toReturn.Add(service);
                }
            }

            return service;
        };

        _connection.Query("SELECT " +
                          "[VehicleService].* " +
                          ",[SupplyOrder].* " +
                          ",[PackingList].* " +
                          ",[SupplyInvoice].* " +
                          ",[VehicleOrganization].* " +
                          ",[BillOfLadingDocument].* " +
                          ",[SupplyOrganizationAgreement].* " +
                          ",[Client].* " +
                          ",[SupplyOrderVehicleService].* " +
                          "FROM [SupplyOrderVehicleService] " +
                          "LEFT JOIN [SupplyOrder] " +
                          "ON [SupplyOrder].ID = [SupplyOrderVehicleService].SupplyOrderID " +
                          "LEFT JOIN [VehicleService] " +
                          "ON [VehicleService].ID = [SupplyOrderVehicleService].VehicleServiceID " +
                          "LEFT JOIN [PackingList] " +
                          "ON [PackingList].VehicleServiceID = [VehicleService].ID " +
                          "AND [PackingList].Deleted = 0 " +
                          "LEFT JOIN [SupplyInvoice] " +
                          "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
                          "LEFT JOIN [SupplyOrganization] AS [VehicleOrganization] " +
                          "ON [VehicleOrganization].ID = [VehicleService].VehicleOrganizationID " +
                          "LEFT JOIN [BillOfLadingDocument] " +
                          "ON [BillOfLadingDocument].ID = [VehicleService].BillOfLadingDocumentID " +
                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                          "ON [SupplyOrganizationAgreement].[ID] = [VehicleService].[SupplyOrganizationAgreementID] " +
                          "LEFT JOIN [Client] " +
                          "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
                          "WHERE [SupplyOrderVehicleService].Deleted = 0 " +
                          "AND [SupplyOrder].IsOrderShipped = 0", types, mapper);

        return toReturn;
    }

    public string GetTermDeliveryInDaysById(long vehicleServiceId) {
        return _connection.Query<string>(
                "SELECT TermDeliveryInDays FROM VehicleService WHERE ID = @Id",
                new { Id = vehicleServiceId }
            )
            .SingleOrDefault();
    }

    public void UpdateDeliveryTerms(long vehicleServiceId, string termDeliveryInDays) {
        _connection.Execute(
            "UPDATE VehicleService SET TermDeliveryInDays = @Days WHERE ID = @Id",
            new { Id = vehicleServiceId, Days = termDeliveryInDays }
        );
    }

    public VehicleService GetByIdWithoutIncludes(long vehicleServiceId) {
        return _connection.Query<VehicleService>(
            "SELECT * FROM [VehicleService] " +
            "WHERE [VehicleService].[ID] = @Id; ",
            new { Id = vehicleServiceId }).FirstOrDefault();
    }

    public bool IsVehicleInOrder(Guid supplyOrderNetId, Guid vehicleServiceNetId) {
        return _connection.Query<bool>(
            "SELECT " +
            "TOP(1) IIF([VehicleService].[ID] IS NULL, 0, 1) " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [SupplyOrderVehicleService] " +
            "ON [SupplyOrderVehicleService].[SupplyOrderID] = [SupplyOrder].[ID] " +
            "LEFT JOIN [VehicleService] " +
            "ON [VehicleService].[ID] = [SupplyOrderVehicleService].[VehicleServiceID] " +
            "WHERE [VehicleService].[NetUID] = @ServiceNetId " +
            "AND [SupplyOrder].[NetUID] = @SupplyOrderNetId " +
            "AND [SupplyOrderVehicleService].[Deleted] = 0 " +
            "AND [SupplyOrder].[Deleted] = 0",
            new {
                SupplyOrderNetId = supplyOrderNetId,
                ServiceNetId = vehicleServiceNetId
            }).Single();
    }

    public long GetIdByNetId(Guid vehicleServiceNetId) {
        return _connection.Query<long>(
            "SELECT [VehicleService].[ID] FROM [VehicleService] " +
            "WHERE [VehicleService].[NetUID] = @NetId ",
            new { NetId = vehicleServiceNetId }).Single();
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [VehicleService] " +
            "SET [Deleted] = 1 " +
            ", [Updated] = getutcdate() " +
            "WHERE [VehicleService].[Id] = @Id; ",
            new { Id = id });
    }

    public VehicleService GetById(long id) {
        return _connection.Query<VehicleService>(
            "SELECT * FROM [VehicleService] " +
            "WHERE [VehicleService].[ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }
}