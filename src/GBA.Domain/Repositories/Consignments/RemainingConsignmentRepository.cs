using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers.Consignments;
using GBA.Domain.Repositories.Consignments.Contracts;

namespace GBA.Domain.Repositories.Consignments;

public sealed class RemainingConsignmentRepository : IRemainingConsignmentRepository {
    private readonly IDbConnection _connection;

    public RemainingConsignmentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public IEnumerable<RemainingConsignment> GetAllByProductNetId(Guid productNetId) {
        return _connection.Query<RemainingConsignment, Product, RemainingConsignment>(
            ";WITH [RemainingConsignments_CTE] " +
            "AS ( " +
            "SELECT [Product].ID AS [ProductID] " +
            ", [ConsignmentItem].RemainingQty AS [RemainingQty] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ISNULL([Supplier].FullName, N'') AS [SupplierName] " +
            ", [ProductIncome].FromDate AS [FromDate] " +
            ", [ProductIncome].Number AS [ProductIncomeNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [SupplyInvoice].ID IS NOT NULL " +
            "THEN [SupplyInvoice].Number " +
            "WHEN [SupplyOrderUkraine].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraine].InvNumber " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].Number " +
            "WHEN [ProductCapitalization].ID IS NOT NULL " +
            "THEN [ProductCapitalization].Number " +
            "ELSE N'' " +
            "END " +
            ") AS [InvoiceNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", N'EUR' AS [CurrencyName] " +
            ", [ConsignmentItem].NetPrice AS [NetPrice] " +
            ", [ConsignmentItem].NetPrice * [ConsignmentItem].[RemainingQty] AS [TotalNetPrice] " +
            ", [ConsignmentItem].Price * [ConsignmentItem].[RemainingQty] AS [GrossPrice] " +
            ", [ConsignmentItem].AccountingPrice * [ConsignmentItem].[RemainingQty] AS [AccountingGrossPrice] " +
            ", [ConsignmentItem].[Weight] AS [Weight] " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [ClientAgreement] AS [SupplyOrderUkraineClientAgreement] " +
            "ON [SupplyOrderUkraineClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = ( " +
            "CASE " +
            "WHEN [SupplyOrder].ID IS NOT NULL " +
            "THEN [SupplyOrder].ClientID " +
            "WHEN [SupplyOrderUkraineClientAgreement].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraineClientAgreement].ClientID " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].ClientID " +
            "ELSE 0 " +
            "END " +
            ") " +
            "WHERE [Product].NetUID = @ProductNetId " +
            "AND [ConsignmentItem].RemainingQty <> 0 " +
            "), " +
            "[Grouped_CTE] " +
            "AS ( " +
            "SELECT [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[CurrencyName] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[TotalNetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[AccountingGrossPrice] " +
            ", SUM([RemainingItems].[RemainingQty]) AS [RemainingQty] " +
            ", [RemainingItems].[Weight] " +
            "FROM [RemainingConsignments_CTE] AS [RemainingItems] " +
            "GROUP BY [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[CurrencyName] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[TotalNetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[AccountingGrossPrice] " +
            ", [RemainingItems].[Weight] " +
            ") " +
            "SELECT [Grouped_CTE].* " +
            ", [Product].* " +
            "FROM [Grouped_CTE] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [Grouped_CTE].ProductID " +
            "ORDER BY [Grouped_CTE].[FromDate] ",
            (remainingConsignment, product) => {
                remainingConsignment.Product = product;

                return remainingConsignment;
            },
            new { ProductNetId = productNetId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public IEnumerable<RemainingConsignment> GetAllByStorageNetId(Guid storageNetId) {
        return _connection.Query<RemainingConsignment, Product, RemainingConsignment>(
            ";WITH [RemainingConsignments_CTE] " +
            "AS ( " +
            "SELECT [Product].ID AS [ProductID] " +
            ", [ConsignmentItem].RemainingQty AS [RemainingQty] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ISNULL([Supplier].FullName, N'') AS [SupplierName] " +
            ", [ProductIncome].FromDate AS [FromDate] " +
            ", [ProductIncome].Number AS [ProductIncomeNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [SupplyInvoice].ID IS NOT NULL " +
            "THEN [SupplyInvoice].Number " +
            "WHEN [SupplyOrderUkraine].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraine].InvNumber " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].Number " +
            "WHEN [ProductCapitalization].ID IS NOT NULL " +
            "THEN [ProductCapitalization].Number " +
            "ELSE N'' " +
            "END " +
            ") AS [InvoiceNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", N'EUR' AS [CurrencyName] " +
            ", [ConsignmentItem].NetPrice AS [NetPrice] " +
            ", [ConsignmentItem].Price AS [GrossPrice] " +
            ", [ConsignmentItem].[Weight] AS [Weight] " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [ClientAgreement] AS [SupplyOrderUkraineClientAgreement] " +
            "ON [SupplyOrderUkraineClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = ( " +
            "CASE " +
            "WHEN [SupplyOrder].ID IS NOT NULL " +
            "THEN [SupplyOrder].ClientID " +
            "WHEN [SupplyOrderUkraineClientAgreement].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraineClientAgreement].ClientID " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].ClientID " +
            "ELSE 0 " +
            "END " +
            ") " +
            "WHERE [Storage].NetUID = @StorageNetId " +
            "AND [ConsignmentItem].RemainingQty <> 0 " +
            "), " +
            "[Grouped_CTE] " +
            "AS ( " +
            "SELECT [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[CurrencyName] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", SUM([RemainingItems].[RemainingQty]) AS [RemainingQty] " +
            ", [RemainingItems].[Weight] " +
            "FROM [RemainingConsignments_CTE] AS [RemainingItems] " +
            "GROUP BY [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[CurrencyName] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[Weight] " +
            ") " +
            "SELECT [Grouped_CTE].* " +
            ", [Product].* " +
            "FROM [Grouped_CTE] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [Grouped_CTE].ProductID " +
            "ORDER BY [Grouped_CTE].[FromDate] ",
            (remainingConsignment, product) => {
                remainingConsignment.Product = product;

                return remainingConsignment;
            },
            new { StorageNetId = storageNetId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public IEnumerable<RemainingConsignment> GetAllByProductIncomeNetId(Guid productIncomeNetId) {
        return _connection.Query<RemainingConsignment, Product, RemainingConsignment>(
            ";WITH [RemainingConsignments_CTE] " +
            "AS ( " +
            "SELECT [Product].ID AS [ProductID] " +
            ", [ConsignmentItem].RemainingQty AS [RemainingQty] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ISNULL([Supplier].FullName, N'') AS [SupplierName] " +
            ", [ProductIncome].FromDate AS [FromDate] " +
            ", [ProductIncome].Number AS [ProductIncomeNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [SupplyInvoice].ID IS NOT NULL " +
            "THEN [SupplyInvoice].Number " +
            "WHEN [SupplyOrderUkraine].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraine].InvNumber " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].Number " +
            "WHEN [ProductCapitalization].ID IS NOT NULL " +
            "THEN [ProductCapitalization].Number " +
            "ELSE N'' " +
            "END " +
            ") AS [InvoiceNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", N'EUR' AS [CurrencyName] " +
            ", [ConsignmentItem].NetPrice AS [NetPrice] " +
            ", [ConsignmentItem].NetPrice * [ConsignmentItem].[RemainingQty] AS [TotalNetPrice] " +
            ", [ConsignmentItem].Price * [ConsignmentItem].[RemainingQty] AS [GrossPrice] " +
            ", [ConsignmentItem].AccountingPrice * [ConsignmentItem].[RemainingQty] AS [AccountingGrossPrice] " +
            ", [ConsignmentItem].[Weight] AS [Weight] " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [ClientAgreement] AS [SupplyOrderUkraineClientAgreement] " +
            "ON [SupplyOrderUkraineClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = ( " +
            "CASE " +
            "WHEN [SupplyOrder].ID IS NOT NULL " +
            "THEN [SupplyOrder].ClientID " +
            "WHEN [SupplyOrderUkraineClientAgreement].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraineClientAgreement].ClientID " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].ClientID " +
            "ELSE 0 " +
            "END " +
            ") " +
            "WHERE [ProductIncome].NetUID = @ProductIncomeNetId " +
            "AND [ConsignmentItem].RemainingQty <> 0 " +
            "), " +
            "[Grouped_CTE] " +
            "AS ( " +
            "SELECT [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[CurrencyName] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[TotalNetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[AccountingGrossPrice] " +
            ", SUM([RemainingItems].[RemainingQty]) AS [RemainingQty] " +
            ", [RemainingItems].[Weight] " +
            "FROM [RemainingConsignments_CTE] AS [RemainingItems] " +
            "GROUP BY [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[CurrencyName] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[TotalNetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[AccountingGrossPrice] " +
            ", [RemainingItems].[Weight] " +
            ") " +
            "SELECT [Grouped_CTE].* " +
            ", [Product].* " +
            "FROM [Grouped_CTE] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [Grouped_CTE].ProductID " +
            "ORDER BY [Grouped_CTE].[FromDate] ",
            (remainingConsignment, product) => {
                remainingConsignment.Product = product;

                return remainingConsignment;
            },
            new { ProductIncomeNetId = productIncomeNetId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public IEnumerable<AvailableConsignment> GetAllAvailableConsignmentsForSupplyReturnByProductAndStorageNetIds(Guid productNetId, Guid storageNetId) {
        return _connection.Query<AvailableConsignment, Organization, Client, ClientAgreement, Agreement, Currency, AvailableConsignment>(
            ";WITH [SearchAvailableConsignments_CTE] " +
            "AS ( " +
            "SELECT [ConsignmentItem].ID, [RootConsignmentItem].ID AS [RootItemID] " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "OUTER APPLY dbo.GetTopRootConsignmentItemByConsignmentItemId([ConsignmentItem].ID) AS [RootConsignmentItem] " +
            "WHERE [ConsignmentItem].RemainingQty <> 0 " +
            "AND [Storage].NetUID = @StorageNetId " +
            "AND [Product].NetUID = @ProductNetId " +
            "), " +
            "[FilteredData_CTE] " +
            "AS ( " +
            "SELECT [ProductIncome].Number AS [ProductIncomeNumber] " +
            ", [ProductIncome].FromDate AS [FromDate] " +
            ", ( " +
            "CASE " +
            "WHEN [RootItem].ID IS NOT NULL " +
            "THEN [RootItem].ID " +
            "ELSE [ConsignmentItem].ID " +
            "END " +
            ") AS [ConsignmentItemId] " +
            ", [ConsignmentItem].RemainingQty " +
            ", [Consignment].OrganizationID AS [OrganizationId] " +
            ", [Supplier].ID AS [SupplierId] " +
            ", ( " +
            "CASE " +
            "WHEN [SupplyOrder].ID IS NOT NULL " +
            "THEN [SupplyOrder].ClientAgreementID " +
            "WHEN [SupplyOrderUkraineClientAgreement].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraineClientAgreement].ID " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [Sale].ClientAgreementID " +
            "ELSE 0 " +
            "END " +
            ") AS [SupplierAgreementId] " +
            ", ROW_NUMBER() OVER(ORDER BY (CASE WHEN [RootItem].ID IS NULL THEN 0 ELSE 1 END), [ProductIncome].[FromDate]) AS [RowNumber] " +
            "FROM [SearchAvailableConsignments_CTE] AS [SearchResult] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [SearchResult].ID = [ConsignmentItem].ID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ConsignmentItem] AS [RootItem] " +
            "ON [SearchResult].RootItemID = [RootItem].ID " +
            "LEFT JOIN [Consignment] AS [RootConsignment] " +
            "ON [RootConsignment].ID = [RootItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = ( " +
            "CASE " +
            "WHEN [RootConsignment].ProductIncomeID IS NOT NULL " +
            "THEN [RootConsignment].ProductIncomeID " +
            "ELSE [Consignment].ProductIncomeID " +
            "END " +
            ") " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [ClientAgreement] AS [SupplyOrderUkraineClientAgreement] " +
            "ON [SupplyOrderUkraineClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            " " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [OrderItem].OrderID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = ( " +
            "CASE " +
            "WHEN [SupplyOrder].ID IS NOT NULL " +
            "THEN [SupplyOrder].ClientID " +
            "WHEN [SupplyOrderUkraineClientAgreement].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraineClientAgreement].ClientID " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].ClientID " +
            "ELSE 0 " +
            "END " +
            ") " +
            "WHERE [Supplier].ID IS NOT NULL " +
            "), " +
            "[Grouped_CTE] " +
            "AS ( " +
            "SELECT [FilteredData_CTE].ConsignmentItemId " +
            ", [FilteredData_CTE].FromDate " +
            ", [FilteredData_CTE].SupplierId " +
            ", [FilteredData_CTE].SupplierAgreementId " +
            ", [FilteredData_CTE].OrganizationId " +
            ", [FilteredData_CTE].ProductIncomeNumber " +
            ", SUM([FilteredData_CTE].RemainingQty) AS [RemainingQty] " +
            "FROM [FilteredData_CTE] " +
            "GROUP BY [FilteredData_CTE].ConsignmentItemId " +
            ", [FilteredData_CTE].FromDate " +
            ", [FilteredData_CTE].SupplierId " +
            ", [FilteredData_CTE].SupplierAgreementId " +
            ", [FilteredData_CTE].OrganizationId " +
            ", [FilteredData_CTE].ProductIncomeNumber " +
            ") " +
            "SELECT * " +
            "FROM [Grouped_CTE] AS [AvailableItem] " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [AvailableItem].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [AvailableItem].SupplierId " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [AvailableItem].SupplierAgreementId " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "ORDER BY [AvailableItem].FromDate ",
            (availableConsignment, organization, supplier, clientAgreement, agreement, currency) => {
                if (agreement != null)
                    agreement.Currency = currency;

                clientAgreement.Agreement = agreement;

                availableConsignment.ClientAgreement = clientAgreement;
                availableConsignment.Supplier = supplier;
                availableConsignment.Organization = organization;

                return availableConsignment;
            },
            new {
                ProductNetId = productNetId,
                StorageNetId = storageNetId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );
    }

    public IEnumerable<RemainingConsignment> GetAllByStorageNetIdFiltered(
        Guid storageNetId,
        Guid? supplierNetId,
        DateTime from,
        DateTime to,
        string searchValue,
        int limit,
        int offset) {
        return _connection.Query<RemainingConsignment, Product, RemainingConsignment>(
            ";WITH [RemainingConsignments_CTE] " +
            "AS ( " +
            "SELECT [Product].ID AS [ProductID] " +
            ", [ConsignmentItem].RemainingQty AS [RemainingQty] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ISNULL([Supplier].FullName, N'') AS [SupplierName] " +
            ", [ProductIncome].FromDate AS [FromDate] " +
            ", [ProductIncome].Number AS [ProductIncomeNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [SupplyInvoice].ID IS NOT NULL " +
            "THEN [SupplyInvoice].Number " +
            "WHEN [SupplyOrderUkraine].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraine].InvNumber " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].Number " +
            "WHEN [ProductCapitalization].ID IS NOT NULL " +
            "THEN [ProductCapitalization].Number " +
            "ELSE N'' " +
            "END " +
            ") AS [InvoiceNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", N'EUR' AS [CurrencyName] " +
            ", [ConsignmentItem].NetPrice AS [NetPrice] " +
            ", [ConsignmentItem].Price AS [GrossPrice] " +
            ", [ConsignmentItem].AccountingPrice AS [AccountingGrossPrice] " +
            ", [ConsignmentItem].[Weight] AS [Weight] " +
            ", [ConsignmentItem].NetUID AS [ConsignmentItemNetId] " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [ClientAgreement] AS [SupplyOrderUkraineClientAgreement] " +
            "ON [SupplyOrderUkraineClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = ( " +
            "CASE " +
            "WHEN [SupplyOrder].ID IS NOT NULL " +
            "THEN [SupplyOrder].ClientID " +
            "WHEN [SupplyOrderUkraineClientAgreement].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraineClientAgreement].ClientID " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].ClientID " +
            "ELSE 0 " +
            "END " +
            ") " +
            "WHERE [Storage].NetUID = @StorageNetId " +
            "AND [ConsignmentItem].RemainingQty <> 0 " +
            "AND [ProductIncome].FromDate >= @From " +
            "AND [ProductIncome].FromDate <= @To " +
            (
                supplierNetId.HasValue
                    ? "AND [Supplier].NetUID = @SupplierNetId "
                    : string.Empty
            ) +
            "AND PATINDEX(N'%' + @SearchValue + N'%', [Product].VendorCode) > 0 " +
            "), " +
            "[Grouped_CTE] " +
            "AS ( " +
            "SELECT [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[CurrencyName] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[AccountingGrossPrice] " +
            ", SUM([RemainingItems].[RemainingQty]) AS [RemainingQty] " +
            ", [RemainingItems].[Weight] " +
            ", MAX([RemainingItems].ConsignmentItemNetId) AS [ConsignmentItemNetId] " +
            ", ROW_NUMBER() OVER(ORDER BY [RemainingItems].[FromDate]) AS [RowNumber] " +
            "FROM [RemainingConsignments_CTE] AS [RemainingItems] " +
            "GROUP BY [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[CurrencyName] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[AccountingGrossPrice] " +
            ", [RemainingItems].[Weight] " +
            ") " +
            "SELECT [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[CurrencyName] " +
            ", ROUND([RemainingItems].[NetPrice] * [RemainingItems].[RemainingQty], 2) AS [NetPrice] " +
            ", ROUND([RemainingItems].[GrossPrice] * [RemainingItems].[RemainingQty], 2) AS [GrossPrice] " +
            ", ROUND([RemainingItems].[AccountingGrossPrice] * [RemainingItems].[RemainingQty], 2) AS [AccountingGrossPrice] " +
            ", [RemainingItems].[RemainingQty] " +
            ", ROUND([RemainingItems].[Weight] * [RemainingItems].[RemainingQty], 3) AS [Weight] " +
            ", [RemainingItems].[ConsignmentItemNetId] " +
            ", [Product].* " +
            "FROM [Grouped_CTE] AS [RemainingItems] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [RemainingItems].ProductID " +
            "WHERE [RemainingItems].RowNumber > @Offset " +
            "AND [RemainingItems].RowNumber <= @Limit + @Offset " +
            "ORDER BY [RemainingItems].[FromDate] ",
            (remainingConsignment, product) => {
                remainingConsignment.Product = product;

                return remainingConsignment;
            },
            new {
                StorageNetId = storageNetId,
                SupplierNetId = supplierNetId ?? Guid.Empty,
                SearchValue = searchValue,
                From = from,
                To = to,
                Limit = limit,
                Offset = offset,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );
    }

    public Tuple<decimal, decimal, decimal, decimal, double> GetTotalEuroAndLocalAmountsForRemainingConsignmentsByStorageNetId(Guid? storageNetId) {
        return _connection.Query<decimal, decimal, decimal, decimal, double, Tuple<decimal, decimal, decimal, decimal, double>>(
            ";WITH [RemainingConsignments_CTE] " +
            "AS ( " +
            "SELECT [Product].ID AS [ProductID] " +
            ", [ConsignmentItem].RemainingQty AS [RemainingQty] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ISNULL([Supplier].FullName, N'') AS [SupplierName] " +
            ", [ProductIncome].FromDate AS [FromDate] " +
            ", [ProductIncome].Number AS [ProductIncomeNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [SupplyInvoice].ID IS NOT NULL " +
            "THEN [SupplyInvoice].Number " +
            "WHEN [SupplyOrderUkraine].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraine].InvNumber " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].Number " +
            "WHEN [ProductCapitalization].ID IS NOT NULL " +
            "THEN [ProductCapitalization].Number " +
            "ELSE N'' " +
            "END " +
            ") AS [InvoiceNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", N'EUR' AS [CurrencyName] " +
            ", [ConsignmentItem].NetPrice AS [NetPrice] " +
            ", [ConsignmentItem].Price AS [GrossPrice] " +
            ", [ConsignmentItem].AccountingPrice AS [AccountingGrossPrice] " +
            ", [ConsignmentItem].[Weight] AS [Weight] " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [ClientAgreement] AS [SupplyOrderUkraineClientAgreement] " +
            "ON [SupplyOrderUkraineClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = ( " +
            "CASE " +
            "WHEN [SupplyOrder].ID IS NOT NULL " +
            "THEN [SupplyOrder].ClientID " +
            "WHEN [SupplyOrderUkraineClientAgreement].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraineClientAgreement].ClientID " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].ClientID " +
            "ELSE 0 " +
            "END " +
            ") " +
            "WHERE [ConsignmentItem].RemainingQty <> 0 " +
            (
                storageNetId.HasValue
                    ? "AND [Storage].NetUID = @StorageNetId "
                    : string.Empty
            ) +
            "), " +
            "[Grouped_CTE] " +
            "AS ( " +
            "SELECT [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[CurrencyName] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[AccountingGrossPrice] " +
            ", SUM([RemainingItems].[RemainingQty]) AS [RemainingQty] " +
            ", [RemainingItems].[Weight] " +
            "FROM [RemainingConsignments_CTE] AS [RemainingItems] " +
            "GROUP BY [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[CurrencyName] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[AccountingGrossPrice] " +
            ", [RemainingItems].[Weight] " +
            ") " +
            "SELECT ROUND(ISNULL(SUM([Grouped_CTE].GrossPrice * CAST([Grouped_CTE].RemainingQty AS money)), 0), 2) AS [TotalEuro] " +
            ", ROUND(ISNULL(SUM([Grouped_CTE].AccountingGrossPrice * CAST([Grouped_CTE].RemainingQty AS money)), 0), 2) AS [AccountingTotalEuro] " +
            ", ROUND(ISNULL(SUM([Grouped_CTE].GrossPrice * CAST([Grouped_CTE].RemainingQty AS money) * dbo.GetCurrentEuroExchangeRateByCulture(@Culture)), 0), 2) AS [TotalLocal] " +
            ", ROUND(ISNULL(SUM([Grouped_CTE].AccountingGrossPrice * CAST([Grouped_CTE].RemainingQty AS money) * dbo.GetCurrentEuroExchangeRateByCulture(@Culture)), 0), 2) AS [AccountingTotalLocal] " +
            ", ISNULL(SUM([Grouped_CTE].RemainingQty), 0) [TotalQty] " +
            "FROM [Grouped_CTE] ",
            (euroAmount, accountingEuroAmount, localAmount, accountingLocalAmount, totalQty) =>
                new Tuple<decimal, decimal, decimal, decimal, double>(euroAmount, accountingEuroAmount, localAmount, accountingLocalAmount, totalQty),
            new { StorageNetId = storageNetId ?? Guid.Empty, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName },
            splitOn: "AccountingTotalEuro,TotalLocal,AccountingTotalLocal,TotalQty"
        ).Single();
    }

