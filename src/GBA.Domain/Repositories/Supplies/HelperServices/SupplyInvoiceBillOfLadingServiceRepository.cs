using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Repositories.Supplies.HelperServices.Contracts;

namespace GBA.Domain.Repositories.Supplies.HelperServices;

public sealed class SupplyInvoiceBillOfLadingServiceRepository : ISupplyInvoiceBillOfLadingServiceRepository {
    private readonly IDbConnection _connection;

    public SupplyInvoiceBillOfLadingServiceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void RemoveByBillOfLadingId(long id) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceBillOfLadingService] " +
            "SET [SupplyInvoiceBillOfLadingService].[Deleted] = 1 " +
            ",[Updated] = getutcdate() " +
            ",[Value] = 0 " +
            ",[AccountingValue] = 0 " +
            "WHERE [SupplyInvoiceBillOfLadingService].[BillOfLadingServiceID] = @Id; ",
            new { Id = id });
    }

    public List<SupplyInvoiceBillOfLadingService> GetByBillOfLadingServiceId(long id) {
        List<SupplyInvoiceBillOfLadingService> toReturn = new();

        Type[] types = {
            typeof(SupplyInvoiceBillOfLadingService),
            typeof(SupplyInvoice),
            typeof(PackingList),
            typeof(PackingListPackageOrderItem)
        };

        Func<object[], SupplyInvoiceBillOfLadingService> mapper = objects => {
            SupplyInvoiceBillOfLadingService supplyInvoiceBillOfLadingService = (SupplyInvoiceBillOfLadingService)objects[0];
            SupplyInvoice invoice = (SupplyInvoice)objects[1];
            PackingList packingList = (PackingList)objects[2];
            PackingListPackageOrderItem item = (PackingListPackageOrderItem)objects[3];

            if (!toReturn.Any(x => x.Id.Equals(supplyInvoiceBillOfLadingService.Id)))
                toReturn.Add(supplyInvoiceBillOfLadingService);
            else
                supplyInvoiceBillOfLadingService = toReturn.First(x => x.Id.Equals(supplyInvoiceBillOfLadingService.Id));

            if (invoice == null) return supplyInvoiceBillOfLadingService;

            supplyInvoiceBillOfLadingService.SupplyInvoice = invoice;

            if (!invoice.PackingLists.Any(x => x.Id.Equals(packingList.Id)))
                invoice.PackingLists.Add(packingList);
            else
                packingList = invoice.PackingLists.FirstOrDefault(x => x.Id.Equals(packingList.Id));

            if (packingList == null) return supplyInvoiceBillOfLadingService;

            packingList.PackingListPackageOrderItems.Add(item);

            return supplyInvoiceBillOfLadingService;
        };

        _connection.Query(";WITH [TOTAL_CTE] AS ( " +
                          "SELECT [SupplyInvoiceBillOfLadingService].[ID] " +
                          ",ROUND( " +
                          "SUM([PackingListPackageOrderItem].[UnitPrice] *  " +
                          "[PackingListPackageOrderItem].[Qty]) " +
                          ",2) AS [TotalNetPrice] " +
                          ",ROUND( " +
                          "SUM([SupplyOrderItem].NetWeight) " +
                          ",2) AS [TotalNetWeight] " +
                          ",ROUND(SUM([PackingListPackage].CBM), 2) AS [TotalCBM]  " +
                          "FROM [SupplyInvoiceBillOfLadingService] " +
                          "LEFT JOIN [SupplyInvoice] " +
                          "ON [SupplyInvoice].[ID] = [SupplyInvoiceBillOfLadingService].SupplyInvoiceID " +
                          "LEFT JOIN [PackingList] " +
                          "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
                          "AND [PackingList].[Deleted] = 0 " +
                          "LEFT JOIN [PackingListPackage] " +
                          "ON [PackingListPackage].[PackingListID] = [PackingList].[ID] " +
                          "LEFT JOIN [PackingListPackageOrderItem] " +
                          "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
                          "AND [PackingListPackageOrderItem].[Deleted] = 0 " +
                          "LEFT JOIN [SupplyInvoiceOrderItem] " +
                          "ON [PackingListPackageOrderItem].SupplyInvoiceOrderItemID = [SupplyInvoiceOrderItem].[ID] " +
                          "LEFT JOIN [SupplyOrderItem] " +
                          "ON [SupplyInvoiceOrderItem].SupplyOrderItemID = [SupplyOrderItem].[ID] " +
                          "WHERE [SupplyInvoiceBillOfLadingService].[BillOfLadingServiceID] = @Id " +
                          "AND [SupplyInvoiceBillOfLadingService].[Deleted] = 0 " +
                          "AND [SupplyInvoice].[Deleted] = 0 " +
                          "GROUP BY [SupplyInvoiceBillOfLadingService].[ID] " +
                          ") " +
                          "SELECT [SupplyInvoiceBillOfLadingService].* " +
                          ",[SupplyInvoice].* " +
                          ", CASE " +
                          "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
                          "THEN dbo.GetGovExchangedToUahValue( " +
                          "[TOTAL_CTE].[TotalNetPrice] + [SupplyInvoice].[DeliveryAmount] - [SupplyInvoice].[DiscountAmount], " +
                          "[Currency].[ID], " +
                          "[SupplyInvoice].[DateCustomDeclaration]) " +
                          "ELSE dbo.GetGovExchangedToUahValue( " +
                          "[TOTAL_CTE].[TotalNetPrice] + [SupplyInvoice].[DeliveryAmount] - [SupplyInvoice].[DiscountAmount], " +
                          "[Currency].[ID], " +
                          "[SupplyInvoice].[DateFrom]) " +
                          "END [TotalNetPrice] " +
                          ",[TOTAL_CTE].[TotalNetWeight] " +
                          ",[TOTAL_CTE].[TotalCBM] " +
                          ",[PackingList].* " +
                          ",[PackingListPackageOrderItem].* " +
                          "FROM [SupplyInvoiceBillOfLadingService] " +
                          "LEFT JOIN [SupplyInvoice] " +
                          "ON [SupplyInvoice].[ID] = [SupplyInvoiceBillOfLadingService].SupplyInvoiceID " +
                          "LEFT JOIN [PackingList] " +
                          "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
                          "AND [PackingList].[Deleted] = 0 " +
                          "LEFT JOIN [PackingListPackageOrderItem] " +
                          "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
                          "AND [PackingListPackageOrderItem].[Deleted] = 0 " +
                          "LEFT JOIN [SupplyOrder] " +
                          "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
                          "LEFT JOIN [ClientAgreement] " +
                          "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
                          "LEFT JOIN [Agreement] " +
                          "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                          "LEFT JOIN [Currency] " +
                          "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                          "LEFT JOIN [TOTAL_CTE] " +
                          "ON [TOTAL_CTE].[ID] = [SupplyInvoiceBillOfLadingService].[ID] " +
                          "WHERE [TOTAL_CTE].[ID] IS NOT NULL",
            types, mapper,
            new { Id = id });

        return toReturn;
    }

    public void UpdateExtraValue(ICollection<SupplyInvoiceBillOfLadingService> invoices) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceBillOfLadingService] " +
            "SET [Value] = @Value " +
            ", [AccountingValue] = @AccountingValue " +
            ", [IsCalculatedValue] = 1 " +
            ", [Updated] = getutcdate() " +
            "WHERE [ID] = @Id; ",
            invoices);
    }

    public void UnassignAllBillOfLadingServiceIdExceptProvided(long serviceId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceBillOfLadingService] " +
            "SET [Deleted] = 1 " +
            ",[Updated] = getutcdate() " +
            ", [Value] = 0 " +
            ", [AccountingValue] = 0 " +
            "WHERE [SupplyInvoiceBillOfLadingService].[BillOfLadingServiceID] = @Id " +
            "AND [SupplyInvoiceBillOfLadingService].[SupplyInvoiceID] NOT IN @Ids",
            new { Id = serviceId, Ids = ids }
        );
    }

    public void UpdateAssign(long serviceId, long id) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceBillOfLadingService] " +
            "SET [Deleted] = 0 " +
            "WHERE [SupplyInvoiceBillOfLadingService].[SupplyInvoiceID] = @Id " +
            "AND [SupplyInvoiceBillOfLadingService].[BillOfLadingServiceID] = @ServiceId; ",
            new {
                ServiceId = serviceId,
                Id = id
            });
    }

    public long Add(SupplyInvoiceBillOfLadingService supplyInvoiceBillOfLadingService) {
        return _connection.Query<long>(
            "INSERT INTO [SupplyInvoiceBillOfLadingService]([Updated], [SupplyInvoiceID], [BillOfLadingServiceID], [Value], [AccountingValue]) " +
            "VALUES (getutcdate(), @SupplyInvoiceID, @BillOfLadingServiceID, @Value, @AccountingValue); " +
            "SELECT SCOPE_IDENTITY() ",
            supplyInvoiceBillOfLadingService).Single();
    }

    public void UpdateSupplyInvoiceId(long fromInvoiceId, long toInvoiceId) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceBillOfLadingService] " +
            "SET SupplyInvoiceID = @ToInvoiceId, Updated = GETUTCDATE() " +
            "WHERE SupplyInvoiceID = @FromInvoiceId",
            new { FromInvoiceId = fromInvoiceId, ToInvoiceId = toInvoiceId }
        );
    }

    public SupplyInvoiceBillOfLadingService GetById(long serviceId, long id) {
        return _connection.Query<SupplyInvoiceBillOfLadingService>(
            "SELECT * FROM [SupplyInvoiceBillOfLadingService] " +
            "WHERE [SupplyInvoiceBillOfLadingService].[BillOfLadingServiceID] = @ServiceId " +
            "AND [SupplyInvoiceBillOfLadingService].[SupplyInvoiceID] = @Id; ",
            new {
                ServiceId = serviceId,
                Id = id
            }).FirstOrDefault();
    }

    public void ResetExtraValue(IEnumerable<long> ids, long id) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceBillOfLadingService] " +
            "SET [Value] = 0 " +
            ", [AccountingValue] = 0 " +
            ", [IsCalculatedValue] = 0 " +
            ", [Updated] = getutcdate() " +
            "WHERE [SupplyInvoiceBillOfLadingService].[BillOfLadingServiceID] = @Id " +
            "AND [SupplyInvoiceBillOfLadingService].[ID] NOT IN @Ids ",
            new { Ids = ids, Id = id });
    }

    public List<SupplyInvoiceBillOfLadingService> GetBySupplyInvoiceId(long id) {
        return _connection.Query<SupplyInvoiceBillOfLadingService, BillOfLadingService, SupplyInvoiceBillOfLadingService>(
            "SELECT * FROM [SupplyInvoiceBillOfLadingService] " +
            "LEFT JOIN [BillOfLadingService] " +
            "ON [BillOfLadingService].[ID] = [SupplyInvoiceBillOfLadingService].[BillOfLadingServiceID] " +
            "WHERE [SupplyInvoiceBillOfLadingService].[SupplyInvoiceID] = @Id " +
            "AND [SupplyInvoiceBillOfLadingService].[Deleted] = 0 " +
            "AND [BillOfLadingService].[IsCalculatedValue] = 1; ",
            (supplyInvoiceService, service) => {
                supplyInvoiceService.BillOfLadingService = service;

                return supplyInvoiceService;
            },
            new { Id = id }).ToList();
    }

    public IEnumerable<long> GetSupplyInvoiceIdByBillOfLadingServiceId(long id) {
        return _connection.Query<long>(
            "SELECT [SupplyInvoiceBillOfLadingService].[SupplyInvoiceID] " +
            "FROM [SupplyInvoiceBillOfLadingService] " +
            "WHERE [SupplyInvoiceBillOfLadingService].[BillOfLadingServiceID] = @Id " +
            "AND [SupplyInvoiceBillOfLadingService].[Deleted] = 0 ",
            new { Id = id }).ToList();
    }

    public List<SupplyInvoiceBillOfLadingService> GetBySupplyInvoiceIds(IEnumerable<long> ids) {
        List<SupplyInvoiceBillOfLadingService> toReturn = new();

        Type[] types = {
            typeof(SupplyInvoiceBillOfLadingService),
            typeof(SupplyInvoice),
            typeof(PackingList),
            typeof(PackingListPackageOrderItem)
        };

        Func<object[], SupplyInvoiceBillOfLadingService> mapper = objects => {
            SupplyInvoiceBillOfLadingService supplyInvoiceBillOfLadingService = (SupplyInvoiceBillOfLadingService)objects[0];
            SupplyInvoice invoice = (SupplyInvoice)objects[1];
            PackingList packingList = (PackingList)objects[2];
            PackingListPackageOrderItem item = (PackingListPackageOrderItem)objects[3];

            if (!toReturn.Any(x => x.Id.Equals(supplyInvoiceBillOfLadingService.Id)))
                toReturn.Add(supplyInvoiceBillOfLadingService);
            else
                supplyInvoiceBillOfLadingService = toReturn.First(x => x.Id.Equals(supplyInvoiceBillOfLadingService.Id));

            if (invoice == null) return supplyInvoiceBillOfLadingService;

            supplyInvoiceBillOfLadingService.SupplyInvoice = invoice;

            if (!invoice.PackingLists.Any(x => x.Id.Equals(packingList.Id)))
                invoice.PackingLists.Add(packingList);
            else
                packingList = invoice.PackingLists.FirstOrDefault(x => x.Id.Equals(packingList.Id));

            if (packingList == null) return supplyInvoiceBillOfLadingService;

            packingList.PackingListPackageOrderItems.Add(item);

            return supplyInvoiceBillOfLadingService;
        };

        _connection.Query(";WITH [TOTAL_CTE] AS ( " +
                          "SELECT [SupplyInvoiceBillOfLadingService].[ID] " +
                          ",ROUND( " +
                          "SUM([PackingListPackageOrderItem].[UnitPrice] *  " +
                          "[PackingListPackageOrderItem].[Qty]) " +
                          ",2) AS [TotalNetPrice] " +
                          ",ROUND( " +
                          "SUM([SupplyOrderItem].NetWeight) " +
                          ",2) AS [TotalNetWeight] " +
                          ",ROUND(SUM([PackingListPackage].CBM), 2) AS [TotalCBM]  " +
                          "FROM [SupplyInvoiceBillOfLadingService] " +
                          "LEFT JOIN [SupplyInvoice] " +
                          "ON [SupplyInvoice].[ID] = [SupplyInvoiceBillOfLadingService].SupplyInvoiceID " +
                          "LEFT JOIN [PackingList] " +
                          "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
                          "AND [PackingList].[Deleted] = 0 " +
                          "LEFT JOIN [PackingListPackage] " +
                          "ON [PackingListPackage].[PackingListID] = [PackingList].[ID] " +
                          "LEFT JOIN [PackingListPackageOrderItem] " +
                          "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
                          "AND [PackingListPackageOrderItem].[Deleted] = 0 " +
                          "LEFT JOIN [SupplyInvoiceOrderItem] " +
                          "ON [PackingListPackageOrderItem].SupplyInvoiceOrderItemID = [SupplyInvoiceOrderItem].[ID] " +
                          "LEFT JOIN [SupplyOrderItem] " +
                          "ON [SupplyInvoiceOrderItem].SupplyOrderItemID = [SupplyOrderItem].[ID] " +
                          "WHERE [SupplyInvoiceBillOfLadingService].[SupplyInvoiceID] IN @Ids " +
                          "AND [SupplyInvoice].[Deleted] = 0 " +
                          "GROUP BY [SupplyInvoiceBillOfLadingService].[ID] " +
                          ") " +
                          "SELECT [SupplyInvoiceBillOfLadingService].* " +
                          ",[SupplyInvoice].* " +
                          ", CASE " +
                          "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
                          "THEN dbo.GetGovExchangedToUahValue( " +
                          "[TOTAL_CTE].[TotalNetPrice] + [SupplyInvoice].[DeliveryAmount] - [SupplyInvoice].[DiscountAmount], " +
                          "[Currency].[ID], " +
                          "[SupplyInvoice].[DateCustomDeclaration]) " +
                          "ELSE dbo.GetGovExchangedToUahValue( " +
                          "[TOTAL_CTE].[TotalNetPrice] + [SupplyInvoice].[DeliveryAmount] - [SupplyInvoice].[DiscountAmount], " +
                          "[Currency].[ID], " +
                          "[SupplyInvoice].[DateFrom]) " +
                          "END [TotalNetPrice] " +
                          ",[TOTAL_CTE].[TotalNetWeight] " +
                          ",[TOTAL_CTE].[TotalCBM] " +
                          ",[PackingList].* " +
                          ",[PackingListPackageOrderItem].* " +
                          "FROM [SupplyInvoiceBillOfLadingService] " +
                          "LEFT JOIN [SupplyInvoice] " +
                          "ON [SupplyInvoice].[ID] = [SupplyInvoiceBillOfLadingService].SupplyInvoiceID " +
                          "LEFT JOIN [PackingList] " +
                          "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
                          "AND [PackingList].[Deleted] = 0 " +
                          "LEFT JOIN [PackingListPackageOrderItem] " +
                          "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
                          "AND [PackingListPackageOrderItem].[Deleted] = 0 " +
                          "LEFT JOIN [SupplyOrder] " +
                          "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
                          "LEFT JOIN [ClientAgreement] " +
                          "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
                          "LEFT JOIN [Agreement] " +
                          "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                          "LEFT JOIN [Currency] " +
                          "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                          "LEFT JOIN [TOTAL_CTE] " +
                          "ON [TOTAL_CTE].[ID] = [SupplyInvoiceBillOfLadingService].[ID] " +
                          "WHERE [TOTAL_CTE].[ID] IS NOT NULL",
            types, mapper,
            new { Ids = ids });

        return toReturn;
    }
}