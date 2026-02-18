using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Repositories.Supplies.HelperServices.Contracts;

namespace GBA.Domain.Repositories.Supplies.HelperServices;

public sealed class SupplyInvoiceMergedServiceRepository : ISupplyInvoiceMergedServiceRepository {
    private readonly IDbConnection _connection;

    public SupplyInvoiceMergedServiceRepository(
        IDbConnection connection) {
        _connection = connection;
    }

    public void RemoveByMergedServiceId(long id) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceMergedService] " +
            "SET [Deleted] = 1 " +
            ", [Updated] = getutcdate() " +
            "WHERE [SupplyInvoiceMergedService].[MergedServiceID] = @Id; ",
            new { Id = id });
    }

    public List<SupplyInvoiceMergedService> GetByMergedServiceId(long id) {
        List<SupplyInvoiceMergedService> toReturn = new();

        Type[] types = {
            typeof(SupplyInvoiceMergedService),
            typeof(SupplyInvoice),
            typeof(PackingList),
            typeof(PackingListPackageOrderItem)
        };

        Func<object[], SupplyInvoiceMergedService> mapper = objects => {
            SupplyInvoiceMergedService supplyInvoiceMergedService = (SupplyInvoiceMergedService)objects[0];
            SupplyInvoice invoice = (SupplyInvoice)objects[1];
            PackingList packingList = (PackingList)objects[2];
            PackingListPackageOrderItem item = (PackingListPackageOrderItem)objects[3];

            if (!toReturn.Any(x => x.Id.Equals(supplyInvoiceMergedService.Id)))
                toReturn.Add(supplyInvoiceMergedService);
            else
                supplyInvoiceMergedService = toReturn.First(x => x.Id.Equals(supplyInvoiceMergedService.Id));

            if (invoice == null) return supplyInvoiceMergedService;

            supplyInvoiceMergedService.SupplyInvoice = invoice;

            if (!invoice.PackingLists.Any(x => x.Id.Equals(packingList.Id)))
                invoice.PackingLists.Add(packingList);
            else
                packingList = invoice.PackingLists.FirstOrDefault(x => x.Id.Equals(packingList.Id));

            if (packingList == null) return supplyInvoiceMergedService;

            packingList.PackingListPackageOrderItems.Add(item);

            return supplyInvoiceMergedService;
        };

        _connection.Query(";WITH [TOTAL_CTE] AS ( " +
                          "SELECT [SupplyInvoiceMergedService].[ID] " +
                          ",ROUND( " +
                          "SUM([PackingListPackageOrderItem].[UnitPrice] *  " +
                          "[PackingListPackageOrderItem].[Qty]) " +
                          ",2) AS [TotalNetPrice] " +
                          ",ROUND( " +
                          "SUM([SupplyOrderItem].NetWeight) " +
                          ",2) AS [TotalNetWeight] " +
                          ",ROUND(SUM([PackingListPackage].CBM), 2) AS [TotalCBM]  " +
                          "FROM [SupplyInvoiceMergedService] " +
                          "LEFT JOIN [SupplyInvoice] " +
                          "ON [SupplyInvoice].[ID] = [SupplyInvoiceMergedService].SupplyInvoiceID " +
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
                          "WHERE [SupplyInvoiceMergedService].[MergedServiceID] = @Id " +
                          "AND [SupplyInvoiceMergedService].[Deleted] = 0 " +
                          "AND [SupplyInvoice].[Deleted] = 0 " +
                          "GROUP BY [SupplyInvoiceMergedService].[ID] " +
                          ") " +
                          "SELECT [SupplyInvoiceMergedService].* " +
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
                          "FROM [SupplyInvoiceMergedService] " +
                          "LEFT JOIN [SupplyInvoice] " +
                          "ON [SupplyInvoice].[ID] = [SupplyInvoiceMergedService].SupplyInvoiceID " +
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
                          "ON [TOTAL_CTE].[ID] = [SupplyInvoiceMergedService].[ID] " +
                          "WHERE [TOTAL_CTE].[ID] IS NOT NULL",
            types, mapper,
            new { Id = id });

        return toReturn;
    }

    public void UpdateExtraValue(ICollection<SupplyInvoiceMergedService> invoices) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceMergedService] " +
            "SET [Value] = @Value " +
            ", [AccountingValue] = @AccountingValue " +
            ", [IsCalculatedValue] = 1 " +
            ", [Updated] = getutcdate() " +
            "WHERE [ID] = @Id; ",
            invoices);
    }

    public void UpdateAssign(long serviceId, long id) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceMergedService] " +
            "SET [Deleted] = 0 " +
            "WHERE [SupplyInvoiceMergedService].[SupplyInvoiceID] = @Id " +
            "AND [SupplyInvoiceMergedService].[MergedServiceID] = @ServiceId; ",
            new {
                ServiceId = serviceId,
                Id = id
            });
    }

    public void Add(SupplyInvoiceMergedService supplyInvoiceMergedService) {
        _connection.Execute(
            "INSERT INTO [SupplyInvoiceMergedService]([Updated], [SupplyInvoiceID], [MergedServiceID], [Value], [AccountingValue]) " +
            "VALUES (getutcdate(), @SupplyInvoiceID, @MergedServiceID, @Value, @AccountingValue) ",
            supplyInvoiceMergedService);
    }

    public void UpdateSupplyInvoiceId(long fromInvoiceId, long toInvoiceId) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceMergedService] " +
            "SET SupplyInvoiceID = @ToInvoiceId, Updated = GETUTCDATE() " +
            "WHERE SupplyInvoiceID = @FromInvoiceId",
            new { FromInvoiceId = fromInvoiceId, ToInvoiceId = toInvoiceId }
        );
    }

    public void UnassignAllMergedServiceIdExceptProvided(long serviceId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceMergedService] " +
            "SET [Deleted] = 1 " +
            ",[Updated] = getutcdate() " +
            "WHERE [SupplyInvoiceMergedService].[MergedServiceID] = @Id " +
            "AND [SupplyInvoiceMergedService].[SupplyInvoiceID] NOT IN @Ids",
            new { Id = serviceId, Ids = ids }
        );
    }

    public SupplyInvoiceMergedService GetById(long serviceId, long id) {
        return _connection.Query<SupplyInvoiceMergedService>(
            "SELECT * FROM [SupplyInvoiceMergedService] " +
            "WHERE [SupplyInvoiceMergedService].[MergedServiceID] = @ServiceId " +
            "AND [SupplyInvoiceMergedService].[SupplyInvoiceID] = @Id; ",
            new {
                ServiceId = serviceId,
                Id = id
            }).FirstOrDefault();
    }

    public void ResetExtraValue(IEnumerable<long> ids, long serviceId) {
        _connection.Execute(
            "UPDATE [SupplyInvoiceMergedService] " +
            "SET [Value] = 0 " +
            ", [AccountingValue] = 0 " +
            ", [IsCalculatedValue] = 0 " +
            ", [Updated] = getutcdate() " +
            "WHERE [SupplyInvoiceMergedService].[MergedServiceID] = @Id " +
            "AND [SupplyInvoiceMergedService].[ID] NOT IN @Ids ",
            new { Ids = ids, Id = serviceId });
    }

    public List<SupplyInvoiceMergedService> GetBySupplyInvoiceId(long id) {
        return _connection.Query<SupplyInvoiceMergedService, MergedService, ConsumableProduct, SupplyInvoiceMergedService>(
            "SELECT * FROM [SupplyInvoiceMergedService] " +
            "LEFT JOIN [MergedService] " +
            "ON [MergedService].[ID] = [SupplyInvoiceMergedService].[MergedServiceID] " +
            "LEFT JOIN [ConsumableProduct] " +
            "ON [ConsumableProduct].[ID] = [MergedService].[ConsumableProductID] " +
            "WHERE [SupplyInvoiceMergedService].[SupplyInvoiceID] = @Id " +
            "AND [SupplyInvoiceMergedService].[Deleted] = 0 " +
            "AND [MergedService].[IsCalculatedValue] = 1 ",
            (invoiceService, service, consumableProduct) => {
                service.ConsumableProduct = consumableProduct;
                invoiceService.MergedService = service;
                return invoiceService;
            },
            new { Id = id }).ToList();
    }

    public IEnumerable<long> GetSupplyInvoiceIdByMergedServiceId(long id) {
        return _connection.Query<long>(
            "SELECT [SupplyInvoiceMergedService].[SupplyInvoiceID] " +
            "FROM [SupplyInvoiceMergedService] " +
            "WHERE [SupplyInvoiceMergedService].[MergedServiceID] = @Id " +
            "AND [SupplyInvoiceMergedService].[Deleted] = 0; ",
            new { Id = id }).ToList();
    }

    public List<SupplyInvoiceMergedService> GetBySupplyInvoiceIds(IEnumerable<long> ids) {
        List<SupplyInvoiceMergedService> toReturn = new();

        Type[] types = {
            typeof(SupplyInvoiceMergedService),
            typeof(SupplyInvoice),
            typeof(PackingList),
            typeof(PackingListPackageOrderItem)
        };

        Func<object[], SupplyInvoiceMergedService> mapper = objects => {
            SupplyInvoiceMergedService supplyInvoiceMergedService = (SupplyInvoiceMergedService)objects[0];
            SupplyInvoice invoice = (SupplyInvoice)objects[1];
            PackingList packingList = (PackingList)objects[2];
            PackingListPackageOrderItem item = (PackingListPackageOrderItem)objects[3];

            if (!toReturn.Any(x => x.Id.Equals(supplyInvoiceMergedService.Id)))
                toReturn.Add(supplyInvoiceMergedService);
            else
                supplyInvoiceMergedService = toReturn.First(x => x.Id.Equals(supplyInvoiceMergedService.Id));

            if (invoice == null) return supplyInvoiceMergedService;

            supplyInvoiceMergedService.SupplyInvoice = invoice;

            if (!invoice.PackingLists.Any(x => x.Id.Equals(packingList.Id)))
                invoice.PackingLists.Add(packingList);
            else
                packingList = invoice.PackingLists.FirstOrDefault(x => x.Id.Equals(packingList.Id));

            if (packingList == null) return supplyInvoiceMergedService;

            packingList.PackingListPackageOrderItems.Add(item);

            return supplyInvoiceMergedService;
        };

        _connection.Query(";WITH [TOTAL_CTE] AS ( " +
                          "SELECT [SupplyInvoiceMergedService].[ID] " +
                          ",ROUND( " +
                          "SUM([PackingListPackageOrderItem].[UnitPrice] *  " +
                          "[PackingListPackageOrderItem].[Qty]) " +
                          ",2) AS [TotalNetPrice] " +
                          ",ROUND( " +
                          "SUM([SupplyOrderItem].NetWeight) " +
                          ",2) AS [TotalNetWeight] " +
                          ",ROUND(SUM([PackingListPackage].CBM), 2) AS [TotalCBM]  " +
                          "FROM [SupplyInvoiceMergedService] " +
                          "LEFT JOIN [SupplyInvoice] " +
                          "ON [SupplyInvoice].[ID] = [SupplyInvoiceMergedService].SupplyInvoiceID " +
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
                          "WHERE [SupplyInvoiceMergedService].[SupplyInvoiceID] IN @Ids " +
                          "AND [SupplyInvoice].[Deleted] = 0 " +
                          "GROUP BY [SupplyInvoiceMergedService].[ID] " +
                          ") " +
                          "SELECT [SupplyInvoiceMergedService].* " +
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
                          "FROM [SupplyInvoiceMergedService] " +
                          "LEFT JOIN [SupplyInvoice] " +
                          "ON [SupplyInvoice].[ID] = [SupplyInvoiceMergedService].SupplyInvoiceID " +
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
                          "ON [TOTAL_CTE].[ID] = [SupplyInvoiceMergedService].[ID] " +
                          "WHERE [TOTAL_CTE].[ID] IS NOT NULL",
            types, mapper,
            new { Ids = ids });

        return toReturn;
    }
}