    public Tuple<decimal, decimal, decimal, decimal, double> GetTotalEuroAndLocalAmountsForRemainingConsignmentsByStorageNetIdFiltered(
        Guid storageNetId,
        Guid? supplierNetId,
        DateTime from,
        DateTime to,
        string searchValue) {
        return _connection.Query<decimal, decimal, decimal, decimal, double, Tuple<decimal, decimal, decimal, decimal, double>>(
            ";WITH [RemainingConsignments_CTE] " +
            "AS ( " +
            "SELECT [Product].ID AS [ProductID] " +
            ", [ConsignmentItem].RemainingQty AS [RemainingQty] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ISNULL([Supplier].FullName, N'') AS [SupplierName] " +
            ", [ProductIncome].FromDate AS [FromDate] " +
            ", [ProductIncome].Number AS [ProductIncomeNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [SupplyInvoice].ID IS NOT NULL " +
            "THEN [SupplyInvoice].Number " +
            "WHEN [SupplyOrderUkraine].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraine].InvNumber " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].Number " +
            "WHEN [ProductCapitalization].ID IS NOT NULL " +
            "THEN [ProductCapitalization].Number " +
            "ELSE N'' " +
            "END " +
            ") AS [InvoiceNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", N'EUR' AS [CurrencyName] " +
            ", [ConsignmentItem].NetPrice AS [NetPrice] " +
            ", [ConsignmentItem].Price AS [GrossPrice] " +
            ", [ConsignmentItem].AccountingPrice AS [AccountingGrossPrice] " +
            ", [ConsignmentItem].[Weight] AS [Weight] " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [ClientAgreement] AS [SupplyOrderUkraineClientAgreement] " +
            "ON [SupplyOrderUkraineClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = ( " +
            "CASE " +
            "WHEN [SupplyOrder].ID IS NOT NULL " +
            "THEN [SupplyOrder].ClientID " +
            "WHEN [SupplyOrderUkraineClientAgreement].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraineClientAgreement].ClientID " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].ClientID " +
            "ELSE 0 " +
            "END " +
            ") " +
            "WHERE [Storage].NetUID = @StorageNetId " +
            "AND [ConsignmentItem].RemainingQty <> 0 " +
            "AND [ProductIncome].FromDate >= @From " +
            "AND [ProductIncome].FromDate <= @To " +
            (
                supplierNetId.HasValue
                    ? "AND [Supplier].NetUID = @SupplierNetId "
                    : string.Empty
            ) +
            "AND PATINDEX(N'%' + @SearchValue + N'%', [Product].VendorCode) > 0 " +
            "), " +
            "[Grouped_CTE] " +
            "AS ( " +
            "SELECT [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[CurrencyName] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[AccountingGrossPrice] " +
            ", SUM([RemainingItems].[RemainingQty]) AS [RemainingQty] " +
            ", [RemainingItems].[Weight] " +
            "FROM [RemainingConsignments_CTE] AS [RemainingItems] " +
            "GROUP BY [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[CurrencyName] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[AccountingGrossPrice] " +
            ", [RemainingItems].[Weight] " +
            ") " +
            "SELECT ROUND(ISNULL(SUM([Grouped_CTE].GrossPrice * CAST([Grouped_CTE].RemainingQty AS money)), 0), 2) AS [TotalEuro] " +
            ", ROUND(ISNULL(SUM([Grouped_CTE].AccountingGrossPrice * CAST([Grouped_CTE].RemainingQty AS money)), 0), 2) AS [AccountingTotalEuro] " +
            ", ROUND(ISNULL(SUM([Grouped_CTE].GrossPrice * CAST([Grouped_CTE].RemainingQty AS money) * dbo.GetCurrentEuroExchangeRateByCulture(@Culture)), 0), 2) AS [TotalLocal] " +
            ", ROUND(ISNULL(SUM([Grouped_CTE].AccountingGrossPrice * CAST([Grouped_CTE].RemainingQty AS money) * dbo.GetCurrentEuroExchangeRateByCulture(@Culture)), 0), 2) AS [AccountingTotalLocal] " +
            ", ISNULL(SUM([Grouped_CTE].RemainingQty), 0) [TotalQty] " +
            "FROM [Grouped_CTE] ",
            (euroAmount, accountingEuroAmount, localAmount, accountingLocalAmount, totalQty) =>
                new Tuple<decimal, decimal, decimal, decimal, double>(euroAmount, accountingEuroAmount, localAmount, accountingLocalAmount, totalQty),
            new {
                StorageNetId = storageNetId,
                SupplierNetId = supplierNetId ?? Guid.Empty,
                SearchValue = searchValue,
                From = from,
                To = to,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            },
            splitOn: "AccountingTotalEuro,TotalLocal,AccountingTotalLocal,TotalQty"
        ).Single();
    }

    public Tuple<decimal, decimal, decimal, decimal, double> GetTotalEuroAndLocalAmountsForRemainingConsignmentsByStorageNetIdFiltered(
        Guid? storageNetId,
        Guid? supplierNetId,
        DateTime from,
        DateTime to) {
        return _connection.Query<decimal, decimal, decimal, decimal, double, Tuple<decimal, decimal, decimal, decimal, double>>(
            ";WITH [RemainingConsignments_CTE] " +
            "AS ( " +
            "SELECT [Product].ID AS [ProductID] " +
            ", [ConsignmentItem].RemainingQty AS [RemainingQty] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ISNULL([Supplier].FullName, N'') AS [SupplierName] " +
            ", [ProductIncome].FromDate AS [FromDate] " +
            ", [ProductIncome].Number AS [ProductIncomeNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [SupplyInvoice].ID IS NOT NULL " +
            "THEN [SupplyInvoice].Number " +
            "WHEN [SupplyOrderUkraine].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraine].InvNumber " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].Number " +
            "WHEN [ProductCapitalization].ID IS NOT NULL " +
            "THEN [ProductCapitalization].Number " +
            "ELSE N'' " +
            "END " +
            ") AS [InvoiceNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", N'EUR' AS [CurrencyName] " +
            ", [ConsignmentItem].NetPrice AS [NetPrice] " +
            ", [ConsignmentItem].Price AS [GrossPrice] " +
            ", [ConsignmentItem].AccountingPrice AS [AccountingGrossPrice] " +
            ", [ConsignmentItem].[Weight] AS [Weight] " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [ClientAgreement] AS [SupplyOrderUkraineClientAgreement] " +
            "ON [SupplyOrderUkraineClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = ( " +
            "CASE " +
            "WHEN [SupplyOrder].ID IS NOT NULL " +
            "THEN [SupplyOrder].ClientID " +
            "WHEN [SupplyOrderUkraineClientAgreement].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraineClientAgreement].ClientID " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].ClientID " +
            "ELSE 0 " +
            "END " +
            ") " +
            "WHERE [ConsignmentItem].RemainingQty <> 0 " +
            "AND [ProductIncome].FromDate >= @From " +
            "AND [ProductIncome].FromDate <= @To " +
            (
                supplierNetId.HasValue
                    ? "AND [Supplier].NetUID = @SupplierNetId "
                    : string.Empty
            ) +
            (
                storageNetId.HasValue
                    ? "AND [Storage].NetUID = @StorageNetId "
                    : string.Empty
            ) +
            "), " +
            "[Grouped_CTE] " +
            "AS ( " +
            "SELECT [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[CurrencyName] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[AccountingGrossPrice] " +
            ", SUM([RemainingItems].[RemainingQty]) AS [RemainingQty] " +
            ", [RemainingItems].[Weight] " +
            "FROM [RemainingConsignments_CTE] AS [RemainingItems] " +
            "GROUP BY [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[CurrencyName] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[AccountingGrossPrice] " +
            ", [RemainingItems].[Weight] " +
            ") " +
            "SELECT ROUND(ISNULL(SUM([Grouped_CTE].GrossPrice * CAST([Grouped_CTE].RemainingQty AS money)), 0), 2) AS [TotalEuro] " +
            ", ROUND(ISNULL(SUM([Grouped_CTE].AccountingGrossPrice * CAST([Grouped_CTE].RemainingQty AS money)), 0), 2) AS [AccountingTotalEuro] " +
            ", ROUND(ISNULL(SUM([Grouped_CTE].GrossPrice * CAST([Grouped_CTE].RemainingQty AS money) * dbo.GetCurrentEuroExchangeRateByCulture(@Culture)), 0), 2) AS [TotalLocal] " +
            ", ROUND(ISNULL(SUM([Grouped_CTE].AccountingGrossPrice * CAST([Grouped_CTE].RemainingQty AS money) * dbo.GetCurrentEuroExchangeRateByCulture(@Culture)), 0), 2) AS [AccountingTotalLocal] " +
            ", ISNULL(SUM([Grouped_CTE].RemainingQty), 0) [TotalQty] " +
            "FROM [Grouped_CTE] ",
            (euroAmount, accountingEuroAmount, localAmount, accountingLocalAmount, totalQty) =>
                new Tuple<decimal, decimal, decimal, decimal, double>(euroAmount, accountingEuroAmount, localAmount, accountingLocalAmount, totalQty),
            new {
                StorageNetId = storageNetId ?? Guid.Empty,
                SupplierNetId = supplierNetId ?? Guid.Empty,
                From = from,
                To = to,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            },
            splitOn: "AccountingTotalEuro,TotalLocal,AccountingTotalLocal,TotalQty"
        ).Single();
    }

    public IEnumerable<GroupedConsignment> GetGroupedByStorageNetIdFiltered(
        Guid? storageNetId,
        Guid? supplierNetId,
        DateTime from,
        DateTime to,
        int limit,
        int offset) {
        List<GroupedConsignment> groupedConsignments = new();

        _connection.Query<GroupedConsignment, GroupedConsignmentItem, Product, GroupedConsignment>(
            "DECLARE @RemainingItems TABLE ( " +
            "[FromDate] datetime2, " +
            "[StorageName] nvarchar(150), " +
            "[SupplierName] nvarchar(250), " +
            "[InvoiceNumber] nvarchar(50), " +
            "[ProductIncomeNumber] nvarchar(50), " +
            "[OrganizationName] nvarchar(150), " +
            "[ProductID] bigint, " +
            "[NetPrice] money, " +
            "[GrossPrice] money, " +
            "[AccountingGrossPrice] money, " +
            "[RemainingQty] float, " +
            "[Weight] float, " +
            "[RowNumber] int " +
            "); " +
            ";WITH [RemainingConsignments_CTE] " +
            "AS ( " +
            "SELECT [Product].ID AS [ProductID] " +
            ", [ConsignmentItem].RemainingQty AS [RemainingQty] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ISNULL([Supplier].FullName, N'') AS [SupplierName] " +
            ", [ProductIncome].FromDate AS [FromDate] " +
            ", [ProductIncome].Number AS [ProductIncomeNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [SupplyInvoice].ID IS NOT NULL " +
            "THEN [SupplyInvoice].Number " +
            "WHEN [SupplyOrderUkraine].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraine].InvNumber " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].Number " +
            "WHEN [ProductCapitalization].ID IS NOT NULL " +
            "THEN [ProductCapitalization].Number " +
            "ELSE N'' " +
            "END " +
            ") AS [InvoiceNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [ConsignmentItem].NetPrice AS [NetPrice] " +
            ", [ConsignmentItem].Price AS [GrossPrice] " +
            ", [ConsignmentItem].AccountingPrice AS [AccountingGrossPrice] " +
            ", [ConsignmentItem].[Weight] AS [Weight] " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [ClientAgreement] AS [SupplyOrderUkraineClientAgreement] " +
            "ON [SupplyOrderUkraineClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = ( " +
            "CASE " +
            "WHEN [SupplyOrder].ID IS NOT NULL " +
            "THEN [SupplyOrder].ClientID " +
            "WHEN [SupplyOrderUkraineClientAgreement].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraineClientAgreement].ClientID " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].ClientID " +
            "ELSE 0 " +
            "END " +
            ") " +
            "WHERE [ConsignmentItem].RemainingQty <> 0" +
            "AND [ConsignmentItem].RemainingQty <> 0 " +
            "AND [ProductIncome].FromDate >= @From " +
            "AND [ProductIncome].FromDate <= @To " +
            (
                supplierNetId.HasValue
                    ? "AND [Supplier].NetUID = @SupplierNetId "
                    : string.Empty
            ) +
            (
                storageNetId.HasValue
                    ? "AND [Storage].NetUID = @StorageNetId "
                    : string.Empty
            ) +
            "), " +
            "[Grouped_CTE] " +
            "AS ( " +
            "SELECT [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[AccountingGrossPrice] " +
            ", SUM([RemainingItems].[RemainingQty]) AS [RemainingQty] " +
            ", [RemainingItems].[Weight] " +
            "FROM [RemainingConsignments_CTE] AS [RemainingItems] " +
            "GROUP BY [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[AccountingGrossPrice] " +
            ", [RemainingItems].[Weight] " +
            ") " +
            "INSERT INTO @RemainingItems " +
            "( " +
            "[FromDate] " +
            ", [StorageName] " +
            ", [SupplierName] " +
            ", [InvoiceNumber] " +
            ", [ProductIncomeNumber] " +
            ", [OrganizationName] " +
            ", [ProductID] " +
            ", [NetPrice] " +
            ", [GrossPrice] " +
            ", [AccountingGrossPrice] " +
            ", [RemainingQty] " +
            ", [Weight] " +
            ") " +
            "SELECT [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", ROUND([RemainingItems].[NetPrice] * [RemainingItems].[RemainingQty], 2) AS [NetPrice] " +
            ", ROUND([RemainingItems].[GrossPrice] * [RemainingItems].[RemainingQty], 2) AS [GrossPrice] " +
            ", ROUND([RemainingItems].[AccountingGrossPrice] * [RemainingItems].[RemainingQty], 3) AS [AccountingGrossPrice] " +
            ", [RemainingItems].[RemainingQty] " +
            ", ROUND([RemainingItems].[Weight] * [RemainingItems].[RemainingQty], 3) AS [Weight] " +
            "FROM [Grouped_CTE] AS [RemainingItems] " +
            ";WITH [GroupedRemainings_CTE] " +
            "AS ( " +
            "SELECT [RemainingItems].FromDate " +
            ", [RemainingItems].ProductIncomeNumber " +
            ", [RemainingItems].InvoiceNumber " +
            ", [RemainingItems].SupplierName " +
            ", [RemainingItems].OrganizationName " +
            ", SUM([RemainingItems].GrossPrice) AS [TotalGrossPrice] " +
            ", SUM([RemainingItems].AccountingGrossPrice) AS [AccountingTotalGrossPrice] " +
            ", SUM([RemainingItems].[Weight]) AS [TotalWeight] " +
            ", ROW_NUMBER() OVER(ORDER BY [RemainingItems].FromDate) AS [RowNumber] " +
            "FROM @RemainingItems AS [RemainingItems] " +
            "GROUP BY [RemainingItems].FromDate " +
            ", [RemainingItems].ProductIncomeNumber " +
            ", [RemainingItems].InvoiceNumber " +
            ", [RemainingItems].SupplierName " +
            ", [RemainingItems].OrganizationName " +
            ") " +
            "SELECT [GroupedItems].FromDate " +
            ", [GroupedItems].ProductIncomeNumber " +
            ", [GroupedItems].InvoiceNumber " +
            ", [GroupedItems].SupplierName " +
            ", [GroupedItems].OrganizationName " +
            ", [GroupedItems].TotalGrossPrice " +
            ", [GroupedItems].AccountingTotalGrossPrice " +
            ", [GroupedItems].TotalWeight " +
            ", [GroupedItems].RowNumber " +
            ", [RemainingItems].ProductID " +
            ", [RemainingItems].NetPrice " +
            ", [RemainingItems].GrossPrice " +
            ", [RemainingItems].AccountingGrossPrice " +
            ", [RemainingItems].RemainingQty " +
            ", [RemainingItems].FromDate " +
            ", [RemainingItems].[Weight] " +
            ", [Product].* " +
            "FROM [GroupedRemainings_CTE] AS [GroupedItems] " +
            "LEFT JOIN @RemainingItems AS [RemainingItems] " +
            "ON [RemainingItems].FromDate = [GroupedItems].FromDate " +
            "AND [RemainingItems].ProductIncomeNumber = [GroupedItems].ProductIncomeNumber " +
            "AND [RemainingItems].InvoiceNumber = [GroupedItems].InvoiceNumber " +
            "AND [RemainingItems].SupplierName = [GroupedItems].SupplierName " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [RemainingItems].ProductID " +
            "WHERE [GroupedItems].RowNumber > @Offset " +
            "AND [GroupedItems].RowNumber <= @Limit + @Offset " +
            "ORDER BY [GroupedItems].RowNumber",
            (groupedConsignment, groupedConsignmentItem, product) => {
                if (groupedConsignments.Any(c => c.RowNumber.Equals(groupedConsignment.RowNumber)))
                    groupedConsignment = groupedConsignments.First(c => c.RowNumber.Equals(groupedConsignment.RowNumber));
                else
                    groupedConsignments.Add(groupedConsignment);

                if (groupedConsignmentItem == null) return groupedConsignment;

                groupedConsignmentItem.Product = product;

                groupedConsignment.GroupedConsignmentItems.Add(groupedConsignmentItem);

                return groupedConsignment;
            },
            new {
                StorageNetId = storageNetId,
                SupplierNetId = supplierNetId ?? Guid.Empty,
                From = from,
                To = to,
                Limit = limit,
                Offset = offset,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            },
            splitOn: "ProductID,ID"
        );

        return groupedConsignments;
    }

    public List<RemainingConsignment> GetAllByStorageForDocumentExport(
        Guid storageNetId,
        Guid? supplierNetId,
        DateTime from,
        DateTime to,
        string searchValue) {
        return _connection.Query<RemainingConsignment, Product, RemainingConsignment>(
            ";WITH [RemainingConsignments_CTE] " +
            "AS ( " +
            "SELECT [Product].ID AS [ProductID] " +
            ", [ConsignmentItem].RemainingQty AS [RemainingQty] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ISNULL([Supplier].FullName, N'') AS [SupplierName] " +
            ", [ProductIncome].FromDate AS [FromDate] " +
            ", [ProductIncome].Number AS [ProductIncomeNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [SupplyInvoice].ID IS NOT NULL " +
            "THEN [SupplyInvoice].Number " +
            "WHEN [SupplyOrderUkraine].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraine].InvNumber " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].Number " +
            "WHEN [ProductCapitalization].ID IS NOT NULL " +
            "THEN [ProductCapitalization].Number " +
            "ELSE N'' " +
            "END " +
            ") AS [InvoiceNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", N'EUR' AS [CurrencyName] " +
            ", [ConsignmentItem].NetPrice AS [NetPrice] " +
            ", [ConsignmentItem].Price AS [GrossPrice] " +
            ", [ConsignmentItem].AccountingPrice AS [AccountingGrossPrice] " +
            ", [ConsignmentItem].[Weight] AS [Weight] " +
            ", [ConsignmentItem].NetUID AS [ConsignmentItemNetId] " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [ClientAgreement] AS [SupplyOrderUkraineClientAgreement] " +
            "ON [SupplyOrderUkraineClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = ( " +
            "CASE " +
            "WHEN [SupplyOrder].ID IS NOT NULL " +
            "THEN [SupplyOrder].ClientID " +
            "WHEN [SupplyOrderUkraineClientAgreement].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraineClientAgreement].ClientID " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].ClientID " +
            "ELSE 0 " +
            "END " +
            ") " +
            "WHERE [Storage].NetUID = @StorageNetId " +
            "AND [ConsignmentItem].RemainingQty <> 0 " +
            "AND [ProductIncome].FromDate >= @From " +
            "AND [ProductIncome].FromDate <= @To " +
            (
                supplierNetId.HasValue
                    ? "AND [Supplier].NetUID = @SupplierNetId "
                    : string.Empty
            ) +
            "AND PATINDEX(N'%' + @SearchValue + N'%', [Product].VendorCode) > 0 " +
            "), " +
            "[Grouped_CTE] " +
            "AS ( " +
            "SELECT [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[CurrencyName] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[AccountingGrossPrice] " +
            ", SUM([RemainingItems].[RemainingQty]) AS [RemainingQty] " +
            ", [RemainingItems].[Weight] " +
            ", MAX([RemainingItems].ConsignmentItemNetId) AS [ConsignmentItemNetId] " +
            "FROM [RemainingConsignments_CTE] AS [RemainingItems] " +
            "GROUP BY [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[CurrencyName] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[AccountingGrossPrice] " +
            ", [RemainingItems].[Weight] " +
            ") " +
            "SELECT [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[CurrencyName] " +
            ", ROUND([RemainingItems].[NetPrice] * [RemainingItems].[RemainingQty], 2) AS [NetPrice] " +
            ", ROUND([RemainingItems].[GrossPrice] * [RemainingItems].[RemainingQty], 2) AS [GrossPrice] " +
            ", ROUND([RemainingItems].[AccountingGrossPrice] * [RemainingItems].[RemainingQty], 2) AS [AccountingGrossPrice] " +
            ", [RemainingItems].[RemainingQty] " +
            ", ROUND([RemainingItems].[Weight] * [RemainingItems].[RemainingQty], 3) AS [Weight] " +
            ", [RemainingItems].[ConsignmentItemNetId] " +
            ", [Product].* " +
            "FROM [Grouped_CTE] AS [RemainingItems] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [RemainingItems].ProductID " +
            "ORDER BY [RemainingItems].[FromDate] ",
            (remainingConsignment, product) => {
                remainingConsignment.Product = product;

                return remainingConsignment;
            },
            new {
                StorageNetId = storageNetId,
                SupplierNetId = supplierNetId ?? Guid.Empty,
                SearchValue = searchValue ?? "",
                From = from,
                To = to,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        ).ToList();
    }

    public List<GroupedConsignment> GetGroupedByStorageForDocumentExport(
        Guid? storageNetId,
        Guid? supplierNetId,
        DateTime from,
        DateTime to) {
        List<GroupedConsignment> groupedConsignments = new();

        _connection.Query<GroupedConsignment, GroupedConsignmentItem, Product, GroupedConsignment>(
            "DECLARE @RemainingItems TABLE ( " +
            "[FromDate] datetime2, " +
            "[StorageName] nvarchar(150), " +
            "[SupplierName] nvarchar(250), " +
            "[InvoiceNumber] nvarchar(50), " +
            "[ProductIncomeNumber] nvarchar(50), " +
            "[OrganizationName] nvarchar(150), " +
            "[ProductID] bigint, " +
            "[NetPrice] money, " +
            "[GrossPrice] money, " +
            "[AccountingGrossPrice] money, " +
            "[RemainingQty] float, " +
            "[Weight] float, " +
            "[RowNumber] int " +
            "); " +
            ";WITH [RemainingConsignments_CTE] " +
            "AS ( " +
            "SELECT [Product].ID AS [ProductID] " +
            ", [ConsignmentItem].RemainingQty AS [RemainingQty] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ISNULL([Supplier].FullName, N'') AS [SupplierName] " +
            ", [ProductIncome].FromDate AS [FromDate] " +
            ", [ProductIncome].Number AS [ProductIncomeNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [SupplyInvoice].ID IS NOT NULL " +
            "THEN [SupplyInvoice].Number " +
            "WHEN [SupplyOrderUkraine].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraine].InvNumber " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].Number " +
            "WHEN [ProductCapitalization].ID IS NOT NULL " +
            "THEN [ProductCapitalization].Number " +
            "ELSE N'' " +
            "END " +
            ") AS [InvoiceNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [ConsignmentItem].NetPrice AS [NetPrice] " +
            ", [ConsignmentItem].Price AS [GrossPrice] " +
            ", [ConsignmentItem].AccountingPrice AS [AccountingGrossPrice] " +
            ", [ConsignmentItem].[Weight] AS [Weight] " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [ClientAgreement] AS [SupplyOrderUkraineClientAgreement] " +
            "ON [SupplyOrderUkraineClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = ( " +
            "CASE " +
            "WHEN [SupplyOrder].ID IS NOT NULL " +
            "THEN [SupplyOrder].ClientID " +
            "WHEN [SupplyOrderUkraineClientAgreement].ID IS NOT NULL " +
            "THEN [SupplyOrderUkraineClientAgreement].ClientID " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN [SaleReturn].ClientID " +
            "ELSE 0 " +
            "END " +
            ") " +
            "WHERE [ConsignmentItem].RemainingQty <> 0 " +
            "AND [ProductIncome].FromDate >= @From " +
            "AND [ProductIncome].FromDate <= @To " +
            (
                supplierNetId.HasValue
                    ? "AND [Supplier].NetUID = @SupplierNetId "
                    : string.Empty
            ) +
            (
                storageNetId.HasValue
                    ? "AND [Storage].NetUID = @StorageNetId "
                    : string.Empty
            ) +
            "), " +
            "[Grouped_CTE] " +
            "AS ( " +
            "SELECT [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[AccountingGrossPrice] " +
            ", SUM([RemainingItems].[RemainingQty]) AS [RemainingQty] " +
            ", [RemainingItems].[Weight] " +
            "FROM [RemainingConsignments_CTE] AS [RemainingItems] " +
            "GROUP BY [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", [RemainingItems].[NetPrice] " +
            ", [RemainingItems].[GrossPrice] " +
            ", [RemainingItems].[AccountingGrossPrice] " +
            ", [RemainingItems].[Weight] " +
            ") " +
            "INSERT INTO @RemainingItems " +
            "( " +
            "[FromDate] " +
            ", [StorageName] " +
            ", [SupplierName] " +
            ", [InvoiceNumber] " +
            ", [ProductIncomeNumber] " +
            ", [OrganizationName] " +
            ", [ProductID] " +
            ", [NetPrice] " +
            ", [GrossPrice] " +
            ", [AccountingGrossPrice] " +
            ", [RemainingQty] " +
            ", [Weight] " +
            ") " +
            "SELECT [RemainingItems].[FromDate] " +
            ", [RemainingItems].[StorageName] " +
            ", [RemainingItems].[SupplierName] " +
            ", [RemainingItems].[InvoiceNumber] " +
            ", [RemainingItems].[ProductIncomeNumber] " +
            ", [RemainingItems].[OrganizationName] " +
            ", [RemainingItems].[ProductID] " +
            ", ROUND([RemainingItems].[NetPrice] * [RemainingItems].[RemainingQty], 2) AS [NetPrice] " +
            ", ROUND([RemainingItems].[GrossPrice] * [RemainingItems].[RemainingQty], 2) AS [GrossPrice] " +
            ", ROUND([RemainingItems].[AccountingGrossPrice] * [RemainingItems].[RemainingQty], 2) AS [AccountingGrossPrice] " +
            ", [RemainingItems].[RemainingQty] " +
            ", ROUND([RemainingItems].[Weight] * [RemainingItems].[RemainingQty], 3) AS [Weight] " +
            "FROM [Grouped_CTE] AS [RemainingItems] " +
            ";WITH [GroupedRemainings_CTE] " +
            "AS ( " +
            "SELECT [RemainingItems].FromDate " +
            ", [RemainingItems].ProductIncomeNumber " +
            ", [RemainingItems].InvoiceNumber " +
            ", [RemainingItems].SupplierName " +
            ", [RemainingItems].OrganizationName " +
            ", SUM([RemainingItems].GrossPrice) AS [TotalGrossPrice] " +
            ", SUM([RemainingItems].AccountingGrossPrice) AS [AccountingTotalGrossPrice] " +
            ", SUM([RemainingItems].[Weight]) AS [TotalWeight] " +
            ", ROW_NUMBER() OVER(ORDER BY [RemainingItems].FromDate) AS [RowNumber] " +
            "FROM @RemainingItems AS [RemainingItems] " +
            "GROUP BY [RemainingItems].FromDate " +
            ", [RemainingItems].ProductIncomeNumber " +
            ", [RemainingItems].InvoiceNumber " +
            ", [RemainingItems].SupplierName " +
            ", [RemainingItems].OrganizationName " +
            ") " +
            "SELECT [GroupedItems].FromDate " +
            ", [GroupedItems].ProductIncomeNumber " +
            ", [GroupedItems].InvoiceNumber " +
            ", [GroupedItems].SupplierName " +
            ", [GroupedItems].OrganizationName " +
            ", [GroupedItems].TotalGrossPrice " +
            ", [GroupedItems].AccountingTotalGrossPrice " +
            ", [GroupedItems].TotalWeight " +
            ", [GroupedItems].[RowNumber] " +
            ", [RemainingItems].ProductID " +
            ", [RemainingItems].NetPrice " +
            ", [RemainingItems].GrossPrice " +
            ", [RemainingItems].AccountingGrossPrice " +
            ", [RemainingItems].RemainingQty " +
            ", [RemainingItems].FromDate " +
            ", [RemainingItems].[Weight] " +
            ", [Product].* " +
            "FROM [GroupedRemainings_CTE] AS [GroupedItems] " +
            "LEFT JOIN @RemainingItems AS [RemainingItems] " +
            "ON [RemainingItems].FromDate = [GroupedItems].FromDate " +
            "AND [RemainingItems].ProductIncomeNumber = [GroupedItems].ProductIncomeNumber " +
            "AND [RemainingItems].InvoiceNumber = [GroupedItems].InvoiceNumber " +
            "AND [RemainingItems].SupplierName = [GroupedItems].SupplierName " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [RemainingItems].ProductID " +
            "ORDER BY [GroupedItems].[FromDate]",
            (groupedConsignment, groupedConsignmentItem, product) => {
                if (groupedConsignments.Any(c => c.RowNumber.Equals(groupedConsignment.RowNumber)))
                    groupedConsignment = groupedConsignments.First(c => c.RowNumber.Equals(groupedConsignment.RowNumber));
                else
                    groupedConsignments.Add(groupedConsignment);

                if (groupedConsignmentItem == null) return groupedConsignment;

                groupedConsignmentItem.Product = product;

                groupedConsignment.GroupedConsignmentItems.Add(groupedConsignmentItem);

                return groupedConsignment;
            },
            new {
                StorageNetId = storageNetId ?? Guid.Empty,
                SupplierNetId = supplierNetId ?? Guid.Empty,
                From = from,
                To = to,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            },
            splitOn: "ProductID,ID"
        );

        return groupedConsignments;
    }
}