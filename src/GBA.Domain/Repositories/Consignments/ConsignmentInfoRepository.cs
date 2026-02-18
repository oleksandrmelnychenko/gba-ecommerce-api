using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Dapper;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.EntityHelpers.Consignments;
using GBA.Domain.Repositories.Consignments.Contracts;

namespace GBA.Domain.Repositories.Consignments;

public sealed class ConsignmentInfoRepository : IConsignmentInfoRepository {
    private readonly IDbConnection _connection;

    public ConsignmentInfoRepository(IDbConnection connection) {
        _connection = connection;
    }

    public IEnumerable<IncomeConsignmentInfo> GetIncomeConsignmentInfoFiltered(
        Guid productNetId,
        DateTime from,
        DateTime to) {
        return _connection.Query<IncomeConsignmentInfo>(
            ";WITH [IncomeConsignmentInfo_CTE] " +
            "AS ( " +
            "SELECT [Storage].[Name] AS [StorageName] " +
            ", [Client].[FullName] AS [SupplierName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [ProductIncome].[FromDate] AS [IncomeToStorageDate] " +
            ", [ProductIncome].[Number] AS [IncomeToStorageNumber] " +
            ", " +
            "(SELECT CONCAT_WS(' / ', [RootSupplyInvoice].Number, STRING_AGG([MergedSupplyInvoice].[Number], ' / ')) " +
            "FROM [SupplyInvoice] AS [MergedSupplyInvoice] " +
            "WHERE RootSupplyInvoiceID = [RootSupplyInvoice].ID) AS [IncomeInvoiceNumber] " +
            ", [RootSupplyInvoice].[DateFrom] AS [IncomeInvoiceDate] " +
            ", [ConsignmentItem].[NetPrice] AS [NetPrice] " +
            ", [ConsignmentItem].[NetPrice] * [ConsignmentItem].[Qty] AS [TotalNetPrice] " +
            ", [ConsignmentItem].[Price] * [ConsignmentItem].[Qty] AS [GrossPrice] " +
            ", [ConsignmentItem].[AccountingPrice] * [ConsignmentItem].[Qty] AS [AccountingGrossPrice] " +
            ", ROUND([ConsignmentItem].[Weight], 3) AS [Weight] " +
            ", [ConsignmentItem].[Qty] AS [IncomeQty] " +
            ", [ConsignmentItem].[RemainingQty] AS [RemainingQty] " +
            ", [ConsignmentItem].[ExchangeRate] AS [ExchangeRate] " +
            ", N'' AS [FromInvoiceNumber] " +
            ", NULL AS [FromInvoiceDate] " +
            ", NULL AS [ReturnPrice] " +
            ", NULL AS [PriceDifference] " +
            ", [PackingListPackageOrderItem].[UnitPrice] AS [UnitPriceLocal] " +
            ", [Currency].[Code] AS [Currency] " +
            ", [PackingListPackageOrderItem].[AccountingGrossUnitPriceEur] AS [AccountingEurUnitPrice] " +
            ", [PackingListPackageOrderItem].[GrossUnitPriceEur] + [PackingListPackageOrderItem].[AccountingGrossUnitPriceEur] AS [ManagementEurUnitPrice] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItemMovement].ConsignmentItemID = [ConsignmentItem].ID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [SupplyInvoice] AS [RootSupplyInvoice] " +
            "ON [RootSupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [RootSupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "WHERE [ConsignmentItemMovement].MovementType = 3 " +
            "AND [ConsignmentItemMovement].Deleted = 0 " +
            "AND [Product].NetUID = @ProductNetId " +
            "AND [ConsignmentItem].RemainingQty != 0 " +
            "AND [Consignment].FromDate >= @From " +
            "AND [Consignment].FromDate <= @To " +
            "UNION ALL " +
            "SELECT [Storage].[Name] AS [StorageName] " +
            ", [Client].[FullName] AS [SupplierName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [ProductIncome].[FromDate] AS [IncomeToStorageDate] " +
            ", [ProductIncome].[Number] AS [IncomeToStorageNumber] " +
            ", [SupplyOrderUkraine].[InvNumber] AS [IncomeInvoiceNumber] " +
            ", [SupplyOrderUkraine].[InvDate] AS [IncomeInvoiceDate] " +
            ", [ConsignmentItem].[NetPrice] AS [NetPrice] " +
            ", [ConsignmentItem].[NetPrice] * [ConsignmentItem].[Qty] AS [TotalNetPrice] " +
            ", [ConsignmentItem].[Price] * [ConsignmentItem].[Qty] + [DeliveryExpense].GrossAmount AS [GrossPrice] " +
            ", [ConsignmentItem].[AccountingPrice] * [ConsignmentItem].[Qty] + [DeliveryExpense].AccountingGrossAmount AS [AccountingGrossPrice] " +
            ", ROUND([ConsignmentItem].[Weight], 3) AS [Weight] " +
            ", [ConsignmentItem].[Qty] AS [IncomeQty] " +
            ", [ConsignmentItem].[RemainingQty] AS [RemainingQty] " +
            ", [ConsignmentItem].[ExchangeRate] AS [ExchangeRate] " +
            ", ISNULL([SupplyInvoice].[Number], N'') AS [FromInvoiceNumber] " +
            ", [SupplyInvoice].[DateFrom] AS [FromInvoiceDate] " +
            ", NULL AS [ReturnPrice] " +
            ", NULL AS [PriceDifference] " +
            ", [SupplyOrderUkraineItem].[UnitPriceLocal] AS [UnitPriceLocal] " +
            ", [Currency].[Code] AS [Currency] " +
            ", CASE WHEN [PackingListPackageOrderItem].[GrossUnitPriceEur] IS NOT NULL " +
            "THEN [PackingListPackageOrderItem].[GrossUnitPriceEur] + [PackingListPackageOrderItem].[AccountingGrossUnitPriceEur] " +
            "ELSE [ConsignmentItem].[NetPrice] END [ManagementEurUnitPrice] " +
            ", CASE WHEN [PackingListPackageOrderItem].[AccountingGrossUnitPriceEur] IS NOT NULL " +
            "THEN [PackingListPackageOrderItem].[AccountingGrossUnitPriceEur] " +
            "ELSE [ConsignmentItem].[NetPrice] END [AccountingEurUnitPrice] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItemMovement].ConsignmentItemID = [ConsignmentItem].ID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [ConsignmentItem] AS [PlConsignmentItem] " +
            "ON [PlConsignmentItem].ID = [SupplyOrderUkraineItem].ConsignmentItemID " +
            "LEFT JOIN [ProductIncomeItem] AS [PlProductIncomeItem] " +
            "ON [PlProductIncomeItem].ID = [PlConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [DeliveryExpense] " +
            "ON [DeliveryExpense].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
            "WHERE [ConsignmentItemMovement].MovementType = 4 " +
            "AND [ConsignmentItemMovement].Deleted = 0 " +
            "AND [Product].NetUID = @ProductNetId " +
            "AND [ConsignmentItem].RemainingQty != 0 " +
            "AND [Consignment].FromDate >= @From " +
            "AND [Consignment].FromDate <= @To " +
            "UNION ALL " +
            "SELECT [Storage].[Name] AS [StorageName] " +
            ", [Client].FullName AS [SupplierName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [ProductTransfer].[FromDate] AS [IncomeToStorageDate] " +
            ", [ProductTransfer].[Number] AS [IncomeToStorageNumber] " +
            ", N'' AS [IncomeInvoiceNumber] " +
            ", NULL AS [IncomeInvoiceDate] " +
            ", [ConsignmentItem].[NetPrice] AS [NetPrice] " +
            ", [ConsignmentItem].[NetPrice] * [ConsignmentItem].[Qty] AS [TotalNetPrice] " +
            ", [ConsignmentItem].[Price] * [ConsignmentItem].[Qty] AS [GrossPrice] " +
            ", [ConsignmentItem].[AccountingPrice] * [ConsignmentItem].[Qty] AS [AccountingGrossPrice] " +
            ", ROUND([ConsignmentItem].[Weight], 3) AS [Weight] " +
            ", [ConsignmentItem].[Qty] AS [IncomeQty] " +
            ", [ConsignmentItem].[RemainingQty] AS [RemainingQty] " +
            ", [ConsignmentItem].[ExchangeRate] AS [ExchangeRate] " +
            ", [ProductIncome].[Number] AS [FromInvoiceNumber] " +
            ", [ProductIncome].[FromDate] AS [FromInvoiceDate] " +
            ", NULL AS [ReturnPrice] " +
            ", NULL AS [PriceDifference] " +
            ", CASE " +
            "WHEN [SupplyOrderUkraineItem].[UnitPriceLocal] IS NOT NULL " +
            "THEN [SupplyOrderUkraineItem].[UnitPriceLocal] " +
            "ELSE [PackingListPackageOrderItem].[UnitPrice] " +
            "END AS [UnitPriceLocal] " +
            ", CASE " +
            "WHEN [CurrencySupplyOrder].[ID] IS NOT NULL " +
            "THEN [CurrencySupplyOrder].[Code] " +
            "ELSE [Currency].[Code] " +
            "END AS [Currency] " +
            ", CASE WHEN [PackingListPackageOrderItem].[AccountingGrossUnitPriceEur] IS NOT NULL " +
            "THEN [PackingListPackageOrderItem].[AccountingGrossUnitPriceEur] " +
            "ELSE [ConsignmentItem].[NetPrice] END [AccountingEurUnitPrice] " +
            ", CASE WHEN [PackingListPackageOrderItem].[GrossUnitPriceEur] IS NOT NULL " +
            "THEN [PackingListPackageOrderItem].[GrossUnitPriceEur] + [PackingListPackageOrderItem].[AccountingGrossUnitPriceEur] " +
            "ELSE [ConsignmentItem].[NetPrice] END [ManagementEurUnitPrice] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItemMovement].ConsignmentItemID = [ConsignmentItem].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ProductTransfer] " +
            "ON [ProductTransfer].ID = [Consignment].ProductTransferID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [ConsignmentItem] AS [RootConsignmentItem] " +
            "ON [RootConsignmentItem].ID = [ConsignmentItem].RootConsignmentItemID " +
            "LEFT JOIN [ProductIncomeItem] AS [RootProductIncomeItem] " +
            "ON [RootProductIncomeItem].ID = [RootConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [RootProductIncomeItem].SupplyOrderUkraineItemID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [RootProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [ClientAgreement] AS [ClientAgreementSupplyOrder] " +
            "ON [ClientAgreementSupplyOrder].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] AS [AgreementSupplyOrder] " +
            "ON [AgreementSupplyOrder].[ID] = [ClientAgreementSupplyOrder].[AgreementID] " +
            "LEFT JOIN [Currency] AS [CurrencySupplyOrder] " +
            "ON [CurrencySupplyOrder].[ID] = [AgreementSupplyOrder].[CurrencyID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = ( " +
            "CASE " +
            "WHEN [SupplyOrder].ClientID IS NOT NULL " +
            "THEN [SupplyOrder].ClientID " +
            "ELSE [ClientAgreement].ClientID " +
            "END " +
            ") " +
            "WHERE [ConsignmentItemMovement].MovementType = 7 " +
            "AND [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].IsIncomeMovement = 1 " +
            "AND [Product].NetUID = @ProductNetId " +
            "AND [ConsignmentItem].RemainingQty != 0 " +
            "AND [Consignment].FromDate >= @From " +
            "AND [Consignment].FromDate <= @To " +
            "UNION ALL " +
            "SELECT [Storage].[Name] AS [StorageName] " +
            ", [Client].FullName AS [SupplierName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [SaleReturn].[FromDate] AS [IncomeToStorageDate] " +
            ", [SaleReturn].[Number] AS [IncomeToStorageNumber] " +
            ", N'' AS [IncomeInvoiceNumber] " +
            ", NULL AS [IncomeInvoiceDate] " +
            ", [ConsignmentItem].[NetPrice] AS [NetPrice] " +
            ", [ConsignmentItem].[NetPrice] * [ConsignmentItem].[Qty] AS [TotalNetPrice] " +
            ", [ConsignmentItem].[Price] * [ConsignmentItem].[Qty] AS [GrossPrice] " +
            ", [ConsignmentItem].[AccountingPrice] * [ConsignmentItem].[Qty] AS [AccountingGrossPrice] " +
            ", ROUND([ConsignmentItem].[Weight], 3) AS [Weight] " +
            ", [ConsignmentItem].[Qty] AS [IncomeQty] " +
            ", [ConsignmentItem].[RemainingQty] AS [RemainingQty] " +
            ", [ConsignmentItem].[ExchangeRate] AS [ExchangeRate] " +
            ", [ProductIncome].[Number] AS [FromInvoiceNumber] " +
            ", [ProductIncome].[FromDate] AS [FromInvoiceDate] " +
            ", CAST([SaleReturnItem].Amount / [SaleReturnItem].Qty AS money) AS [ReturnPrice] " +
            ", CAST([SaleReturnItem].Amount / [SaleReturnItem].Qty - [ConsignmentItem].Price AS money) AS [PriceDifference] " +
            ", CASE " +
            "WHEN [SupplyOrderUkraineItem].[UnitPriceLocal] IS NOT NULL " +
            "THEN [SupplyOrderUkraineItem].[UnitPriceLocal] " +
            "ELSE [PackingListPackageOrderItem].[UnitPrice] " +
            "END AS [UnitPriceLocal] " +
            ", CASE " +
            "WHEN [CurrencySupplyOrder].[ID] IS NOT NULL " +
            "THEN [CurrencySupplyOrder].[Code] " +
            "ELSE [Currency].[Code] " +
            "END AS [Currency] " +
            ", CASE WHEN [PackingListPackageOrderItem].[GrossUnitPriceEur] IS NOT NULL " +
            "THEN [PackingListPackageOrderItem].[GrossUnitPriceEur] + [PackingListPackageOrderItem].[AccountingGrossUnitPriceEur] " +
            "ELSE [ConsignmentItem].[NetPrice] END AS [ManagementEurUnitPrice] " +
            ", CASE WHEN [PackingListPackageOrderItem].[AccountingGrossUnitPriceEur] IS NOT NULL " +
            "THEN [PackingListPackageOrderItem].[AccountingGrossUnitPriceEur] " +
            "ELSE [ConsignmentItem].[NetPrice] END AS [AccountingEurUnitPrice] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItemMovement].ConsignmentItemID = [ConsignmentItem].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [ConsignmentItem] AS [RootConsignmentItem] " +
            "ON [RootConsignmentItem].ID = [ConsignmentItem].RootConsignmentItemID " +
            "LEFT JOIN [ProductIncomeItem] AS [RootProductIncomeItem] " +
            "ON [RootProductIncomeItem].ID = [RootConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [RootProductIncomeItem].SupplyOrderUkraineItemID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [RootProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [ClientAgreement] AS [ClientAgreementSupplyOrder] " +
            "ON [ClientAgreementSupplyOrder].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] AS [AgreementSupplyOrder] " +
            "ON [AgreementSupplyOrder].[ID] = [ClientAgreementSupplyOrder].[AgreementID] " +
            "LEFT JOIN [Currency] AS [CurrencySupplyOrder] " +
            "ON [CurrencySupplyOrder].[ID] = [AgreementSupplyOrder].[CurrencyID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = ( " +
            "CASE " +
            "WHEN [SupplyOrder].ClientID IS NOT NULL " +
            "THEN [SupplyOrder].ClientID " +
            "ELSE [ClientAgreement].ClientID " +
            "END " +
            ") " +
            "WHERE [ConsignmentItemMovement].MovementType = 1 " +
            "AND [ConsignmentItemMovement].Deleted = 0 " +
            "AND [Product].NetUID = @ProductNetId " +
            "AND [ConsignmentItem].RemainingQty != 0 " +
            "AND [Consignment].FromDate >= @From " +
            "AND [Consignment].FromDate <= @To " +
            "AND [SaleReturn].[IsCanceled] = 0 " +
            "UNION ALL " +
            "SELECT [Storage].[Name] AS [StorageName] " +
            ", N'' AS [SupplierName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [ProductIncome].[FromDate] AS [IncomeToStorageDate] " +
            ", [ProductIncome].[Number] AS [IncomeToStorageNumber] " +
            ", [ProductCapitalization].[Number] AS [IncomeInvoiceNumber] " +
            ", [ProductCapitalization].[FromDate] AS [IncomeInvoiceDate] " +
            ", [ConsignmentItem].[NetPrice] AS [NetPrice] " +
            ", [ConsignmentItem].[NetPrice] * [ConsignmentItem].[Qty] AS [TotalNetPrice] " +
            ", [ConsignmentItem].[Price] * [ConsignmentItem].[Qty] AS [GrossPrice] " +
            ", [ConsignmentItem].[AccountingPrice] * [ConsignmentItem].[Qty] AS [AccountingGrossPrice] " +
            ", ROUND([ConsignmentItem].[Weight], 3) AS [Weight] " +
            ", [ConsignmentItem].[Qty] AS [IncomeQty] " +
            ", [ConsignmentItem].[RemainingQty] AS [RemainingQty] " +
            ", [ConsignmentItem].[ExchangeRate] AS [ExchangeRate] " +
            ", N'' AS [FromInvoiceNumber] " +
            ", NULL AS [FromInvoiceDate] " +
            ", NULL AS [ReturnPrice] " +
            ", NULL AS [PriceDifference] " +
            ", [ProductCapitalizationItem].[UnitPrice] AS [UnitPriceLocal] " +
            ", 'EUR' AS [Currency] " +
            ", [ConsignmentItem].[NetPrice] AS [AccountingEurUnitPrice] " +
            ", [ConsignmentItem].[NetPrice] AS [ManagementEurUnitPrice] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItemMovement].ConsignmentItemID = [ConsignmentItem].ID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "WHERE [ConsignmentItemMovement].MovementType = 11 " +
            "AND [ConsignmentItemMovement].Deleted = 0 " +
            "AND [Product].NetUID = @ProductNetId " +
            "AND [ConsignmentItem].RemainingQty != 0 " +
            "AND [Consignment].FromDate >= @From " +
            "AND [Consignment].FromDate <= @To " +
            ") " +
            "SELECT " +
            "[Infos].[StorageName] " +
            ", [Infos].[SupplierName] " +
            ", [Infos].[OrganizationName] " +
            ", [Infos].[IncomeToStorageDate] " +
            ", [Infos].[IncomeToStorageNumber] " +
            ", [Infos].[IncomeInvoiceNumber] " +
            ", [Infos].[IncomeInvoiceDate] " +
            ", [Infos].[NetPrice] " +
            ", SUM([Infos].[TotalNetPrice]) AS [TotalNetPrice] " +
            ", SUM([Infos].[GrossPrice]) AS [GrossPrice] " +
            ", SUM([Infos].[AccountingGrossPrice]) AS [AccountingGrossPrice] " +
            ", [Infos].[Weight] " +
            ", SUM([Infos].[IncomeQty]) AS [IncomeQty] " +
            ", SUM([Infos].[RemainingQty]) AS [RemainingQty] " +
            ", [Infos].[FromInvoiceNumber] " +
            ", [Infos].[FromInvoiceDate] " +
            ", [Infos].[ReturnPrice] " +
            ", [Infos].[PriceDifference] " +
            ", [Infos].[UnitPriceLocal] " +
            ", [Infos].[Currency] " +
            ", [Infos].[ExchangeRate] " +
            ", [Infos].[AccountingEurUnitPrice] " +
            ", [Infos].[ManagementEurUnitPrice] " +
            "FROM [IncomeConsignmentInfo_CTE] AS [Infos] " +
            "GROUP BY [Infos].[IncomeToStorageNumber] " +
            ", [Infos].[IncomeToStorageDate] " +
            ", [Infos].[StorageName] " +
            ", [Infos].[SupplierName] " +
            ", [Infos].[OrganizationName] " +
            ", [Infos].[IncomeInvoiceNumber] " +
            ", [Infos].[IncomeInvoiceDate] " +
            ", [Infos].[NetPrice] " +
            ", [Infos].[RemainingQty]" +
            ", [Infos].[Weight] " +
            ", [Infos].[FromInvoiceNumber] " +
            ", [Infos].[FromInvoiceDate] " +
            ", [Infos].[ReturnPrice] " +
            ", [Infos].[PriceDifference] " +
            ", [Infos].[UnitPriceLocal] " +
            ", [Infos].[ExchangeRate] " +
            ", [Infos].[Currency] " +
            ", [Infos].[AccountingEurUnitPrice] " +
            ", [Infos].[ManagementEurUnitPrice] " +
            "ORDER BY (CASE WHEN SUM([Infos].[RemainingQty]) <> 0 THEN 0 ELSE 1 END) " +
            ", [Infos].[RemainingQty] DESC ",
            //", [Infos].[IncomeToStorageDate] DESC ",
            new {
                ProductNetId = productNetId,
                From = from,
                To = to,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );
    }

    public Tuple<IEnumerable<ConsignmentAvailabilityItem>, int> GetConsignmentAvailabilityFiltered(
        Guid storageNetId,
        DateTime from,
        DateTime to,
        string vendorCode,
        int limit,
        int offset) {
        List<ConsignmentAvailabilityItem> toReturn = new();

        int totalQty = default;

        _connection.Query<ConsignmentAvailabilityItem, ProductPlacement, int, ConsignmentAvailabilityItem>(
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [Consignment].FromDate) AS RowNumber " +
            ", [ConsignmentItem].[ID] AS [ID] " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "WHERE [ConsignmentItem].[Deleted] = 0 " +
            "AND [ConsignmentItem].RemainingQty > 0 " +
            "AND [Product].[VendorCode] LIKE '%' + @VendorCode + '%' " +
            "AND [Storage].[NetUID] = @StorageNetId " +
            "AND [Consignment].FromDate >= @From " +
            "AND [Consignment].FromDate <= @To " +
            ") " +
            "SELECT " +
            " [ConsignmentItem].[ID] AS [ID] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", [Storage].[NetUID] AS [StorageNetId] " +
            ", [Storage].[ID] AS [StorageId] " +
            ", [Product].[ID] AS [ProductId] " +
            ", [Product].[NetUID] AS [ProductNetId] " +
            ", [Product].[Name] AS [ProductName] " +
            ", [Product].[VendorCode] AS [VendorCode] " +
            ", [ConsignmentItem].[Qty] AS [IncomeQty] " +
            ", [ConsignmentItem].[RemainingQty] AS [Qty] " +
            ", ROUND([ConsignmentItem].[ExchangeRate], 4) AS [ExchangeRate] " +
            ", [Consignment].[FromDate] AS [FromDate] " +
            ", ROUND([ConsignmentItem].[NetPrice], 2) AS [NetPrice] " +
            ", ROUND([ConsignmentItem].[NetPrice] * [ConsignmentItem].[Qty], 2) AS [TotalNetPrice] " +
            ", ROUND([ConsignmentItem].[AccountingPrice], 2) AS [UnitAccountingGrossPrice] " +
            ", ROUND([ConsignmentItem].[Price], 2) AS [UnitGrossPrice] " +
            ", ROUND([ConsignmentItem].[Price] * [ConsignmentItem].[Qty], 2) AS [GrossPrice] " +
            ", ROUND([ConsignmentItem].[AccountingPrice] * [ConsignmentItem].[Qty], 2) AS [AccountingGrossPrice] " +
            ", [ProductPlacement].* " +
            ", (SELECT COUNT(1) FROM [Search_CTE]) AS [Total] " +
            "FROM [Search_CTE] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [Search_CTE].[ID] = [ConsignmentItem].[ID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].[ProductID] = [Product].[ID] " +
            "AND [ProductPlacement].[StorageID] = [Storage].[ID] " +
            "AND [ProductPlacement].[Deleted] = 0 " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset ",
            (availability, placement, total) => {
                if (toReturn.Any(x => x.Id.Equals(availability.Id)))
                    availability = toReturn.First(x => x.Id == availability.Id);
                else
                    toReturn.Add(availability);

                availability.Placements.Add(placement);

                totalQty = total;

                return availability;
            },
            new {
                VendorCode = vendorCode ?? string.Empty,
                From = from,
                To = to,
                StorageNetId = storageNetId,
                Limit = limit,
                Offset = offset
            },
            splitOn: "ConsignmentItemId,ID,Total"
        );

        return new Tuple<IEnumerable<ConsignmentAvailabilityItem>, int>(toReturn, totalQty);
    }

    public IEnumerable<ConsignmentAvailabilityItem> GetAllConsignmentAvailabilityFiltered(
        Guid storageNetId,
        DateTime from,
        DateTime to,
        string vendorCode) {
        List<ConsignmentAvailabilityItem> toReturn = new();

        _connection.Query<ConsignmentAvailabilityItem, ProductPlacement, ConsignmentAvailabilityItem>(
            "SELECT " +
            " [ConsignmentItem].[ID] AS [ID] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", [Storage].[NetUID] AS [StorageNetId] " +
            ", [Storage].[ID] AS [StorageId] " +
            ", [Product].[ID] AS [ProductId] " +
            ", [Product].[NetUID] AS [ProductNetId] " +
            ", [Product].[Name] AS [ProductName] " +
            ", [Product].[VendorCode] AS [VendorCode] " +
            ", [ConsignmentItem].[Qty] AS [IncomeQty] " +
            ", [ConsignmentItem].[RemainingQty] AS [Qty] " +
            ", ROUND([ConsignmentItem].[ExchangeRate], 4) AS [ExchangeRate] " +
            ", [Consignment].[FromDate] AS [FromDate] " +
            ", ROUND([ConsignmentItem].[NetPrice], 2) AS [NetPrice] " +
            ", ROUND([ConsignmentItem].[NetPrice] * [ConsignmentItem].[Qty], 2) AS [TotalNetPrice] " +
            ", ROUND([ConsignmentItem].[AccountingPrice], 2) AS [UnitAccountingGrossPrice] " +
            ", ROUND([ConsignmentItem].[Price], 2) AS [UnitGrossPrice] " +
            ", ROUND([ConsignmentItem].[Price] * [ConsignmentItem].[Qty], 2) AS [GrossPrice] " +
            ", ROUND([ConsignmentItem].[AccountingPrice] * [ConsignmentItem].[Qty], 2) AS [AccountingGrossPrice] " +
            ", [ProductPlacement].* " +
            "FROM [ConsignmentItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].[ProductID] = [Product].[ID] " +
            "AND [ProductPlacement].[StorageID] = [Storage].[ID] " +
            "AND [ProductPlacement].[Deleted] = 0 " +
            "AND [ProductPlacement].[Qty] != 0 " +
            "WHERE [ConsignmentItem].[Deleted] = 0 " +
            "AND [ConsignmentItem].RemainingQty > 0 " +
            "AND [Product].[VendorCode] LIKE '%' + @VendorCode + '%' " +
            "AND [Consignment].FromDate >= @From " +
            "AND [Consignment].FromDate <= @To " +
            "AND [Storage].[NetUID] = @StorageNetId ",
            (availability, placement) => {
                if (toReturn.Any(x => x.Id.Equals(availability.Id)))
                    availability = toReturn.First(x => x.Id == availability.Id);
                else
                    toReturn.Add(availability);

                availability.Placements.Add(placement);

                return availability;
            },
            new {
                VendorCode = vendorCode ?? string.Empty,
                From = from,
                To = to,
                StorageNetId = storageNetId
            }
        );

        return toReturn;
    }

    public IEnumerable<OutcomeConsignmentInfo> GetOutcomeConsignmentInfoFiltered(
        Guid productNetId,
        DateTime from,
        DateTime to) {
        return _connection.Query<OutcomeConsignmentInfo>(
            ";WITH [OutcomeConsignmentInfo_CTE] " +
            "AS ( " +
            "SELECT ISNULL([Sale].ChangedToInvoice, [Sale].Updated) AS [FromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentTypeName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [SaleNumber].[Value] AS [DocumentNumber] " +
            ", [Client].FullName AS [ClientName] " +
            ", [User].LastName AS [ResponsibleName] " +
            ", [OrderItem].PricePerItem AS [Price] " +
            ", [ConsignmentItemMovement].Qty AS [Qty] " +
            ", (  " +
            "CASE  " +
            "WHEN EXISTS (SELECT 1 " +
            "FROM [HistoryInvoiceEdit] " +
            "WHERE [HistoryInvoiceEdit].SaleId = [Sale].ID) " +
            "THEN 1  " +
            "ELSE 0 " +
            "END " +
            ") AS [HasUpdateDataCarrier] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 0 " +
            ") " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [ConsignmentItemMovement].OrderItemID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [OrderItem].OrderID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = ( " +
            "CASE " +
            "WHEN [Sale].ChangedToInvoiceByID IS NOT NULL " +
            "THEN [Sale].ChangedToInvoiceByID " +
            "ELSE [Sale].UserID " +
            "END " +
            ") " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 0 " +
            "AND [Product].NetUID = @ProductNetId " +
            "AND [ConsignmentItemMovement].OrderItemId IS NOT NULL " +
            "UNION ALL " +
            "SELECT [OrderItemBaseShiftStatus].Updated AS [FromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentTypeName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [SaleNumber].[Value] AS [DocumentNumber] " +
            ", [Client].FullName AS [ClientName] " +
            ", [User].LastName AS [ResponsibleName] " +
            ", [OrderItem].PricePerItem AS [Price] " +
            ", [ConsignmentItemMovement].Qty AS [Qty] " +
            ", (  " +
            "CASE  " +
            "WHEN EXISTS (SELECT 1 " +
            "FROM [HistoryInvoiceEdit] " +
            "WHERE [HistoryInvoiceEdit].SaleId = [Sale].ID) " +
            "THEN 1  " +
            "ELSE 0 " +
            "END " +
            ") AS [HasUpdateDataCarrier] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 2 " +
            ") " +
            "LEFT JOIN [OrderItemBaseShiftStatus] " +
            "ON [OrderItemBaseShiftStatus].ID = [ConsignmentItemMovement].OrderItemBaseShiftStatusID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [OrderItemBaseShiftStatus].OrderItemID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [OrderItem].OrderID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OrderItemBaseShiftStatus].UserID " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 2 " +
            "AND [Product].NetUID = @ProductNetId " +
            "UNION ALL " +
            "SELECT [OrderItemBaseShiftStatus].Updated AS [FromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentTypeName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [SaleNumber].[Value] AS [DocumentNumber] " +
            ", [Client].FullName AS [ClientName] " +
            ", [User].LastName AS [ResponsibleName] " +
            ", [OrderItem].PricePerItem AS [Price] " +
            ", [ConsignmentItemMovement].Qty AS [Qty] " +
            ", (  " +
            "CASE  " +
            "WHEN EXISTS (SELECT 1 " +
            "FROM [HistoryInvoiceEdit] " +
            "WHERE [HistoryInvoiceEdit].SaleId = [Sale].ID) " +
            "THEN 1  " +
            "ELSE 0 " +
            "END " +
            ") AS [HasUpdateDataCarrier] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 12 " +
            ") " +
            "LEFT JOIN [OrderItemBaseShiftStatus] " +
            "ON [OrderItemBaseShiftStatus].ID = [ConsignmentItemMovement].OrderItemBaseShiftStatusID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [OrderItemBaseShiftStatus].OrderItemID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [OrderItem].OrderID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OrderItemBaseShiftStatus].UserID " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 12 " +
            "AND [Product].NetUID = @ProductNetId " +
            "UNION ALL " +
            "SELECT [DepreciatedOrder].FromDate AS [FromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentTypeName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [DepreciatedOrder].Number AS [DocumentNumber] " +
            ", N'' AS [ClientName] " +
            ", [User].LastName AS [ResponsibleName] " +
            ", [ConsignmentItem].Price AS [Price] " +
            ", [ConsignmentItemMovement].Qty AS [Qty] " +
            ", 0 AS [HasUpdateDataCarrier] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 5 " +
            ") " +
            "LEFT JOIN [DepreciatedOrderItem] " +
            "ON [DepreciatedOrderItem].ID = [ConsignmentItemMovement].DepreciatedOrderItemID " +
            "LEFT JOIN [DepreciatedOrder] " +
            "ON [DepreciatedOrder].ID = [DepreciatedOrderItem].DepreciatedOrderID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [DepreciatedOrder].ResponsibleID " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 5 " +
            "AND [Product].NetUID = @ProductNetId " +
            "UNION ALL " +
            "SELECT [SupplyReturn].FromDate AS [FromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentTypeName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [SupplyReturn].Number AS [DocumentNumber] " +
            ", [Client].FullName AS [ClientName] " +
            ", [User].LastName AS [ResponsibleName] " +
            ", [ConsignmentItem].Price AS [Price] " +
            ", [ConsignmentItemMovement].Qty AS [Qty] " +
            ", 0 AS [HasUpdateDataCarrier] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 6 " +
            ") " +
            "LEFT JOIN [SupplyReturnItem] " +
            "ON [SupplyReturnItem].ID = [ConsignmentItemMovement].SupplyReturnItemID " +
            "LEFT JOIN [SupplyReturn] " +
            "ON [SupplyReturn].ID = [SupplyReturnItem].SupplyReturnID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [SupplyReturn].ResponsibleID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyReturn].SupplierID " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 6 " +
            "AND [Product].NetUID = @ProductNetId " +
            "UNION ALL " +
            "SELECT [ProductTransfer].FromDate AS [FromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentTypeName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [ProductTransfer].Number AS [DocumentNumber] " +
            ", N'' AS [ClientName] " +
            ", [User].LastName AS [ResponsibleName] " +
            ", [ConsignmentItem].Price AS [Price] " +
            ", [ConsignmentItemMovement].Qty AS [Qty] " +
            ", 0 AS [HasUpdateDataCarrier]" +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 7 " +
            ") " +
            "LEFT JOIN [ProductTransferItem] " +
            "ON [ProductTransferItem].ID = [ConsignmentItemMovement].ProductTransferItemID " +
            "LEFT JOIN [ProductTransfer] " +
            "ON [ProductTransfer].ID = [ProductTransferItem].ProductTransferID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductTransfer].ResponsibleID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductTransfer].FromStorageID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Storage].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 7 " +
            "AND [ConsignmentItemMovement].IsIncomeMovement = 0 " +
            "AND [Product].NetUID = @ProductNetId " +
            "UNION ALL " +
            "SELECT [Sad].FromDate AS [FromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentTypeName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [Sad].Number AS [DocumentNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [Sad].ClientID IS NOT NULL " +
            "THEN [Client].FullName " +
            "ELSE [OrganizationClient].FullName " +
            "END " +
            ") AS [ClientName] " +
            ", [User].LastName AS [ResponsibleName] " +
            ", ( " +
            "CASE " +
            "WHEN [SupplyOrderUkraineCartItem].ID IS NOT NULL " +
            "THEN ROUND([SupplyOrderUkraineCartItem].UnitPrice + [SupplyOrderUkraineCartItem].UnitPrice * [Sad].MarginAmount / 100, 2) " +
            "ELSE ROUND([SadItem].UnitPrice + [SadItem].UnitPrice * [Sad].MarginAmount / 100, 2) " +
            "END " +
            ") AS [Price] " +
            ", [ConsignmentItemMovement].Qty AS [Qty]" +
            ", 0 AS [HasUpdateDataCarrier] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 8 " +
            ") " +
            "LEFT JOIN [SadItem] " +
            "ON [SadItem].ID = [ConsignmentItemMovement].SadItemID " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
            "LEFT JOIN [Sad] " +
            "ON [Sad].ID = [SadItem].SadID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [Sad].ResponsibleID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [Sad].ClientID " +
            "LEFT JOIN [OrganizationClient] " +
            "ON [OrganizationClient].ID = [Sad].OrganizationClientID " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 8 " +
            "AND [Product].NetUID = @ProductNetId " +
            "UNION ALL " +
            "SELECT [TaxFreePackList].FromDate AS [FromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentTypeName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [TaxFree].Number AS [DocumentNumber] " +
            ", [Client].FullName AS [ClientName] " +
            ", [User].LastName AS [ResponsibleName] " +
            ", ( " +
            "CASE " +
            "WHEN [SupplyOrderUkraineCartItem].ID IS NOT NULL " +
            "THEN ROUND([SupplyOrderUkraineCartItem].UnitPrice + [SupplyOrderUkraineCartItem].UnitPrice * [TaxFreePackList].MarginAmount / 100, 2) " +
            "ELSE [OrderItem].PricePerItem " +
            "END " +
            ") AS [Price] " +
            ", [ConsignmentItemMovement].Qty AS [Qty]" +
            ", 0 AS [HasUpdateDataCarrier] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 0 " +
            ") " +
            "LEFT JOIN [TaxFreeItem] " +
            "ON [TaxFreeItem].ID = [ConsignmentItemMovement].TaxFreeItemID " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [TaxFreeItem].SupplyOrderUkraineCartItemID " +
            "LEFT JOIN [TaxFreePackListOrderItem] " +
            "ON [TaxFreePackListOrderItem].ID = [TaxFreeItem].TaxFreePackListOrderItemID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [TaxFreePackListOrderItem].OrderItemID " +
            "LEFT JOIN [TaxFree] " +
            "ON [TaxFree].ID = [TaxFreeItem].TaxFreeID " +
            "LEFT JOIN [TaxFreePackList] " +
            "ON [TaxFreePackList].ID = [TaxFree].TaxFreePackListID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [TaxFreePackList].ClientID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [TaxFree].ResponsibleID " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 9 " +
            "AND [Product].NetUID = @ProductNetId " +
            "UNION ALL " +
            "SELECT [SupplyOrderUkraineCartItem].FromDate AS [FromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentTypeName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", N'' AS [DocumentNumber] " +
            ", N'' AS [ClientName] " +
            ", [User].LastName AS [ResponsibleName] " +
            ", 0 AS [Price] " +
            ", [SupplyOrderUkraineCartItemReservation].Qty AS [Qty] " +
            ", 0 AS [HasUpdateDataCarrier]" +
            "FROM [SupplyOrderUkraineCartItem] " +
            "LEFT JOIN [SadItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 10 " +
            ") " +
            "LEFT JOIN [SupplyOrderUkraineCartItemReservation] " +
            "ON [SupplyOrderUkraineCartItemReservation].SupplyOrderUkraineCartItemID = [SupplyOrderUkraineCartItem].ID " +
            "AND [SupplyOrderUkraineCartItemReservation].Deleted = 0 " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = [SupplyOrderUkraineCartItemReservation].ProductAvailabilityID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Storage].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = ( " +
            "CASE " +
            "WHEN [SupplyOrderUkraineCartItem].ResponsibleID IS NOT NULL " +
            "THEN [SupplyOrderUkraineCartItem].ResponsibleID " +
            "WHEN [SupplyOrderUkraineCartItem].UpdatedByID IS NOT NULL " +
            "THEN [SupplyOrderUkraineCartItem].UpdatedByID " +
            "ELSE [SupplyOrderUkraineCartItem].CreatedByID " +
            "END " +
            ") " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyOrderUkraineCartItem].ProductID " +
            "WHERE [SupplyOrderUkraineCartItem].Deleted = 0 " +
            "AND [SupplyOrderUkraineCartItem].TaxFreePackListID IS NULL " +
            "AND [SadItem].ID IS NULL " +
            "AND [Product].NetUID = @ProductNetId " +
            "UNION ALL " +
            "SELECT ISNULL([ReSale].ChangedToInvoice, [ReSale].Updated) AS [FromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentTypeName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [SaleNumber].[Value] AS [DocumentNumber] " +
            ", [Client].FullName AS [ClientName] " +
            ", [User].LastName AS [ResponsibleName] " +
            ", [ReSaleItem].PricePerItem / " +
            " [dbo].GetExchangeRateByCurrencyIdAndCode(NULL, 'EUR' " +
            ", ISNULL([ReSale].[ChangedToInvoice], [ReSale].[Updated])) " +
            " AS [Price] " +
            ", SUM([ConsignmentItemMovement].Qty) AS [Qty] " +
            ", 0 AS [HasUpdateDataCarrier]" +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ReSaleItem] " +
            "ON [ReSaleItem].ID = [ConsignmentItemMovement].[ReSaleItemID] " +
            "LEFT JOIN [ReSaleAvailability] " +
            "ON [ReSaleAvailability].ID = [ReSaleItem].[ReSaleAvailabilityID] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 0 " +
            ") " +
            "LEFT JOIN [ReSale] " +
            "ON [ReSale].ID = [ReSaleItem].ReSaleID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [ReSale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [ReSale].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = ( " +
            "CASE " +
            "WHEN [ReSale].ChangedToInvoiceByID IS NOT NULL " +
            "THEN [ReSale].ChangedToInvoiceByID " +
            "ELSE [ReSale].UserID " +
            "END " +
            ") " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 0 " +
            "AND [Product].NetUID = @ProductNetId " +
            "AND [ConsignmentItemMovement].ReSaleItemId IS NOT NULL " +
            "GROUP BY [ProductIncome].Number " +
            ", [ProductIncome].FromDate " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") " +
            ", [SaleNumber].[Value] " +
            ", ISNULL([ReSale].ChangedToInvoice, [ReSale].Updated) " +
            ", [Client].FullName " +
            ", [Storage].[Name] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") " +
            ", [User].LastName " +
            ", [ReSaleItem].PricePerItem " +
            ", [ReSale].Comment " +
            ", [ReSale].ChangedToInvoice " +
            ", [ConsignmentItemMovement].IsIncomeMovement " +
            ") " +
            "SELECT * " +
            "FROM [OutcomeConsignmentInfo_CTE] " +
            "ORDER BY [OutcomeConsignmentInfo_CTE].FromDate DESC ",
            new {
                ProductNetId = productNetId,
                From = from,
                To = to,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );
    }

    public ConsignmentItem GetInfoIcomesFiltered(long id) {
        ConsignmentItem consignmentItems = new();

        Type[] incomesTypesConsigmentItem = {
            typeof(ConsignmentItem),
            typeof(Consignment),
            typeof(ProductIncome)
        };

        Func<object[], ProductIncome> incomesMapperConsignmentItem = objects => {
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[0];
            Consignment consignment = (Consignment)objects[1];
            ProductIncome productIncome = (ProductIncome)objects[2];

            consignment.ProductIncome = productIncome;
            consignmentItem.Consignment = consignment;
            consignmentItems = consignmentItem;

            return productIncome;
        };

        _connection.Query(
            "SELECT DISTINCT * FROM ConsignmentItem " +
            "LEFT JOIN Consignment " +
            "ON Consignment.ID = ConsignmentItem.ConsignmentID " +
            "LEFT JOIN ProductIncome " +
            "ON ProductIncome.ID = Consignment.ProductIncomeID " +
            "WHERE ConsignmentItem.ID = @Id ",
            incomesTypesConsigmentItem,
            incomesMapperConsignmentItem,
            new {
                Id = id
            });

        return consignmentItems;
    }

    public IEnumerable<InfoIncome> GetInfoIcomesFiltered(Guid productNetId) {
        List<InfoIncome> infoIncomes = new();

        Type[] incomesTypesConsigmentItem = {
            typeof(ConsignmentItem),
            typeof(Consignment),
            typeof(ProductIncome),
            typeof(Product)
        };

        Func<object[], ProductIncome> incomesMapperConsignmentItem = objects => {
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[0];
            Consignment consignment = (Consignment)objects[1];
            ProductIncome productIncome = (ProductIncome)objects[2];
            if (!infoIncomes.Any(x => x.ConsignmentItemId == consignmentItem.Id))
                infoIncomes.Add(new InfoIncome {
                    ConsignmentItemId = consignmentItem.Id,
                    Number = productIncome.Number + " К-сть: " + consignmentItem.RemainingQty,
                    StorageId = consignment.StorageId
                });

            return productIncome;
        };

        _connection.Query(
            "SELECT DISTINCT * FROM ConsignmentItem " +
            "LEFT JOIN Consignment " +
            "ON Consignment.ID = ConsignmentItem.ConsignmentID " +
            "LEFT JOIN ProductIncome " +
            "ON ProductIncome.ID = Consignment.ProductIncomeID " +
            "LEFT JOIN Product " +
            "ON Product.ID = ConsignmentItem.ProductID " +
            "WHERE Product.NetUID = @NetID",
            incomesTypesConsigmentItem,
            incomesMapperConsignmentItem,
            new {
                NetID = productNetId
            });

        return infoIncomes;
    }

    public IEnumerable<MovementConsignmentInfo> GetMovementConsignmentInfoFiltered(
        IEnumerable<ConsignmentItemMovementType> types,
        Guid productNetId,
        DateTime from,
        DateTime to,
        ConsignmentMovementType consignmentMovementType) {
        if (!types.Any()) return new List<MovementConsignmentInfo>();

        StringBuilder builder = new();

        builder.Append(";WITH [MovementConsignmentInfo_CTE] ");
        builder.Append("AS ( ");

        bool firstType = true;

        foreach (ConsignmentItemMovementType movementType in types) {
            if (!firstType)
                builder.Append("UNION ALL ");

            switch (movementType) {
                case ConsignmentItemMovementType.Sale:
                    builder.Append("SELECT [ProductIncome].Number AS [IncomeDocumentNumber] ");
                    builder.Append(", [ProductIncome].FromDate AS [IncomeDocumentFromDate] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN @Culture = N'uk' ");
                    builder.Append("THEN [ConsignmentItemMovementTypeName].NameUa ");
                    builder.Append("ELSE [ConsignmentItemMovementTypeName].NamePl ");
                    builder.Append("END ");
                    builder.Append(") AS [DocumentType] ");
                    builder.Append(", [SaleNumber].[Value] AS [DocumentNumber] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [Sale].ChangedToInvoice IS NOT NULL ");
                    builder.Append("THEN [Sale].ChangedToInvoice ");
                    builder.Append("ELSE [Sale].Updated ");
                    builder.Append("END ");
                    builder.Append(") AS [DocumentFromDate] ");
                    builder.Append(", [Client].FullName AS [ClientName] ");
                    builder.Append(", [Storage].[Name] AS [StorageName] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [OrganizationTranslation].[Name] IS NOT NULL ");
                    builder.Append("THEN [OrganizationTranslation].[Name] ");
                    builder.Append("ELSE [Organization].[Name] ");
                    builder.Append("END ");
                    builder.Append(") AS [OrganizationName] ");
                    builder.Append(", [User].LastName AS [Responsible] ");
                    builder.Append(", (CASE ");
                    builder.Append("WHEN [OrderItem].PricePerItem <> 0 ");
                    builder.Append("THEN [OrderItem].PricePerItem ");
                    builder.Append(
                        "ELSE dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, [ClientAgreement].NetUID, @Culture, [Agreement].WithVATAccounting, null) ");
                    builder.Append("END) AS [Price] ");
                    builder.Append(", 0 AS [AccountingPrice] ");
                    builder.Append(", ROUND([OrderItem].OneTimeDiscount + [OrderItem].DiscountAmount, 2) AS [Discount] ");
                    builder.Append(", 0 AS [IncomeQty] ");
                    builder.Append(", CASE WHEN [ConsignmentItemMovement].Qty IS NOT NULL ");
                    builder.Append("THEN SUM([ConsignmentItemMovement].Qty) ");
                    builder.Append("ELSE SUM([OrderItem].Qty) ");
                    builder.Append("END AS [OutcomeQty] ");
                    builder.Append(", [Sale].Comment AS [Comment] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN EXISTS (SELECT 1 ");
                    builder.Append("FROM [HistoryInvoiceEdit] ");
                    builder.Append("WHERE [HistoryInvoiceEdit].SaleId = [Sale].ID) ");
                    builder.Append("THEN 1 ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [IsEdited] ");
                    builder.Append("FROM [Sale] ");
                    builder.Append("LEFT JOIN [Order] ");
                    builder.Append("ON [Order].ID = [Sale].OrderID ");
                    builder.Append("LEFT JOIN [OrderItem] ");
                    builder.Append("ON [OrderItem].OrderID = [Order].ID ");
                    builder.Append("AND [OrderItem].Deleted = 0 ");
                    builder.Append("AND [OrderItem].IsClosed = 0 ");
                    builder.Append("LEFT JOIN [Product] ");
                    builder.Append("ON [Product].ID = [OrderItem].ProductID ");
                    builder.Append("LEFT JOIN [ConsignmentItemMovement] ");
                    builder.Append("ON [ConsignmentItemMovement].OrderItemID = [OrderItem].ID ");
                    builder.Append("LEFT JOIN [ConsignmentItem] ");
                    builder.Append("ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID ");
                    builder.Append("LEFT JOIN [Consignment] ");
                    builder.Append("ON [Consignment].ID = [ConsignmentItem].ConsignmentID ");
                    builder.Append("LEFT JOIN [ProductIncome] ");
                    builder.Append("ON [ProductIncome].ID = [Consignment].ProductIncomeID ");
                    builder.Append("LEFT JOIN ( ");
                    builder.Append("SELECT DISTINCT [OrderItemID], [ProductAvailabilityID] FROM [ProductReservation] ");
                    builder.Append(") AS [ProductReservation] ");
                    builder.Append("ON [ProductReservation].OrderItemID = [OrderItem].ID ");
                    builder.Append("LEFT JOIN [ProductAvailability] ");
                    builder.Append("ON [ProductAvailability].ID = [ProductReservation].ProductAvailabilityID ");
                    builder.Append("LEFT JOIN [Storage] ");
                    builder.Append("ON [Storage].ID = [ProductAvailability].StorageID ");
                    builder.Append("LEFT JOIN [Organization] ");
                    builder.Append("ON [Organization].ID = [Storage].OrganizationID ");
                    builder.Append("LEFT JOIN [OrganizationTranslation] ");
                    builder.Append("ON [OrganizationTranslation].OrganizationID = [Organization].ID ");
                    builder.Append("AND [OrganizationTranslation].CultureCode = @Culture ");
                    builder.Append("AND [OrganizationTranslation].Deleted = 0 ");
                    builder.Append("LEFT JOIN [SaleNumber] ");
                    builder.Append("ON [SaleNumber].ID = [Sale].SaleNumberID ");
                    builder.Append("LEFT JOIN [BaseLifeCycleStatus] ");
                    builder.Append("ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID ");
                    builder.Append("LEFT JOIN [ConsignmentItemMovementTypeName] ");
                    builder.Append("ON [ConsignmentItemMovementTypeName].ID = ( ");
                    builder.Append("SELECT TOP(1) [JoinTypeName].ID ");
                    builder.Append("FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] ");
                    builder.Append("WHERE [JoinTypeName].MovementType = ");
                    builder.Append("CASE WHEN [BaseLifeCycleStatus].SaleLifeCycleType = 1 ");
                    builder.Append("THEN 0 ");
                    builder.Append("ELSE 15 ");
                    builder.Append("END ");
                    builder.Append(") ");
                    builder.Append("LEFT JOIN [ClientAgreement] ");
                    builder.Append("ON [ClientAgreement].ID = [Sale].ClientAgreementID ");
                    builder.Append("LEFT JOIN [Agreement] ");
                    builder.Append("ON [Agreement].ID = [ClientAgreement].AgreementID ");
                    builder.Append("LEFT JOIN [Client] ");
                    builder.Append("ON [Client].ID = [ClientAgreement].ClientID ");
                    builder.Append("LEFT JOIN [User] ");
                    builder.Append("ON [User].ID = ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [Sale].ChangedToInvoiceByID IS NOT NULL ");
                    builder.Append("THEN [Sale].ChangedToInvoiceByID ");
                    builder.Append("ELSE [Sale].UserID ");
                    builder.Append("END ");
                    builder.Append(") ");
                    builder.Append("WHERE [Product].NetUID = @ProductNetId ");

                    if (!consignmentMovementType.Equals(ConsignmentMovementType.All)) {
                        if (consignmentMovementType.Equals(ConsignmentMovementType.Accounting))
                            builder.Append("AND [Sale].[IsVatSale] = 1 ");
                        else if (consignmentMovementType.Equals(ConsignmentMovementType.Management))
                            builder.Append("AND [Sale].[IsVatSale] = 0 ");
                    }

                    builder.Append("GROUP BY [ProductIncome].Number ");
                    builder.Append(", [ProductIncome].FromDate ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN @Culture = N'uk' ");
                    builder.Append("THEN [ConsignmentItemMovementTypeName].NameUa ");
                    builder.Append("ELSE [ConsignmentItemMovementTypeName].NamePl ");
                    builder.Append("END ");
                    builder.Append(") ");
                    builder.Append(", [SaleNumber].[Value] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [Sale].ChangedToInvoice IS NOT NULL ");
                    builder.Append("THEN [Sale].ChangedToInvoice ");
                    builder.Append("ELSE [Sale].Updated ");
                    builder.Append("END ");
                    builder.Append(") ");
                    builder.Append(", [Client].FullName ");
                    builder.Append(", [Storage].[Name] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [OrganizationTranslation].[Name] IS NOT NULL ");
                    builder.Append("THEN [OrganizationTranslation].[Name] ");
                    builder.Append("ELSE [Organization].[Name] ");
                    builder.Append("END ");
                    builder.Append(") ");
                    builder.Append(", [User].LastName ");
                    builder.Append(", [OrderItem].PricePerItem ");
                    builder.Append(", [OrderItem].DiscountAmount ");
                    builder.Append(", [OrderItem].OneTimeDiscount ");
                    builder.Append(", [OrderItem].AssignedSpecificationID ");
                    builder.Append(", [Sale].Comment ");
                    builder.Append(", [ConsignmentItemMovement].IsIncomeMovement ");
                    builder.Append(", [Product].NetUID ");
                    builder.Append(", [Sale].NetUID ");
                    builder.Append(", [ClientAgreement].NetUID ");
                    builder.Append(", [Agreement].WithVATAccounting ");
                    builder.Append(", [ConsignmentItemMovement].Qty ");
                    builder.Append(", [Sale].ID ");

                    builder.Append("UNION ALL ");

                    if (consignmentMovementType.Equals(ConsignmentMovementType.Management))
                        continue;

                    builder.Append("SELECT [ProductIncome].Number AS [IncomeDocumentNumber] ");
                    builder.Append(", [ProductIncome].FromDate AS [IncomeDocumentFromDate] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN @Culture = N'uk' ");
                    builder.Append("THEN [ConsignmentItemMovementTypeName].NameUa ");
                    builder.Append("ELSE [ConsignmentItemMovementTypeName].NamePl ");
                    builder.Append("END ");
                    builder.Append(") AS [DocumentType] ");
                    builder.Append(", [SaleNumber].[Value] AS [DocumentNumber] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ReSale].ChangedToInvoice IS NOT NULL ");
                    builder.Append("THEN [ReSale].ChangedToInvoice ");
                    builder.Append("ELSE [ReSale].Updated ");
                    builder.Append("END ");
                    builder.Append(") AS [DocumentFromDate] ");
                    builder.Append(", [Client].FullName AS [ClientName] ");
                    builder.Append(", [Storage].[Name] AS [StorageName] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [OrganizationTranslation].[Name] IS NOT NULL ");
                    builder.Append("THEN [OrganizationTranslation].[Name] ");
                    builder.Append("ELSE [Organization].[Name] ");
                    builder.Append("END ");
                    builder.Append(") AS [OrganizationName] ");
                    builder.Append(", [User].LastName AS [Responsible] ");
                    builder.Append(", [ReSaleItem].PricePerItem / [dbo].GetExchangeRateByCurrencyIdAndCode(NULL, 'EUR' ");
                    builder.Append(", CASE WHEN [ReSale].[ChangedToInvoice] IS NOT NULL THEN [ReSale].[ChangedToInvoice] ELSE [ReSale].[Updated] END)");
                    builder.Append(" AS [Price] ");
                    builder.Append(", 0 AS [AccountingPrice] ");
                    builder.Append(", 0 AS [Discount] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 ");
                    builder.Append("THEN SUM([ConsignmentItemMovement].Qty) ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [IncomeQty] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 ");
                    builder.Append("THEN SUM([ConsignmentItemMovement].Qty) ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [OutcomeQty] ");
                    builder.Append(", [ReSale].Comment AS [Comment] ");
                    builder.Append(", 0 AS [IsEdited] ");
                    builder.Append("FROM [ConsignmentItemMovement] ");
                    builder.Append("LEFT JOIN [ReSaleItem] ");
                    builder.Append("ON [ReSaleItem].ID = [ConsignmentItemMovement].[ReSaleItemID] ");
                    builder.Append("LEFT JOIN [ReSaleAvailability] ");
                    builder.Append("ON [ReSaleAvailability].ID = [ReSaleItem].[ReSaleAvailabilityID] ");
                    builder.Append("LEFT JOIN [ConsignmentItem] ");
                    builder.Append("ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID ");
                    builder.Append("LEFT JOIN [Product] ");
                    builder.Append("ON [Product].ID = [ConsignmentItem].ProductID ");
                    builder.Append("LEFT JOIN [Consignment] ");
                    builder.Append("ON [Consignment].ID = [ConsignmentItem].ConsignmentID ");
                    builder.Append("LEFT JOIN [ProductIncome] ");
                    builder.Append("ON [ProductIncome].ID = [Consignment].ProductIncomeID ");
                    builder.Append("LEFT JOIN [Organization] ");
                    builder.Append("ON [Organization].ID = [Consignment].OrganizationID ");
                    builder.Append("LEFT JOIN [OrganizationTranslation] ");
                    builder.Append("ON [OrganizationTranslation].OrganizationID = [Organization].ID ");
                    builder.Append("AND [OrganizationTranslation].CultureCode = @Culture ");
                    builder.Append("LEFT JOIN [Storage] ");
                    builder.Append("ON [Storage].ID = [Consignment].StorageID ");
                    builder.Append("LEFT JOIN [ConsignmentItemMovementTypeName] ");
                    builder.Append("ON [ConsignmentItemMovementTypeName].ID = ( ");
                    builder.Append("SELECT TOP(1) [JoinTypeName].ID ");
                    builder.Append("FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] ");
                    builder.Append("WHERE [JoinTypeName].MovementType = 0 ");
                    builder.Append(") ");
                    builder.Append("LEFT JOIN [ReSale] ");
                    builder.Append("ON [ReSale].ID = [ReSaleItem].ReSaleID ");
                    builder.Append("LEFT JOIN [SaleNumber] ");
                    builder.Append("ON [SaleNumber].ID = [ReSale].SaleNumberID ");
                    builder.Append("LEFT JOIN [ClientAgreement] ");
                    builder.Append("ON [ClientAgreement].ID = [ReSale].ClientAgreementID ");
                    builder.Append("LEFT JOIN [Client] ");
                    builder.Append("ON [Client].ID = [ClientAgreement].ClientID ");
                    builder.Append("LEFT JOIN [User] ");
                    builder.Append("ON [User].ID = ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ReSale].ChangedToInvoiceByID IS NOT NULL ");
                    builder.Append("THEN [ReSale].ChangedToInvoiceByID ");
                    builder.Append("ELSE [ReSale].UserID ");
                    builder.Append("END ");
                    builder.Append(") ");
                    builder.Append("WHERE [ConsignmentItemMovement].Deleted = 0 ");
                    builder.Append("AND [ConsignmentItemMovement].MovementType = 0 ");
                    builder.Append("AND [Product].NetUID = @ProductNetId ");
                    builder.Append("AND [ConsignmentItemMovement].ReSaleItemId IS NOT NULL ");
                    builder.Append("GROUP BY [ProductIncome].Number ");
                    builder.Append(", [ProductIncome].FromDate ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN @Culture = N'uk' ");
                    builder.Append("THEN [ConsignmentItemMovementTypeName].NameUa ");
                    builder.Append("ELSE [ConsignmentItemMovementTypeName].NamePl ");
                    builder.Append("END ");
                    builder.Append(") ");
                    builder.Append(", [SaleNumber].[Value] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ReSale].ChangedToInvoice IS NOT NULL ");
                    builder.Append("THEN [ReSale].ChangedToInvoice ");
                    builder.Append("ELSE [ReSale].Updated ");
                    builder.Append("END ");
                    builder.Append(") ");
                    builder.Append(", [Client].FullName ");
                    builder.Append(", [Storage].[Name] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [OrganizationTranslation].[Name] IS NOT NULL ");
                    builder.Append("THEN [OrganizationTranslation].[Name] ");
                    builder.Append("ELSE [Organization].[Name] ");
                    builder.Append("END ");
                    builder.Append(") ");
                    builder.Append(", [User].LastName ");
                    builder.Append(", [ReSaleItem].PricePerItem ");
                    builder.Append(", [ReSale].Comment ");
                    builder.Append(", [ConsignmentItemMovement].IsIncomeMovement ");

                    break;
                case ConsignmentItemMovementType.Return:
                    builder.Append("SELECT [RootProductIncome].Number AS [IncomeDocumentNumber] ");
                    builder.Append(", [RootProductIncome].FromDate AS [IncomeDocumentFromDate] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN @Culture = N'uk' ");
                    builder.Append("THEN [ConsignmentItemMovementTypeName].NameUa ");
                    builder.Append("ELSE [ConsignmentItemMovementTypeName].NamePl ");
                    builder.Append("END ");
                    builder.Append(") AS [DocumentType] ");
                    builder.Append(", [SaleReturn].Number AS [DocumentNumber] ");
                    builder.Append(", [SaleReturn].FromDate AS [DocumentFromDate] ");
                    builder.Append(", [Client].FullName AS [ClientName] ");
                    builder.Append(", [Storage].[Name] AS [StorageName] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [OrganizationTranslation].[Name] IS NOT NULL ");
                    builder.Append("THEN [OrganizationTranslation].[Name] ");
                    builder.Append("ELSE [Organization].[Name] ");
                    builder.Append("END ");
                    builder.Append(") AS [OrganizationName] ");
                    builder.Append(", [User].LastName AS [Responsible] ");
                    builder.Append(", ROUND([SaleReturnItem].Amount / [SaleReturnItem].Qty, 2) AS [Price] ");
                    builder.Append(", 0 AS [AccountingPrice] ");
                    builder.Append(", 0 AS [Discount] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 ");
                    builder.Append("THEN SUM([ConsignmentItemMovement].Qty) ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [IncomeQty] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 ");
                    builder.Append("THEN SUM([ConsignmentItemMovement].Qty) ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [OutcomeQty] ");
                    builder.Append(", N'' AS [Comment] ");
                    builder.Append(", 0 AS [IsEdited] ");
                    builder.Append("FROM [ConsignmentItemMovement] ");
                    builder.Append("LEFT JOIN [ConsignmentItem] ");
                    builder.Append("ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID ");
                    builder.Append("LEFT JOIN [Product] ");
                    builder.Append("ON [Product].ID = [ConsignmentItem].ProductID ");
                    builder.Append("LEFT JOIN [Consignment] ");
                    builder.Append("ON [Consignment].ID = [ConsignmentItem].ConsignmentID ");
                    builder.Append("LEFT JOIN [ProductIncome] ");
                    builder.Append("ON [ProductIncome].ID = [Consignment].ProductIncomeID ");
                    builder.Append("LEFT JOIN [Organization] ");
                    builder.Append("ON [Organization].ID = [Consignment].OrganizationID ");
                    builder.Append("LEFT JOIN [OrganizationTranslation] ");
                    builder.Append("ON [OrganizationTranslation].OrganizationID = [Organization].ID ");
                    builder.Append("AND [OrganizationTranslation].CultureCode = @Culture ");
                    builder.Append("AND [OrganizationTranslation].Deleted = 0 ");
                    builder.Append("LEFT JOIN [Storage] ");
                    builder.Append("ON [Storage].ID = [Consignment].StorageID ");
                    builder.Append("LEFT JOIN [ConsignmentItemMovementTypeName] ");
                    builder.Append("ON [ConsignmentItemMovementTypeName].ID = ( ");
                    builder.Append("SELECT TOP(1) [JoinTypeName].ID ");
                    builder.Append("FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] ");
                    builder.Append("WHERE [JoinTypeName].MovementType = 1 ");
                    builder.Append(") ");
                    builder.Append("LEFT JOIN [ProductIncomeItem] ");
                    builder.Append("ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID ");
                    builder.Append("LEFT JOIN [SaleReturnItem] ");
                    builder.Append("ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID ");
                    builder.Append("LEFT JOIN [SaleReturn] ");
                    builder.Append("ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID ");
                    builder.Append("LEFT JOIN [ClientAgreement] ");
                    builder.Append("ON [ClientAgreement].[ID] = [SaleReturn].[ClientAgreementID] ");
                    builder.Append("LEFT JOIN [Agreement] ");
                    builder.Append("ON [Agreement].[ID] = [ClientAgreement].[AgreementID] ");
                    builder.Append("LEFT JOIN [Client] ");
                    builder.Append("ON [Client].ID = [SaleReturn].ClientID ");
                    builder.Append("LEFT JOIN [User] ");
                    builder.Append("ON [User].ID = ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [SaleReturn].UpdatedByID IS NOT NULL ");
                    builder.Append("THEN [SaleReturn].UpdatedByID ");
                    builder.Append("ELSE [SaleReturn].CreatedByID ");
                    builder.Append("END ");
                    builder.Append(") ");
                    builder.Append("LEFT JOIN [ConsignmentItem] AS [RootConsignmentItem] ");
                    builder.Append("ON [RootConsignmentItem].ID = [ConsignmentItem].RootConsignmentItemID ");
                    builder.Append("LEFT JOIN [Consignment] AS [RootConsignment] ");
                    builder.Append("ON [RootConsignment].ID = [RootConsignmentItem].ConsignmentID ");
                    builder.Append("LEFT JOIN [ProductIncome] AS [RootProductIncome] ");
                    builder.Append("ON [RootProductIncome].ID = [RootConsignment].ProductIncomeID ");
                    builder.Append("WHERE [ConsignmentItemMovement].Deleted = 0 ");
                    builder.Append("AND [ConsignmentItemMovement].MovementType = 1 ");
                    builder.Append("AND [Product].NetUID = @ProductNetId ");
                    builder.Append("AND [SaleReturn].[IsCanceled] = 0 ");

                    if (!consignmentMovementType.Equals(ConsignmentMovementType.All)) {
                        if (consignmentMovementType.Equals(ConsignmentMovementType.Accounting))
                            builder.Append("AND [Agreement].[WithVATAccounting] = 1 ");
                        else if (consignmentMovementType.Equals(ConsignmentMovementType.Management))
                            builder.Append("AND [Agreement].[WithVATAccounting] = 0 ");
                    }

                    builder.Append("GROUP BY ");
                    builder.Append("[RootProductIncome].Number ");
                    builder.Append(", [ConsignmentItemMovement].IsIncomeMovement ");
                    builder.Append(", [RootProductIncome].FromDate ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN @Culture = N'uk' ");
                    builder.Append("THEN [ConsignmentItemMovementTypeName].NameUa ");
                    builder.Append("ELSE [ConsignmentItemMovementTypeName].NamePl ");
                    builder.Append("END ");
                    builder.Append(") ");
                    builder.Append(", [SaleReturn].Number ");
                    builder.Append(", [SaleReturn].FromDate ");
                    builder.Append(", [Client].FullName ");
                    builder.Append(", [Storage].[Name] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [OrganizationTranslation].[Name] IS NOT NULL ");
                    builder.Append("THEN [OrganizationTranslation].[Name] ");
                    builder.Append("ELSE [Organization].[Name] ");
                    builder.Append("END ");
                    builder.Append(") ");
                    builder.Append(", [User].LastName ");
                    builder.Append(", ROUND([SaleReturnItem].Amount / [SaleReturnItem].Qty, 2) ");

                    break;
                case ConsignmentItemMovementType.Shifting:
                    builder.Append("SELECT [ProductIncome].Number AS [IncomeDocumentNumber] ");
                    builder.Append(", [ProductIncome].FromDate AS [IncomeDocumentFromDate] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN @Culture = N'uk' ");
                    builder.Append("THEN [ConsignmentItemMovementTypeName].NameUa ");
                    builder.Append("ELSE [ConsignmentItemMovementTypeName].NamePl ");
                    builder.Append("END ");
                    builder.Append(") AS [DocumentType] ");
                    builder.Append(", [SaleNumber].[Value] AS [DocumentNumber] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [Sale].ChangedToInvoice IS NOT NULL ");
                    builder.Append("THEN [Sale].ChangedToInvoice ");
                    builder.Append("ELSE [Sale].Updated ");
                    builder.Append("END ");
                    builder.Append(") AS [DocumentFromDate] ");
                    builder.Append(", [Client].FullName AS [ClientName] ");
                    builder.Append(", N'' AS [StorageName] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [OrganizationTranslation].[Name] IS NOT NULL ");
                    builder.Append("THEN [OrganizationTranslation].[Name] ");
                    builder.Append("ELSE [Organization].[Name] ");
                    builder.Append("END ");
                    builder.Append(") AS [OrganizationName] ");
                    builder.Append(", [User].LastName AS [Responsible] ");
                    builder.Append(", [OrderItem].PricePerItem AS [Price] ");
                    builder.Append(", 0 AS [AccountingPrice] ");
                    builder.Append(", ROUND([OrderItem].OneTimeDiscount + [OrderItem].DiscountAmount, 2) AS [Discount] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [IncomeQty] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [OutcomeQty] ");
                    builder.Append(", [OrderItemBaseShiftStatus].Comment AS [Comment] ");
                    builder.Append(", 0 AS [IsEdited] ");
                    builder.Append("FROM [ConsignmentItemMovement] ");
                    builder.Append("LEFT JOIN [ConsignmentItem] ");
                    builder.Append("ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID ");
                    builder.Append("LEFT JOIN [Product] ");
                    builder.Append("ON [Product].ID = [ConsignmentItem].ProductID ");
                    builder.Append("LEFT JOIN [Consignment] ");
                    builder.Append("ON [Consignment].ID = [ConsignmentItem].ConsignmentID ");
                    builder.Append("LEFT JOIN [ProductIncome] ");
                    builder.Append("ON [ProductIncome].ID = [Consignment].ProductIncomeID ");
                    builder.Append("LEFT JOIN [Organization] ");
                    builder.Append("ON [Organization].ID = [Consignment].OrganizationID ");
                    builder.Append("LEFT JOIN [OrganizationTranslation] ");
                    builder.Append("ON [OrganizationTranslation].OrganizationID = [Organization].ID ");
                    builder.Append("AND [OrganizationTranslation].CultureCode = @Culture ");
                    builder.Append("AND [OrganizationTranslation].Deleted = 0 ");
                    builder.Append("LEFT JOIN [Storage] ");
                    builder.Append("ON [Storage].ID = [Consignment].StorageID ");
                    builder.Append("LEFT JOIN [ConsignmentItemMovementTypeName] ");
                    builder.Append("ON [ConsignmentItemMovementTypeName].ID = ( ");
                    builder.Append("SELECT TOP(1) [JoinTypeName].ID ");
                    builder.Append("FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] ");
                    builder.Append("WHERE [JoinTypeName].MovementType = 2 ");
                    builder.Append(") ");
                    builder.Append("LEFT JOIN [OrderItemBaseShiftStatus] ");
                    builder.Append("ON [OrderItemBaseShiftStatus].ID = [ConsignmentItemMovement].OrderItemBaseShiftStatusID ");
                    builder.Append("LEFT JOIN [OrderItem] ");
                    builder.Append("ON [OrderItem].ID = [OrderItemBaseShiftStatus].OrderItemID ");
                    builder.Append("LEFT JOIN [Sale] ");
                    builder.Append("ON [Sale].OrderID = [OrderItem].OrderID ");
                    builder.Append("LEFT JOIN [SaleNumber] ");
                    builder.Append("ON [SaleNumber].ID = [Sale].SaleNumberID ");
                    builder.Append("LEFT JOIN [ClientAgreement] ");
                    builder.Append("ON [ClientAgreement].ID = [Sale].ClientAgreementID ");
                    builder.Append("LEFT JOIN [Client] ");
                    builder.Append("ON [Client].ID = [ClientAgreement].ClientID ");
                    builder.Append("LEFT JOIN [User] ");
                    builder.Append("ON [User].ID = [OrderItemBaseShiftStatus].UserID ");
                    builder.Append("WHERE [ConsignmentItemMovement].Deleted = 0 ");
                    builder.Append("AND [ConsignmentItemMovement].MovementType = 2 ");
                    builder.Append("AND [Product].NetUID = @ProductNetId ");

                    if (!consignmentMovementType.Equals(ConsignmentMovementType.All)) {
                        if (consignmentMovementType.Equals(ConsignmentMovementType.Accounting))
                            builder.Append("AND [Sale].[IsVatSale] = 1 ");
                        else if (consignmentMovementType.Equals(ConsignmentMovementType.Management))
                            builder.Append("AND [Sale].[IsVatSale] = 0 ");
                    }

                    break;
                case ConsignmentItemMovementType.Income:
                    builder.Append("SELECT N'' AS [IncomeDocumentNumber] ");
                    builder.Append(", NULL AS [IncomeDocumentFromDate] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN @Culture = N'uk' ");
                    builder.Append("THEN [ConsignmentItemMovementTypeName].NameUa ");
                    builder.Append("ELSE [ConsignmentItemMovementTypeName].NamePl ");
                    builder.Append("END ");
                    builder.Append(") AS [DocumentType] ");
                    builder.Append(", [ProductIncome].Number AS [DocumentNumber] ");
                    builder.Append(", [ProductIncome].FromDate AS [DocumentFromDate] ");
                    builder.Append(", [Client].FullName AS [ClientName] ");
                    builder.Append(", [Storage].[Name] AS [StorageName] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [OrganizationTranslation].[Name] IS NOT NULL ");
                    builder.Append("THEN [OrganizationTranslation].[Name] ");
                    builder.Append("ELSE [Organization].[Name] ");
                    builder.Append("END ");
                    builder.Append(") AS [OrganizationName] ");
                    builder.Append(", [User].LastName AS [Responsible] ");
                    builder.Append(", [ConsignmentItem].Price AS [Price] ");
                    builder.Append(", [ConsignmentItem].AccountingPrice AS [AccountingPrice] ");
                    builder.Append(", 0 AS [Discount] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [IncomeQty] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [OutcomeQty] ");
                    builder.Append(", [ProductIncome].Comment AS [Comment] ");
                    builder.Append(", 0 AS [IsEdited] ");
                    builder.Append("FROM [ConsignmentItemMovement] ");
                    builder.Append("LEFT JOIN [ConsignmentItem] ");
                    builder.Append("ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID ");
                    builder.Append("LEFT JOIN [Product] ");
                    builder.Append("ON [Product].ID = [ConsignmentItem].ProductID ");
                    builder.Append("LEFT JOIN [Consignment] ");
                    builder.Append("ON [Consignment].ID = [ConsignmentItem].ConsignmentID ");
                    builder.Append("LEFT JOIN [ProductIncome] ");
                    builder.Append("ON [ProductIncome].ID = [Consignment].ProductIncomeID ");
                    builder.Append("LEFT JOIN [Organization] ");
                    builder.Append("ON [Organization].ID = [Consignment].OrganizationID ");
                    builder.Append("LEFT JOIN [OrganizationTranslation] ");
                    builder.Append("ON [OrganizationTranslation].OrganizationID = [Organization].ID ");
                    builder.Append("AND [OrganizationTranslation].CultureCode = @Culture ");
                    builder.Append("AND [OrganizationTranslation].Deleted = 0 ");
                    builder.Append("LEFT JOIN [Storage] ");
                    builder.Append("ON [Storage].ID = [Consignment].StorageID ");
                    builder.Append("LEFT JOIN [ConsignmentItemMovementTypeName] ");
                    builder.Append("ON [ConsignmentItemMovementTypeName].ID = ( ");
                    builder.Append("SELECT TOP(1) [JoinTypeName].ID ");
                    builder.Append("FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] ");
                    builder.Append("WHERE [JoinTypeName].MovementType = 3 ");
                    builder.Append(") ");
                    builder.Append("LEFT JOIN [ProductIncomeItem] ");
                    builder.Append("ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID ");
                    builder.Append("LEFT JOIN [PackingListPackageOrderItem] ");
                    builder.Append("ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID ");
                    builder.Append("LEFT JOIN [PackingList] ");
                    builder.Append("ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID ");
                    builder.Append("LEFT JOIN [SupplyInvoice] ");
                    builder.Append("ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID ");
                    builder.Append("LEFT JOIN [SupplyOrder] ");
                    builder.Append("ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID ");
                    builder.Append("LEFT JOIN [ClientAgreement] ");
                    builder.Append("ON [ClientAgreement].ID = [SupplyOrder].ClientAgreementID ");
                    builder.Append("LEFT JOIN [Agreement] ");
                    builder.Append("ON [Agreement].ID = [ClientAgreement].AgreementID ");
                    builder.Append("LEFT JOIN [Client] ");
                    builder.Append("ON [Client].ID = [SupplyOrder].ClientID ");
                    builder.Append("LEFT JOIN [User] ");
                    builder.Append("ON [ProductIncome].UserID = [User].ID ");
                    builder.Append("WHERE [ConsignmentItemMovement].Deleted = 0 ");
                    builder.Append("AND [ConsignmentItemMovement].Qty != 0 ");
                    builder.Append("AND [ConsignmentItemMovement].MovementType = 3 ");
                    builder.Append("AND [Product].NetUID = @ProductNetId ");

                    if (!consignmentMovementType.Equals(ConsignmentMovementType.All)) {
                        if (consignmentMovementType.Equals(ConsignmentMovementType.Accounting))
                            builder.Append("AND [Storage].[ForVatProducts] = 1 ");
                        else if (consignmentMovementType.Equals(ConsignmentMovementType.Management))
                            builder.Append("AND [Storage].[ForVatProducts] = 0 ");
                    }

                    break;
                case ConsignmentItemMovementType.UkraineOrder:
                    builder.Append("SELECT ISNULL([RootProductIncome].Number, N'') AS [IncomeDocumentNumber] ");
                    builder.Append(", [RootProductIncome].FromDate AS [IncomeDocumentFromDate] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN @Culture = N'uk' ");
                    builder.Append("THEN [ConsignmentItemMovementTypeName].NameUa ");
                    builder.Append("ELSE [ConsignmentItemMovementTypeName].NamePl ");
                    builder.Append("END ");
                    builder.Append(") AS [DocumentType] ");
                    builder.Append(", [ProductIncome].Number AS [DocumentNumber] ");
                    builder.Append(", [ProductIncome].FromDate AS [DocumentFromDate] ");
                    builder.Append(", [Client].FullName AS [ClientName] ");
                    builder.Append(", [Storage].[Name] AS [StorageName] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [OrganizationTranslation].[Name] IS NOT NULL ");
                    builder.Append("THEN [OrganizationTranslation].[Name] ");
                    builder.Append("ELSE [Organization].[Name] ");
                    builder.Append("END ");
                    builder.Append(") AS [OrganizationName] ");
                    builder.Append(", [User].LastName AS [Responsible] ");
                    builder.Append(", [ConsignmentItem].Price AS [Price] ");
                    builder.Append(", [ConsignmentItem].NetPrice AS [AccountingPrice] ");
                    //builder.Append(", [ConsignmentItem].AccountingPrice AS [AccountingPrice] ");
                    builder.Append(", 0 AS [Discount] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [IncomeQty] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [OutcomeQty] ");
                    builder.Append(", [ProductIncome].Comment AS [Comment] ");
                    builder.Append(", 0 AS [IsEdited] ");
                    builder.Append("FROM [ConsignmentItemMovement] ");
                    builder.Append("LEFT JOIN [ConsignmentItem] ");
                    builder.Append("ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID ");
                    builder.Append("LEFT JOIN [Product] ");
                    builder.Append("ON [Product].ID = [ConsignmentItem].ProductID ");
                    builder.Append("LEFT JOIN [Consignment] ");
                    builder.Append("ON [Consignment].ID = [ConsignmentItem].ConsignmentID ");
                    builder.Append("LEFT JOIN [ProductIncome] ");
                    builder.Append("ON [ProductIncome].ID = [Consignment].ProductIncomeID ");
                    builder.Append("LEFT JOIN [Organization] ");
                    builder.Append("ON [Organization].ID = [Consignment].OrganizationID ");
                    builder.Append("LEFT JOIN [OrganizationTranslation] ");
                    builder.Append("ON [OrganizationTranslation].OrganizationID = [Organization].ID ");
                    builder.Append("AND [OrganizationTranslation].CultureCode = @Culture ");
                    builder.Append("AND [OrganizationTranslation].Deleted = 0 ");
                    builder.Append("LEFT JOIN [Storage] ");
                    builder.Append("ON [Storage].ID = [Consignment].StorageID ");
                    builder.Append("LEFT JOIN [ConsignmentItemMovementTypeName] ");
                    builder.Append("ON [ConsignmentItemMovementTypeName].ID = ( ");
                    builder.Append("SELECT TOP(1) [JoinTypeName].ID ");
                    builder.Append("FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] ");
                    builder.Append("WHERE [JoinTypeName].MovementType = 4 ");
                    builder.Append(") ");
                    builder.Append("LEFT JOIN [ProductIncomeItem] ");
                    builder.Append("ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID ");
                    builder.Append("LEFT JOIN [SupplyOrderUkraineItem] ");
                    builder.Append("ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID ");
                    builder.Append("LEFT JOIN [SupplyOrderUkraine] ");
                    builder.Append("ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID ");
                    builder.Append("LEFT JOIN [ClientAgreement] ");
                    builder.Append("ON [ClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID ");
                    builder.Append("LEFT JOIN [Agreement] ");
                    builder.Append("ON [Agreement].ID = [ClientAgreement].AgreementID ");
                    builder.Append("LEFT JOIN [Client] ");
                    builder.Append("ON [Client].ID = [ClientAgreement].ClientID ");
                    builder.Append("LEFT JOIN [ConsignmentItem] AS [RootConsignmentItem] ");
                    builder.Append("ON [RootConsignmentItem].ID = [SupplyOrderUkraineItem].ConsignmentItemID ");
                    builder.Append("LEFT JOIN [Consignment] AS [RootConsignment] ");
                    builder.Append("ON [RootConsignment].ID = [RootConsignmentItem].ConsignmentID ");
                    builder.Append("LEFT JOIN [ProductIncome] AS [RootProductIncome] ");
                    builder.Append("ON [RootProductIncome].ID = [RootConsignment].ProductIncomeID ");
                    builder.Append("LEFT JOIN [User] ");
                    builder.Append("ON [User].ID = [ProductIncome].UserID ");
                    builder.Append("WHERE [ConsignmentItemMovement].Deleted = 0 ");
                    builder.Append("AND [ConsignmentItemMovement].MovementType = 4 ");
                    builder.Append("AND [Product].NetUID = @ProductNetId ");

                    if (!consignmentMovementType.Equals(ConsignmentMovementType.All)) {
                        if (consignmentMovementType.Equals(ConsignmentMovementType.Accounting))
                            builder.Append("AND [Storage].[ForVatProducts] = 1 ");
                        else if (consignmentMovementType.Equals(ConsignmentMovementType.Management))
                            builder.Append("AND [Storage].[ForVatProducts] = 0 ");
                    }

                    break;
                case ConsignmentItemMovementType.DepreciatedOrder:
                    builder.Append("SELECT [ProductIncome].Number AS [IncomeDocumentNumber] ");
                    builder.Append(", [ProductIncome].FromDate AS [IncomeDocumentFromDate] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN @Culture = N'uk' ");
                    builder.Append("THEN [ConsignmentItemMovementTypeName].NameUa ");
                    builder.Append("ELSE [ConsignmentItemMovementTypeName].NamePl ");
                    builder.Append("END ");
                    builder.Append(") AS [DocumentType] ");
                    builder.Append(", [DepreciatedOrder].Number AS [DocumentNumber] ");
                    builder.Append(", [DepreciatedOrder].FromDate AS [DocumentFromDate] ");
                    builder.Append(", N'' AS [ClientName] ");
                    builder.Append(", [Storage].[Name] AS [StorageName] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [OrganizationTranslation].[Name] IS NOT NULL ");
                    builder.Append("THEN [OrganizationTranslation].[Name] ");
                    builder.Append("ELSE [Organization].[Name] ");
                    builder.Append("END ");
                    builder.Append(") AS [OrganizationName] ");
                    builder.Append(", [User].LastName AS [Responsible] ");
                    builder.Append(", [ConsignmentItem].Price AS [Price] ");
                    builder.Append(", [ConsignmentItem].NetPrice AS [AccountingPrice] ");
                    builder.Append(", 0 AS [Discount] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [IncomeQty] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [OutcomeQty] ");
                    builder.Append(", [DepreciatedOrder].Comment AS [Comment] ");
                    builder.Append(", 0 AS [IsEdited] ");
                    builder.Append("FROM [ConsignmentItemMovement] ");
                    builder.Append("LEFT JOIN [ConsignmentItem] ");
                    builder.Append("ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID ");
                    builder.Append("LEFT JOIN [Product] ");
                    builder.Append("ON [Product].ID = [ConsignmentItem].ProductID ");
                    builder.Append("LEFT JOIN [Consignment] ");
                    builder.Append("ON [Consignment].ID = [ConsignmentItem].ConsignmentID ");
                    builder.Append("LEFT JOIN [ProductIncome] ");
                    builder.Append("ON [ProductIncome].ID = [Consignment].ProductIncomeID ");
                    builder.Append("LEFT JOIN [Organization] ");
                    builder.Append("ON [Organization].ID = [Consignment].OrganizationID ");
                    builder.Append("LEFT JOIN [OrganizationTranslation] ");
                    builder.Append("ON [OrganizationTranslation].OrganizationID = [Organization].ID ");
                    builder.Append("AND [OrganizationTranslation].CultureCode = @Culture ");
                    builder.Append("AND [OrganizationTranslation].Deleted = 0 ");
                    builder.Append("LEFT JOIN [Storage] ");
                    builder.Append("ON [Storage].ID = [Consignment].StorageID ");
                    builder.Append("LEFT JOIN [ConsignmentItemMovementTypeName] ");
                    builder.Append("ON [ConsignmentItemMovementTypeName].ID = ( ");
                    builder.Append("SELECT TOP(1) [JoinTypeName].ID ");
                    builder.Append("FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] ");
                    builder.Append("WHERE [JoinTypeName].MovementType = 5 ");
                    builder.Append(") ");
                    builder.Append("LEFT JOIN [DepreciatedOrderItem] ");
                    builder.Append("ON [DepreciatedOrderItem].ID = [ConsignmentItemMovement].DepreciatedOrderItemID ");
                    builder.Append("LEFT JOIN [DepreciatedOrder] ");
                    builder.Append("ON [DepreciatedOrder].ID = [DepreciatedOrderItem].DepreciatedOrderID ");
                    builder.Append("LEFT JOIN [User] ");
                    builder.Append("ON [User].ID = [DepreciatedOrder].ResponsibleID ");
                    builder.Append("WHERE [ConsignmentItemMovement].Deleted = 0 ");
                    builder.Append("AND [ConsignmentItemMovement].MovementType = 5 ");
                    builder.Append("AND [Product].NetUID = @ProductNetId ");

                    if (!consignmentMovementType.Equals(ConsignmentMovementType.All)) {
                        if (consignmentMovementType.Equals(ConsignmentMovementType.Accounting)) {
                            builder.Append("AND [Storage].[ForVatProducts] = 1 ");
                            builder.Append("AND [DepreciatedOrder].[IsManagement] = 0 ");
                        } else if (consignmentMovementType.Equals(ConsignmentMovementType.Management)) {
                            builder.Append("AND [DepreciatedOrder].[IsManagement] = 1 ");
                            builder.Append("AND [Storage].[ForVatProducts] = 0 ");
                        }
                    }

                    break;
                case ConsignmentItemMovementType.SupplyReturn:
                    builder.Append("SELECT [ProductIncome].Number AS [IncomeDocumentNumber] ");
                    builder.Append(", [ProductIncome].FromDate AS [IncomeDocumentFromDate] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN @Culture = N'uk' ");
                    builder.Append("THEN [ConsignmentItemMovementTypeName].NameUa ");
                    builder.Append("ELSE [ConsignmentItemMovementTypeName].NamePl ");
                    builder.Append("END ");
                    builder.Append(") AS [DocumentType] ");
                    builder.Append(", [SupplyReturn].Number AS [DocumentNumber] ");
                    builder.Append(", [SupplyReturn].FromDate AS [DocumentFromDate] ");
                    builder.Append(", [Client].FullName AS [ClientName] ");
                    builder.Append(", [Storage].[Name] AS [StorageName] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [OrganizationTranslation].[Name] IS NOT NULL ");
                    builder.Append("THEN [OrganizationTranslation].[Name] ");
                    builder.Append("ELSE [Organization].[Name] ");
                    builder.Append("END ");
                    builder.Append(") AS [OrganizationName] ");
                    builder.Append(", [User].LastName AS [Responsible] ");
                    builder.Append(", [ConsignmentItem].Price AS [Price] ");
                    builder.Append(", [ConsignmentItem].AccountingPrice AS [AccountingPrice] ");
                    builder.Append(", 0 AS [Discount] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [IncomeQty] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [OutcomeQty] ");
                    builder.Append(", [SupplyReturn].Comment AS [Comment] ");
                    builder.Append(", 0 AS [IsEdited] ");
                    builder.Append("FROM [ConsignmentItemMovement] ");
                    builder.Append("LEFT JOIN [ConsignmentItem] ");
                    builder.Append("ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID ");
                    builder.Append("LEFT JOIN [Product] ");
                    builder.Append("ON [Product].ID = [ConsignmentItem].ProductID ");
                    builder.Append("LEFT JOIN [Consignment] ");
                    builder.Append("ON [Consignment].ID = [ConsignmentItem].ConsignmentID ");
                    builder.Append("LEFT JOIN [ProductIncome] ");
                    builder.Append("ON [ProductIncome].ID = [Consignment].ProductIncomeID ");
                    builder.Append("LEFT JOIN [Organization] ");
                    builder.Append("ON [Organization].ID = [Consignment].OrganizationID ");
                    builder.Append("LEFT JOIN [OrganizationTranslation] ");
                    builder.Append("ON [OrganizationTranslation].OrganizationID = [Organization].ID ");
                    builder.Append("AND [OrganizationTranslation].CultureCode = @Culture ");
                    builder.Append("AND [OrganizationTranslation].Deleted = 0 ");
                    builder.Append("LEFT JOIN [Storage] ");
                    builder.Append("ON [Storage].ID = [Consignment].StorageID ");
                    builder.Append("LEFT JOIN [ConsignmentItemMovementTypeName] ");
                    builder.Append("ON [ConsignmentItemMovementTypeName].ID = ( ");
                    builder.Append("SELECT TOP(1) [JoinTypeName].ID ");
                    builder.Append("FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] ");
                    builder.Append("WHERE [JoinTypeName].MovementType = 6 ");
                    builder.Append(") ");
                    builder.Append("LEFT JOIN [SupplyReturnItem] ");
                    builder.Append("ON [SupplyReturnItem].ID = [ConsignmentItemMovement].SupplyReturnItemID ");
                    builder.Append("LEFT JOIN [SupplyReturn] ");
                    builder.Append("ON [SupplyReturn].ID = [SupplyReturnItem].SupplyReturnID ");
                    builder.Append("LEFT JOIN [ClientAgreement] ");
                    builder.Append("ON [ClientAgreement].ID = [SupplyReturnItem].ConsignmentItemID ");
                    builder.Append("LEFT JOIN [Agreement] ");
                    builder.Append("ON [Agreement].ID = [ClientAgreement].AgreementID ");
                    builder.Append("LEFT JOIN [Client] ");
                    builder.Append("ON [Client].ID = [ClientAgreement].ClientID ");
                    builder.Append("LEFT JOIN [User] ");
                    builder.Append("ON [User].ID = [SupplyReturn].ResponsibleID ");
                    builder.Append("WHERE [ConsignmentItemMovement].Deleted = 0 ");
                    builder.Append("AND [ConsignmentItemMovement].MovementType = 6 ");
                    builder.Append("AND [Product].NetUID = @ProductNetId ");

                    if (!consignmentMovementType.Equals(ConsignmentMovementType.All)) {
                        if (consignmentMovementType.Equals(ConsignmentMovementType.Accounting))
                            builder.Append("AND [SupplyReturn].[IsManagement] = 0 ");
                        else if (consignmentMovementType.Equals(ConsignmentMovementType.Management))
                            builder.Append("AND [SupplyReturn].[IsManagement] = 1 ");
                    }

                    break;
                case ConsignmentItemMovementType.ProductTransfer:
                    builder.Append("SELECT [ProductIncome].Number AS [IncomeDocumentNumber] ");
                    builder.Append(", [ProductIncome].FromDate AS [IncomeDocumentFromDate] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN @Culture = N'uk' ");
                    builder.Append("THEN [ConsignmentItemMovementTypeName].NameUa ");
                    builder.Append("ELSE [ConsignmentItemMovementTypeName].NamePl ");
                    builder.Append("END ");
                    builder.Append(") AS [DocumentType] ");
                    builder.Append(", [ProductTransfer].Number AS [DocumentNumber] ");
                    builder.Append(", [ProductTransfer].FromDate AS [DocumentFromDate] ");
                    builder.Append(", N'' AS [ClientName] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 ");
                    builder.Append("THEN [ToStorage].[Name] ");
                    builder.Append("ELSE [FromStorage].[Name] ");
                    builder.Append("END ");
                    builder.Append(") AS [StorageName] ");

                    builder.Append(", CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 ");
                    builder.Append("THEN ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ToOrganizationTranslation].[Name] IS NOT NULL ");
                    builder.Append("THEN [ToOrganizationTranslation].[Name] ");
                    builder.Append("ELSE [ToOrganization].[Name] ");
                    builder.Append("END) ");
                    builder.Append(" ELSE ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [FromOrganizationTranslation].[Name] IS NOT NULL ");
                    builder.Append("THEN [FromOrganizationTranslation].[Name] ");
                    builder.Append("ELSE [FromOrganization].[Name] ");
                    builder.Append("END) ");
                    builder.Append("END AS [OrganizationName] ");

                    builder.Append(", [User].LastName AS [Responsible] ");
                    builder.Append(", [ConsignmentItem].Price AS [Price] ");
                    builder.Append(", [ConsignmentItem].AccountingPrice AS [AccountingPrice] ");
                    builder.Append(", 0 AS [Discount] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [IncomeQty] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [OutcomeQty] ");
                    builder.Append(", [ProductTransfer].Comment AS [Comment] ");
                    builder.Append(", 0 AS [IsEdited] ");
                    builder.Append("FROM [ConsignmentItemMovement] ");
                    builder.Append("LEFT JOIN [ConsignmentItem] ");
                    builder.Append("ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID ");
                    builder.Append("LEFT JOIN [Product] ");
                    builder.Append("ON [Product].ID = [ConsignmentItem].ProductID ");
                    builder.Append("LEFT JOIN [Consignment] ");
                    builder.Append("ON [Consignment].ID = [ConsignmentItem].ConsignmentID ");
                    builder.Append("LEFT JOIN [ProductIncome] ");
                    builder.Append("ON [ProductIncome].ID = [Consignment].ProductIncomeID ");
                    builder.Append("LEFT JOIN [Storage] ");
                    builder.Append("ON [Storage].ID = [Consignment].StorageID ");
                    builder.Append("LEFT JOIN [ConsignmentItemMovementTypeName] ");
                    builder.Append("ON [ConsignmentItemMovementTypeName].ID = ( ");
                    builder.Append("SELECT TOP(1) [JoinTypeName].ID ");
                    builder.Append("FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] ");
                    builder.Append("WHERE [JoinTypeName].MovementType = 7 ");
                    builder.Append(") ");
                    builder.Append("LEFT JOIN [ProductTransferItem] ");
                    builder.Append("ON [ProductTransferItem].ID = [ConsignmentItemMovement].ProductTransferItemID ");
                    builder.Append("LEFT JOIN [ProductTransfer] ");
                    builder.Append("ON [ProductTransfer].ID = [ProductTransferItem].ProductTransferID ");
                    builder.Append("LEFT JOIN [User] ");
                    builder.Append("ON [User].ID = [ProductTransfer].ResponsibleID ");
                    builder.Append("LEFT JOIN [Storage] AS [FromStorage] ");
                    builder.Append("ON [FromStorage].ID = [ProductTransfer].FromStorageID ");
                    builder.Append("LEFT JOIN [Storage] AS [ToStorage] ");
                    builder.Append("ON [ToStorage].ID = [ProductTransfer].ToStorageID ");
                    builder.Append("LEFT JOIN [Organization] AS [FromOrganization] ");
                    builder.Append("ON [FromOrganization].ID = [FromStorage].OrganizationID ");
                    builder.Append("LEFT JOIN [OrganizationTranslation] AS [FromOrganizationTranslation] ");
                    builder.Append("ON [FromOrganizationTranslation].OrganizationID = [FromOrganization].ID ");
                    builder.Append("AND [FromOrganizationTranslation].CultureCode = @Culture ");
                    builder.Append("AND [FromOrganizationTranslation].Deleted = 0 ");
                    builder.Append("LEFT JOIN [Organization] AS [ToOrganization] ");
                    builder.Append("ON [ToOrganization].ID = [ToStorage].OrganizationID ");
                    builder.Append("LEFT JOIN [OrganizationTranslation] AS [ToOrganizationTranslation] ");
                    builder.Append("ON [ToOrganizationTranslation].OrganizationID = [ToOrganization].ID ");
                    builder.Append("AND [ToOrganizationTranslation].CultureCode = @Culture ");
                    builder.Append("AND [ToOrganizationTranslation].Deleted = 0 ");
                    builder.Append("WHERE [ConsignmentItemMovement].Deleted = 0 ");
                    builder.Append("AND [ConsignmentItemMovement].MovementType = 7 ");
                    builder.Append("AND [Product].NetUID = @ProductNetId ");

                    if (!consignmentMovementType.Equals(ConsignmentMovementType.All)) {
                        if (consignmentMovementType.Equals(ConsignmentMovementType.Accounting)) {
                            builder.Append("AND [Storage].[ForVatProducts] = 1 ");
                        } else if (consignmentMovementType.Equals(ConsignmentMovementType.Management)) {
                            builder.Append("AND ([ProductTransfer].[IsManagement] = 1 OR ");
                            builder.Append("[Storage].[ForVatProducts] = 0) ");
                        }
                    }

                    break;
                case ConsignmentItemMovementType.Export:
                    builder.Append("SELECT [ProductIncome].Number AS [IncomeDocumentNumber] ");
                    builder.Append(", [ProductIncome].FromDate AS [IncomeDocumentFromDate] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN @Culture = N'uk' ");
                    builder.Append("THEN [ConsignmentItemMovementTypeName].NameUa ");
                    builder.Append("ELSE [ConsignmentItemMovementTypeName].NamePl ");
                    builder.Append("END ");
                    builder.Append(") AS [DocumentType] ");
                    builder.Append(", [Sad].Number AS [DocumentNumber] ");
                    builder.Append(", [Sad].FromDate AS [DocumentFromDate] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [Sad].ClientID IS NOT NULL ");
                    builder.Append("THEN [Client].FullName ");
                    builder.Append("ELSE [OrganizationClient].FullName ");
                    builder.Append("END ");
                    builder.Append(") AS [ClientName] ");
                    builder.Append(", [Storage].[Name] AS [StorageName] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [OrganizationTranslation].[Name] IS NOT NULL ");
                    builder.Append("THEN [OrganizationTranslation].[Name] ");
                    builder.Append("ELSE [Organization].[Name] ");
                    builder.Append("END ");
                    builder.Append(") AS [OrganizationName] ");
                    builder.Append(", [User].LastName AS [Responsible] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [SupplyOrderUkraineCartItem].ID IS NOT NULL ");
                    builder.Append("THEN ROUND([SupplyOrderUkraineCartItem].UnitPrice + [SupplyOrderUkraineCartItem].UnitPrice * [Sad].MarginAmount / 100, 2) ");
                    builder.Append("ELSE ROUND([SadItem].UnitPrice + [SadItem].UnitPrice * [Sad].MarginAmount / 100, 2) ");
                    builder.Append("END ");
                    builder.Append(") AS [Price] ");
                    builder.Append(", 0 AS [AccountingPrice] ");
                    builder.Append(", 0 AS [Discount] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [IncomeQty] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [OutcomeQty] ");
                    builder.Append(", [Sad].Comment AS [Comment] ");
                    builder.Append(", 0 AS [IsEdited] ");
                    builder.Append("FROM [ConsignmentItemMovement] ");
                    builder.Append("LEFT JOIN [ConsignmentItem] ");
                    builder.Append("ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID ");
                    builder.Append("LEFT JOIN [Product] ");
                    builder.Append("ON [Product].ID = [ConsignmentItem].ProductID ");
                    builder.Append("LEFT JOIN [Consignment] ");
                    builder.Append("ON [Consignment].ID = [ConsignmentItem].ConsignmentID ");
                    builder.Append("LEFT JOIN [ProductIncome] ");
                    builder.Append("ON [ProductIncome].ID = [Consignment].ProductIncomeID ");
                    builder.Append("LEFT JOIN [Organization] ");
                    builder.Append("ON [Organization].ID = [Consignment].OrganizationID ");
                    builder.Append("LEFT JOIN [OrganizationTranslation] ");
                    builder.Append("ON [OrganizationTranslation].OrganizationID = [Organization].ID ");
                    builder.Append("AND [OrganizationTranslation].CultureCode = @Culture ");
                    builder.Append("AND [OrganizationTranslation].Deleted = 0 ");
                    builder.Append("LEFT JOIN [Storage] ");
                    builder.Append("ON [Storage].ID = [Consignment].StorageID ");
                    builder.Append("LEFT JOIN [ConsignmentItemMovementTypeName] ");
                    builder.Append("ON [ConsignmentItemMovementTypeName].ID = ( ");
                    builder.Append("SELECT TOP(1) [JoinTypeName].ID ");
                    builder.Append("FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] ");
                    builder.Append("WHERE [JoinTypeName].MovementType = 8 ");
                    builder.Append(") ");
                    builder.Append("LEFT JOIN [SadItem] ");
                    builder.Append("ON [SadItem].ID = [ConsignmentItemMovement].SadItemID ");
                    builder.Append("LEFT JOIN [SupplyOrderUkraineCartItem] ");
                    builder.Append("ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID ");
                    builder.Append("LEFT JOIN [Sad] ");
                    builder.Append("ON [Sad].ID = [SadItem].SadID ");
                    builder.Append("LEFT JOIN [User] ");
                    builder.Append("ON [User].ID = [Sad].ResponsibleID ");
                    builder.Append("LEFT JOIN [Client] ");
                    builder.Append("ON [Client].ID = [Sad].ClientID ");
                    builder.Append("LEFT JOIN [OrganizationClient] ");
                    builder.Append("ON [OrganizationClient].ID = [Sad].OrganizationClientID ");
                    builder.Append("LEFT JOIN [ClientAgreement] ");
                    builder.Append("ON [ClientAgreement].ID = [Sad].ClientAgreementID ");
                    builder.Append("LEFT JOIN [Agreement] ");
                    builder.Append("ON [Agreement].ID = [ClientAgreement].AgreementID ");
                    builder.Append("WHERE [ConsignmentItemMovement].Deleted = 0 ");
                    builder.Append("AND [ConsignmentItemMovement].MovementType = 8 ");
                    builder.Append("AND [Product].NetUID = @ProductNetId ");

                    break;
                case ConsignmentItemMovementType.TaxFree:
                    builder.Append("SELECT [ProductIncome].Number AS [IncomeDocumentNumber] ");
                    builder.Append(", [ProductIncome].FromDate AS [IncomeDocumentFromDate] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN @Culture = N'uk' ");
                    builder.Append("THEN [ConsignmentItemMovementTypeName].NameUa ");
                    builder.Append("ELSE [ConsignmentItemMovementTypeName].NamePl ");
                    builder.Append("END ");
                    builder.Append(") AS [DocumentType] ");
                    builder.Append(", [TaxFree].Number AS [DocumentNumber] ");
                    builder.Append(", [TaxFreePackList].FromDate AS [DocumentFromDate] ");
                    builder.Append(", [Client].FullName AS [ClientName] ");
                    builder.Append(", [Storage].[Name] AS [StorageName] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [OrganizationTranslation].[Name] IS NOT NULL ");
                    builder.Append("THEN [OrganizationTranslation].[Name] ");
                    builder.Append("ELSE [Organization].[Name] ");
                    builder.Append("END ");
                    builder.Append(") AS [OrganizationName] ");
                    builder.Append(", [User].LastName AS [Responsible] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [SupplyOrderUkraineCartItem].ID IS NOT NULL ");
                    builder.Append("THEN ROUND([SupplyOrderUkraineCartItem].UnitPrice + [SupplyOrderUkraineCartItem].UnitPrice * [TaxFreePackList].MarginAmount / 100, 2) ");
                    builder.Append("ELSE [OrderItem].PricePerItem ");
                    builder.Append("END ");
                    builder.Append(") AS [Price] ");
                    builder.Append(", 0 AS [AccountingPrice] ");
                    builder.Append(", 0 AS [Discount] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [IncomeQty] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [OutcomeQty] ");
                    builder.Append(", N'' AS [Comment] ");
                    builder.Append(", 0 AS [IsEdited] ");
                    builder.Append("FROM [ConsignmentItemMovement] ");
                    builder.Append("LEFT JOIN [ConsignmentItem] ");
                    builder.Append("ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID ");
                    builder.Append("LEFT JOIN [Product] ");
                    builder.Append("ON [Product].ID = [ConsignmentItem].ProductID ");
                    builder.Append("LEFT JOIN [Consignment] ");
                    builder.Append("ON [Consignment].ID = [ConsignmentItem].ConsignmentID ");
                    builder.Append("LEFT JOIN [ProductIncome] ");
                    builder.Append("ON [ProductIncome].ID = [Consignment].ProductIncomeID ");
                    builder.Append("LEFT JOIN [Organization] ");
                    builder.Append("ON [Organization].ID = [Consignment].OrganizationID ");
                    builder.Append("LEFT JOIN [OrganizationTranslation] ");
                    builder.Append("ON [OrganizationTranslation].OrganizationID = [Organization].ID ");
                    builder.Append("AND [OrganizationTranslation].CultureCode = @Culture ");
                    builder.Append("AND [OrganizationTranslation].Deleted = 0 ");
                    builder.Append("LEFT JOIN [Storage] ");
                    builder.Append("ON [Storage].ID = [Consignment].StorageID ");
                    builder.Append("LEFT JOIN [ConsignmentItemMovementTypeName] ");
                    builder.Append("ON [ConsignmentItemMovementTypeName].ID = ( ");
                    builder.Append("SELECT TOP(1) [JoinTypeName].ID ");
                    builder.Append("FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] ");
                    builder.Append("WHERE [JoinTypeName].MovementType = 9 ");
                    builder.Append(") ");
                    builder.Append("LEFT JOIN [TaxFreeItem] ");
                    builder.Append("ON [TaxFreeItem].ID = [ConsignmentItemMovement].TaxFreeItemID ");
                    builder.Append("LEFT JOIN [SupplyOrderUkraineCartItem] ");
                    builder.Append("ON [SupplyOrderUkraineCartItem].ID = [TaxFreeItem].SupplyOrderUkraineCartItemID ");
                    builder.Append("LEFT JOIN [TaxFreePackListOrderItem] ");
                    builder.Append("ON [TaxFreePackListOrderItem].ID = [TaxFreeItem].TaxFreePackListOrderItemID ");
                    builder.Append("LEFT JOIN [OrderItem] ");
                    builder.Append("ON [OrderItem].ID = [TaxFreePackListOrderItem].OrderItemID ");
                    builder.Append("LEFT JOIN [TaxFree] ");
                    builder.Append("ON [TaxFree].ID = [TaxFreeItem].TaxFreeID ");
                    builder.Append("LEFT JOIN [TaxFreePackList] ");
                    builder.Append("ON [TaxFreePackList].ID = [TaxFree].TaxFreePackListID ");
                    builder.Append("LEFT JOIN [Client] ");
                    builder.Append("ON [Client].ID = [TaxFreePackList].ClientID ");
                    builder.Append("LEFT JOIN [User] ");
                    builder.Append("ON [User].ID = [TaxFree].ResponsibleID ");
                    builder.Append("WHERE [ConsignmentItemMovement].Deleted = 0 ");
                    builder.Append("AND [ConsignmentItemMovement].MovementType = 9 ");
                    builder.Append("AND [Product].NetUID = @ProductNetId ");

                    break;
                case ConsignmentItemMovementType.CartItem:
                    builder.Append("SELECT N'' AS [IncomeDocumentNumber] ");
                    builder.Append(", NULL AS [IncomeDocumentFromDate] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN @Culture = N'uk' ");
                    builder.Append("THEN [ConsignmentItemMovementTypeName].NameUa ");
                    builder.Append("ELSE [ConsignmentItemMovementTypeName].NamePl ");
                    builder.Append("END ");
                    builder.Append(") AS [DocumentType] ");
                    builder.Append(", N'' AS [DocumentNumber] ");
                    builder.Append(", [SupplyOrderUkraineCartItem].FromDate AS [DocumentFromDate] ");
                    builder.Append(", N'' AS [ClientName] ");
                    builder.Append(", N'' AS [StorageName] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [OrganizationTranslation].[Name] IS NOT NULL ");
                    builder.Append("THEN [OrganizationTranslation].[Name] ");
                    builder.Append("ELSE [Organization].[Name] ");
                    builder.Append("END ");
                    builder.Append(") AS [OrganizationName] ");
                    builder.Append(", [User].LastName AS [Responsible] ");
                    builder.Append(", 0 AS [Price] ");
                    builder.Append(", 0 AS [AccountingPrice] ");
                    builder.Append(", 0 AS [Discount] ");
                    builder.Append(", 0 AS [IncomeQty] ");
                    builder.Append(", [SupplyOrderUkraineCartItemReservation].Qty AS [OutcomeQty] ");
                    builder.Append(", N'' AS [Comment] ");
                    builder.Append(", 0 AS [IsEdited] ");
                    builder.Append("FROM [SupplyOrderUkraineCartItem] ");
                    builder.Append("LEFT JOIN [SadItem] ");
                    builder.Append("ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID ");
                    builder.Append("LEFT JOIN [ConsignmentItemMovementTypeName] ");
                    builder.Append("ON [ConsignmentItemMovementTypeName].ID = ( ");
                    builder.Append("SELECT TOP(1) [JoinTypeName].ID ");
                    builder.Append("FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] ");
                    builder.Append("WHERE [JoinTypeName].MovementType = 10 ");
                    builder.Append(") ");
                    builder.Append("LEFT JOIN [SupplyOrderUkraineCartItemReservation] ");
                    builder.Append("ON [SupplyOrderUkraineCartItemReservation].SupplyOrderUkraineCartItemID = [SupplyOrderUkraineCartItem].ID ");
                    builder.Append("AND [SupplyOrderUkraineCartItemReservation].Deleted = 0 ");
                    builder.Append("LEFT JOIN [ProductAvailability] ");
                    builder.Append("ON [ProductAvailability].ID = [SupplyOrderUkraineCartItemReservation].ProductAvailabilityID ");
                    builder.Append("LEFT JOIN [Storage] ");
                    builder.Append("ON [Storage].ID = [ProductAvailability].StorageID ");
                    builder.Append("LEFT JOIN [Organization] ");
                    builder.Append("ON [Organization].ID = [Storage].OrganizationID ");
                    builder.Append("LEFT JOIN [OrganizationTranslation] ");
                    builder.Append("ON [OrganizationTranslation].OrganizationID = [Organization].ID ");
                    builder.Append("AND [OrganizationTranslation].CultureCode = @Culture ");
                    builder.Append("AND [OrganizationTranslation].Deleted = 0 ");
                    builder.Append("LEFT JOIN [User] ");
                    builder.Append("ON [User].ID = ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [SupplyOrderUkraineCartItem].ResponsibleID IS NOT NULL ");
                    builder.Append("THEN [SupplyOrderUkraineCartItem].ResponsibleID ");
                    builder.Append("WHEN [SupplyOrderUkraineCartItem].UpdatedByID IS NOT NULL ");
                    builder.Append("THEN [SupplyOrderUkraineCartItem].UpdatedByID ");
                    builder.Append("ELSE [SupplyOrderUkraineCartItem].CreatedByID ");
                    builder.Append("END ");
                    builder.Append(") ");
                    builder.Append("LEFT JOIN [Product] ");
                    builder.Append("ON [Product].ID = [SupplyOrderUkraineCartItem].ProductID ");
                    builder.Append("WHERE [SupplyOrderUkraineCartItem].Deleted = 0 ");
                    builder.Append("AND [SupplyOrderUkraineCartItem].TaxFreePackListID IS NULL ");
                    builder.Append("AND [SadItem].ID IS NULL ");
                    builder.Append("AND [Product].NetUID = @ProductNetId ");

                    if (!consignmentMovementType.Equals(ConsignmentMovementType.All)) {
                        if (consignmentMovementType.Equals(ConsignmentMovementType.Accounting))
                            builder.Append("AND [Organization].[VatRateID] IS NOT NULL ");
                        else if (consignmentMovementType.Equals(ConsignmentMovementType.Management))
                            builder.Append("AND [Organization].[VatRateID] IS NULL ");
                    }

                    break;
                case ConsignmentItemMovementType.Capitalization:
                    builder.Append("SELECT N'' AS [IncomeDocumentNumber] ");
                    builder.Append(", NULL AS [IncomeDocumentFromDate] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN @Culture = N'uk' ");
                    builder.Append("THEN [ConsignmentItemMovementTypeName].NameUa ");
                    builder.Append("ELSE [ConsignmentItemMovementTypeName].NamePl ");
                    builder.Append("END ");
                    builder.Append(") AS [DocumentType] ");
                    builder.Append(", [ProductIncome].Number AS [DocumentNumber] ");
                    builder.Append(", [ProductIncome].FromDate AS [DocumentFromDate] ");
                    builder.Append(", N'' AS [ClientName] ");
                    builder.Append(", [Storage].[Name] AS [StorageName] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [OrganizationTranslation].[Name] IS NOT NULL ");
                    builder.Append("THEN [OrganizationTranslation].[Name] ");
                    builder.Append("ELSE [Organization].[Name] ");
                    builder.Append("END ");
                    builder.Append(") AS [OrganizationName] ");
                    builder.Append(", [User].LastName AS [Responsible] ");
                    builder.Append(", [ConsignmentItem].Price AS [Price] ");
                    builder.Append(", [ConsignmentItem].AccountingPrice AS [AccountingPrice] ");
                    builder.Append(", 0 AS [Discount] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [IncomeQty] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [OutcomeQty] ");
                    builder.Append(", [ProductCapitalization].Comment AS [Comment] ");
                    builder.Append(", 0 AS [IsEdited] ");
                    builder.Append("FROM [ConsignmentItemMovement] ");
                    builder.Append("LEFT JOIN [ConsignmentItem] ");
                    builder.Append("ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID ");
                    builder.Append("LEFT JOIN [Product] ");
                    builder.Append("ON [Product].ID = [ConsignmentItem].ProductID ");
                    builder.Append("LEFT JOIN [Consignment] ");
                    builder.Append("ON [Consignment].ID = [ConsignmentItem].ConsignmentID ");
                    builder.Append("LEFT JOIN [ProductIncome] ");
                    builder.Append("ON [ProductIncome].ID = [Consignment].ProductIncomeID ");
                    builder.Append("LEFT JOIN [Organization] ");
                    builder.Append("ON [Organization].ID = [Consignment].OrganizationID ");
                    builder.Append("LEFT JOIN [OrganizationTranslation] ");
                    builder.Append("ON [OrganizationTranslation].OrganizationID = [Organization].ID ");
                    builder.Append("AND [OrganizationTranslation].CultureCode = @Culture ");
                    builder.Append("AND [OrganizationTranslation].Deleted = 0 ");
                    builder.Append("LEFT JOIN [Storage] ");
                    builder.Append("ON [Storage].ID = [Consignment].StorageID ");
                    builder.Append("LEFT JOIN [ConsignmentItemMovementTypeName] ");
                    builder.Append("ON [ConsignmentItemMovementTypeName].ID = ( ");
                    builder.Append("SELECT TOP(1) [JoinTypeName].ID ");
                    builder.Append("FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] ");
                    builder.Append("WHERE [JoinTypeName].MovementType = 11 ");
                    builder.Append(") ");
                    builder.Append("LEFT JOIN [ProductIncomeItem] ");
                    builder.Append("ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID ");
                    builder.Append("LEFT JOIN [ProductCapitalizationItem] ");
                    builder.Append("ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID ");
                    builder.Append("LEFT JOIN [ProductCapitalization] ");
                    builder.Append("ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID ");
                    builder.Append("LEFT JOIN [User] ");
                    builder.Append("ON [User].ID = [ProductCapitalization].ResponsibleID ");
                    builder.Append("WHERE [ConsignmentItemMovement].Deleted = 0 ");
                    builder.Append("AND [ConsignmentItemMovement].MovementType = 11 ");
                    builder.Append("AND [Product].NetUID = @ProductNetId ");

                    if (!consignmentMovementType.Equals(ConsignmentMovementType.All)) {
                        if (consignmentMovementType.Equals(ConsignmentMovementType.Accounting))
                            builder.Append("AND [Storage].[ForVatProducts] = 1 ");
                        else if (consignmentMovementType.Equals(ConsignmentMovementType.Management))
                            builder.Append("AND [Storage].[ForVatProducts] = 0 ");
                    }


                    break;
                case ConsignmentItemMovementType.ShiftingStorage:
                default:
                    builder.Append("SELECT [ProductIncome].Number AS [IncomeDocumentNumber] ");
                    builder.Append(", [ProductIncome].FromDate AS [IncomeDocumentFromDate] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN @Culture = N'uk' ");
                    builder.Append("THEN [ConsignmentItemMovementTypeName].NameUa ");
                    builder.Append("ELSE [ConsignmentItemMovementTypeName].NamePl ");
                    builder.Append("END ");
                    builder.Append(") AS [DocumentType] ");
                    builder.Append(", [SaleNumber].[Value] AS [DocumentNumber] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [Sale].ChangedToInvoice IS NOT NULL ");
                    builder.Append("THEN [Sale].ChangedToInvoice ");
                    builder.Append("ELSE [Sale].Updated ");
                    builder.Append("END ");
                    builder.Append(") AS [DocumentFromDate] ");
                    builder.Append(", [Client].FullName AS [ClientName] ");
                    builder.Append(", [Storage].[Name] AS [StorageName] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [OrganizationTranslation].[Name] IS NOT NULL ");
                    builder.Append("THEN [OrganizationTranslation].[Name] ");
                    builder.Append("ELSE [Organization].[Name] ");
                    builder.Append("END ");
                    builder.Append(") AS [OrganizationName] ");
                    builder.Append(", [User].LastName AS [Responsible] ");
                    builder.Append(", [OrderItem].PricePerItem AS [Price] ");
                    builder.Append(", 0 AS [AccountingPrice] ");
                    builder.Append(", ROUND([OrderItem].OneTimeDiscount + [OrderItem].DiscountAmount, 2) AS [Discount] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [IncomeQty] ");
                    builder.Append(", ( ");
                    builder.Append("CASE ");
                    builder.Append("WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 ");
                    builder.Append("THEN [ConsignmentItemMovement].Qty ");
                    builder.Append("ELSE 0 ");
                    builder.Append("END ");
                    builder.Append(") AS [OutcomeQty] ");
                    builder.Append(", [OrderItemBaseShiftStatus].Comment AS [Comment] ");
                    builder.Append(", 0 AS [IsEdited] ");
                    builder.Append("FROM [ConsignmentItemMovement] ");
                    builder.Append("LEFT JOIN [ConsignmentItem] ");
                    builder.Append("ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID ");
                    builder.Append("LEFT JOIN [Product] ");
                    builder.Append("ON [Product].ID = [ConsignmentItem].ProductID ");
                    builder.Append("LEFT JOIN [Consignment] ");
                    builder.Append("ON [Consignment].ID = [ConsignmentItem].ConsignmentID ");
                    builder.Append("LEFT JOIN [ProductIncome] ");
                    builder.Append("ON [ProductIncome].ID = [Consignment].ProductIncomeID ");
                    builder.Append("LEFT JOIN [Organization] ");
                    builder.Append("ON [Organization].ID = [Consignment].OrganizationID ");
                    builder.Append("LEFT JOIN [OrganizationTranslation] ");
                    builder.Append("ON [OrganizationTranslation].OrganizationID = [Organization].ID ");
                    builder.Append("AND [OrganizationTranslation].CultureCode = @Culture ");
                    builder.Append("AND [OrganizationTranslation].Deleted = 0 ");
                    builder.Append("LEFT JOIN [Storage] ");
                    builder.Append("ON [Storage].ID = [Consignment].StorageID ");
                    builder.Append("LEFT JOIN [ConsignmentItemMovementTypeName] ");
                    builder.Append("ON [ConsignmentItemMovementTypeName].ID = ( ");
                    builder.Append("SELECT TOP(1) [JoinTypeName].ID ");
                    builder.Append("FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] ");
                    builder.Append("WHERE [JoinTypeName].MovementType = 12 ");
                    builder.Append(") ");
                    builder.Append("LEFT JOIN [OrderItemBaseShiftStatus] ");
                    builder.Append("ON [OrderItemBaseShiftStatus].ID = [ConsignmentItemMovement].OrderItemBaseShiftStatusID ");
                    builder.Append("LEFT JOIN [OrderItem] ");
                    builder.Append("ON [OrderItem].ID = [OrderItemBaseShiftStatus].OrderItemID ");
                    builder.Append("LEFT JOIN [Sale] ");
                    builder.Append("ON [Sale].OrderID = [OrderItem].OrderID ");
                    builder.Append("LEFT JOIN [SaleNumber] ");
                    builder.Append("ON [SaleNumber].ID = [Sale].SaleNumberID ");
                    builder.Append("LEFT JOIN [ClientAgreement] ");
                    builder.Append("ON [ClientAgreement].ID = [Sale].ClientAgreementID ");
                    builder.Append("LEFT JOIN [Client] ");
                    builder.Append("ON [Client].ID = [ClientAgreement].ClientID ");
                    builder.Append("LEFT JOIN [User] ");
                    builder.Append("ON [User].ID = [OrderItemBaseShiftStatus].UserID ");
                    builder.Append("WHERE [ConsignmentItemMovement].Deleted = 0 ");
                    builder.Append("AND [ConsignmentItemMovement].MovementType = 12 ");
                    builder.Append("AND [Product].NetUID = @ProductNetId ");

                    if (!consignmentMovementType.Equals(ConsignmentMovementType.All)) {
                        if (consignmentMovementType.Equals(ConsignmentMovementType.Accounting))
                            builder.Append("AND [Sale].[IsVatSale] = 1 ");
                        else if (consignmentMovementType.Equals(ConsignmentMovementType.Management))
                            builder.Append("AND [Sale].[IsVatSale] = 0 ");
                    }

                    break;
            }

            firstType = false;
        }

        builder.Append(") ");
        builder.Append("SELECT * ");
        builder.Append("FROM [MovementConsignmentInfo_CTE] ");
        builder.Append("ORDER BY [MovementConsignmentInfo_CTE].DocumentFromDate, [MovementConsignmentInfo_CTE].DocumentType DESC ");

        return _connection.Query<MovementConsignmentInfo>(
            builder.ToString(),
            new {
                ProductNetId = productNetId,
                From = from,
                To = to,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );
    }

    public IEnumerable<MovementConsignmentInfo> GetFullMovementConsignmentInfoByConsignmentItemNetId(
        Guid consignmentItemNetId,
        DateTime from,
        DateTime to) {
        return _connection.Query<MovementConsignmentInfo>(
            "DECLARE @ConsignmentHierarchy TABLE ( " +
            "ID bigint " +
            "); " +
            ";WITH [SearchRootItem_CTE] " +
            "AS ( " +
            "SELECT ID, RootConsignmentItemID " +
            "FROM [ConsignmentItem] " +
            "WHERE [ConsignmentItem].NetUID = @ConsignmentItemNetId " +
            "UNION ALL " +
            "SELECT [ConsignmentItem].ID, [ConsignmentItem].RootConsignmentItemID " +
            "FROM [ConsignmentItem] " +
            "INNER JOIN [SearchRootItem_CTE] AS [Child] " +
            "ON [Child].RootConsignmentItemID = [ConsignmentItem].ID " +
            ") " +
            ", [ChildConsignmentItems_CTE] " +
            "AS ( " +
            "SELECT [ConsignmentItem].ID " +
            "FROM [ConsignmentItem] " +
            "WHERE [ConsignmentItem].ID = ( " +
            "SELECT TOP(1) [RootItem].ID " +
            "FROM [SearchRootItem_CTE] AS [RootItem] " +
            "WHERE [RootItem].RootConsignmentItemID IS NULL " +
            ") " +
            "UNION ALL " +
            "SELECT [ConsignmentItem].ID " +
            "FROM [ConsignmentItem] " +
            "INNER JOIN [ChildConsignmentItems_CTE] AS [RootItem] " +
            "ON [RootItem].ID = [ConsignmentItem].RootConsignmentItemID " +
            ") " +
            "INSERT INTO @ConsignmentHierarchy (ID) " +
            "SELECT [ConsignmentItem].ID " +
            "FROM [ConsignmentItem] " +
            "WHERE ID IN ( " +
            "SELECT [ChildConsignmentItems_CTE].ID " +
            "FROM [ChildConsignmentItems_CTE] " +
            ") " +
            ";WITH [MovementConsignmentInfo_CTE] " +
            "AS ( " +
            "SELECT [ProductIncome].Number AS [IncomeDocumentNumber] " +
            ", [ProductIncome].FromDate AS [IncomeDocumentFromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentType] " +
            ", [SaleNumber].[Value] AS [DocumentNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [Sale].ChangedToInvoice IS NOT NULL " +
            "THEN [Sale].ChangedToInvoice " +
            "ELSE [Sale].Updated " +
            "END " +
            ") AS [DocumentFromDate] " +
            ", [Client].FullName AS [ClientName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [User].LastName AS [Responsible] " +
            ", [OrderItem].PricePerItem AS [Price] " +
            ", ROUND([OrderItem].OneTimeDiscount + [OrderItem].DiscountAmount, 2) AS [Discount] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [IncomeQty] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [OutcomeQty] " +
            ", [Sale].Comment AS [Comment] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 0 " +
            ") " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [ConsignmentItemMovement].OrderItemID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [OrderItem].OrderID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = ( " +
            "CASE " +
            "WHEN [Sale].ChangedToInvoiceByID IS NOT NULL " +
            "THEN [Sale].ChangedToInvoiceByID " +
            "ELSE [Sale].UserID " +
            "END " +
            ") " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 0 " +
            "AND [ConsignmentItemMovement].ConsignmentItemID IN ( " +
            "SELECT [HierarchyItems].ID FROM @ConsignmentHierarchy AS [HierarchyItems] " +
            ") " +
            "UNION ALL " +
            "SELECT [RootProductIncome].Number AS [IncomeDocumentNumber] " +
            ", [RootProductIncome].FromDate AS [IncomeDocumentFromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentType] " +
            ", [SaleReturn].Number AS [DocumentNumber] " +
            ", [SaleReturn].FromDate AS [DocumentFromDate] " +
            ", [Client].FullName AS [ClientName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [User].LastName AS [Responsible] " +
            ", ROUND([SaleReturnItem].Amount / [SaleReturnItem].Qty, 2) AS [Price] " +
            ", 0 AS [Discount] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [IncomeQty] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [OutcomeQty] " +
            ", N'' AS [Comment] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 1 " +
            ") " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = ( " +
            "CASE " +
            "WHEN [SaleReturn].UpdatedByID IS NOT NULL " +
            "THEN [SaleReturn].UpdatedByID " +
            "ELSE [SaleReturn].CreatedByID " +
            "END " +
            ") " +
            "LEFT JOIN [ConsignmentItem] AS [RootConsignmentItem] " +
            "ON [RootConsignmentItem].ID = [ConsignmentItem].RootConsignmentItemID " +
            "LEFT JOIN [Consignment] AS [RootConsignment] " +
            "ON [RootConsignment].ID = [RootConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] AS [RootProductIncome] " +
            "ON [RootProductIncome].ID = [RootConsignment].ProductIncomeID " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 1 " +
            "AND [ConsignmentItemMovement].ConsignmentItemID IN ( " +
            "SELECT [HierarchyItems].ID FROM @ConsignmentHierarchy AS [HierarchyItems] " +
            ") " +
            "UNION ALL " +
            "SELECT [ProductIncome].Number AS [IncomeDocumentNumber] " +
            ", [ProductIncome].FromDate AS [IncomeDocumentFromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentType] " +
            ", [SaleNumber].[Value] AS [DocumentNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [Sale].ChangedToInvoice IS NOT NULL " +
            "THEN [Sale].ChangedToInvoice " +
            "ELSE [Sale].Updated " +
            "END " +
            ") AS [DocumentFromDate] " +
            ", [Client].FullName AS [ClientName] " +
            ", N'' AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [User].LastName AS [Responsible] " +
            ", [OrderItem].PricePerItem AS [Price] " +
            ", ROUND([OrderItem].OneTimeDiscount + [OrderItem].DiscountAmount, 2) AS [Discount] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [IncomeQty] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [OutcomeQty] " +
            ", [OrderItemBaseShiftStatus].Comment AS [Comment] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 2 " +
            ") " +
            "LEFT JOIN [OrderItemBaseShiftStatus] " +
            "ON [OrderItemBaseShiftStatus].ID = [ConsignmentItemMovement].OrderItemBaseShiftStatusID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [OrderItemBaseShiftStatus].OrderItemID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [OrderItem].OrderID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OrderItemBaseShiftStatus].UserID " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 2 " +
            "AND [ConsignmentItemMovement].ConsignmentItemID IN ( " +
            "SELECT [HierarchyItems].ID FROM @ConsignmentHierarchy AS [HierarchyItems] " +
            ") " +
            "UNION ALL " +
            "SELECT N'' AS [IncomeDocumentNumber] " +
            ", NULL AS [IncomeDocumentFromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentType] " +
            ", [ProductIncome].Number AS [DocumentNumber] " +
            ", [ProductIncome].FromDate AS [DocumentFromDate] " +
            ", [Client].FullName AS [ClientName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [User].LastName AS [Responsible] " +
            ", [ConsignmentItem].Price AS [Price] " +
            ", 0 AS [Discount] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [IncomeQty] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [OutcomeQty] " +
            ", [ProductIncome].Comment AS [Comment] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 3 " +
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
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [User] " +
            "ON [ProductIncome].UserID = [User].ID " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 3 " +
            "AND [ConsignmentItemMovement].ConsignmentItemID IN ( " +
            "SELECT [HierarchyItems].ID FROM @ConsignmentHierarchy AS [HierarchyItems] " +
            ") " +
            "UNION ALL " +
            "SELECT ISNULL([RootProductIncome].Number, N'') AS [IncomeDocumentNumber] " +
            ", [RootProductIncome].FromDate AS [IncomeDocumentFromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentType] " +
            ", [ProductIncome].Number AS [DocumentNumber] " +
            ", [ProductIncome].FromDate AS [DocumentFromDate] " +
            ", [Client].FullName AS [ClientName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [User].LastName AS [Responsible] " +
            ", [ConsignmentItem].Price AS [Price] " +
            ", 0 AS [Discount] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [IncomeQty] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [OutcomeQty] " +
            ", [ProductIncome].Comment AS [Comment] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 4 " +
            ") " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SupplyOrderUkraine].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [ConsignmentItem] AS [RootConsignmentItem] " +
            "ON [RootConsignmentItem].ID = [SupplyOrderUkraineItem].ConsignmentItemID " +
            "LEFT JOIN [Consignment] AS [RootConsignment] " +
            "ON [RootConsignment].ID = [RootConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] AS [RootProductIncome] " +
            "ON [RootProductIncome].ID = [RootConsignment].ProductIncomeID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductIncome].UserID " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 4 " +
            "AND [ConsignmentItemMovement].ConsignmentItemID IN ( " +
            "SELECT [HierarchyItems].ID FROM @ConsignmentHierarchy AS [HierarchyItems] " +
            ") " +
            "UNION ALL " +
            "SELECT [ProductIncome].Number AS [IncomeDocumentNumber] " +
            ", [ProductIncome].FromDate AS [IncomeDocumentFromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentType] " +
            ", [DepreciatedOrder].Number AS [DocumentNumber] " +
            ", [DepreciatedOrder].FromDate AS [DocumentFromDate] " +
            ", N'' AS [ClientName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [User].LastName AS [Responsible] " +
            ", [ConsignmentItem].Price AS [Price] " +
            ", 0 AS [Discount] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [IncomeQty] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [OutcomeQty] " +
            ", [DepreciatedOrder].Comment AS [Comment] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 5 " +
            ") " +
            "LEFT JOIN [DepreciatedOrderItem] " +
            "ON [DepreciatedOrderItem].ID = [ConsignmentItemMovement].DepreciatedOrderItemID " +
            "LEFT JOIN [DepreciatedOrder] " +
            "ON [DepreciatedOrder].ID = [DepreciatedOrderItem].DepreciatedOrderID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [DepreciatedOrder].ResponsibleID " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 5 " +
            "AND [ConsignmentItemMovement].ConsignmentItemID IN ( " +
            "SELECT [HierarchyItems].ID FROM @ConsignmentHierarchy AS [HierarchyItems] " +
            ") " +
            "UNION ALL " +
            "SELECT [ProductIncome].Number AS [IncomeDocumentNumber] " +
            ", [ProductIncome].FromDate AS [IncomeDocumentFromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentType] " +
            ", [SupplyReturn].Number AS [DocumentNumber] " +
            ", [SupplyReturn].FromDate AS [DocumentFromDate] " +
            ", [Client].FullName AS [ClientName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [User].LastName AS [Responsible] " +
            ", [ConsignmentItem].Price AS [Price] " +
            ", 0 AS [Discount] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [IncomeQty] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [OutcomeQty] " +
            ", [SupplyReturn].Comment AS [Comment] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 6 " +
            ") " +
            "LEFT JOIN [SupplyReturnItem] " +
            "ON [SupplyReturnItem].ID = [ConsignmentItemMovement].SupplyReturnItemID " +
            "LEFT JOIN [SupplyReturn] " +
            "ON [SupplyReturn].ID = [SupplyReturnItem].SupplyReturnID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SupplyReturnItem].ConsignmentItemID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [SupplyReturn].ResponsibleID " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 6 " +
            "AND [ConsignmentItemMovement].ConsignmentItemID IN ( " +
            "SELECT [HierarchyItems].ID FROM @ConsignmentHierarchy AS [HierarchyItems] " +
            ") " +
            "UNION ALL " +
            "SELECT [ProductIncome].Number AS [IncomeDocumentNumber] " +
            ", [ProductIncome].FromDate AS [IncomeDocumentFromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentType] " +
            ", [ProductTransfer].Number AS [DocumentNumber] " +
            ", [ProductTransfer].FromDate AS [DocumentFromDate] " +
            ", N'' AS [ClientName] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 " +
            "THEN [ToStorage].[Name] " +
            "ELSE [FromStorage].[Name] " +
            "END " +
            ") AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [User].LastName AS [Responsible] " +
            ", [ConsignmentItem].Price AS [Price] " +
            ", 0 AS [Discount] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [IncomeQty] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [OutcomeQty] " +
            ", [ProductTransfer].Comment AS [Comment] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 7 " +
            ") " +
            "LEFT JOIN [ProductTransferItem] " +
            "ON [ProductTransferItem].ID = [ConsignmentItemMovement].ProductTransferItemID " +
            "LEFT JOIN [ProductTransfer] " +
            "ON [ProductTransfer].ID = [ProductTransferItem].ProductTransferID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductTransfer].ResponsibleID " +
            "LEFT JOIN [Storage] AS [FromStorage] " +
            "ON [FromStorage].ID = [ProductTransfer].FromStorageID " +
            "LEFT JOIN [Storage] AS [ToStorage] " +
            "ON [ToStorage].ID = [ProductTransfer].ToStorageID " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 7 " +
            "AND [ConsignmentItemMovement].ConsignmentItemID IN ( " +
            "SELECT [HierarchyItems].ID FROM @ConsignmentHierarchy AS [HierarchyItems] " +
            ") " +
            "UNION ALL " +
            "SELECT [ProductIncome].Number AS [IncomeDocumentNumber] " +
            ", [ProductIncome].FromDate AS [IncomeDocumentFromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentType] " +
            ", [Sad].Number AS [DocumentNumber] " +
            ", [Sad].FromDate AS [DocumentFromDate] " +
            ", ( " +
            "CASE " +
            "WHEN [Sad].ClientID IS NOT NULL " +
            "THEN [Client].FullName " +
            "ELSE [OrganizationClient].FullName " +
            "END " +
            ") AS [ClientName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [User].LastName AS [Responsible] " +
            ", ( " +
            "CASE " +
            "WHEN [SupplyOrderUkraineCartItem].ID IS NOT NULL " +
            "THEN ROUND([SupplyOrderUkraineCartItem].UnitPrice + [SupplyOrderUkraineCartItem].UnitPrice * [Sad].MarginAmount / 100, 2) " +
            "ELSE ROUND([SadItem].UnitPrice + [SadItem].UnitPrice * [Sad].MarginAmount / 100, 2) " +
            "END " +
            ") AS [Price] " +
            ", 0 AS [Discount] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [IncomeQty] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [OutcomeQty] " +
            ", [Sad].Comment AS [Comment] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 8 " +
            ") " +
            "LEFT JOIN [SadItem] " +
            "ON [SadItem].ID = [ConsignmentItemMovement].SadItemID " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [SadItem].SupplyOrderUkraineCartItemID " +
            "LEFT JOIN [Sad] " +
            "ON [Sad].ID = [SadItem].SadID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [Sad].ResponsibleID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [Sad].ClientID " +
            "LEFT JOIN [OrganizationClient] " +
            "ON [OrganizationClient].ID = [Sad].OrganizationClientID " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 8 " +
            "AND [ConsignmentItemMovement].ConsignmentItemID IN ( " +
            "SELECT [HierarchyItems].ID FROM @ConsignmentHierarchy AS [HierarchyItems] " +
            ") " +
            "UNION ALL " +
            "SELECT [ProductIncome].Number AS [IncomeDocumentNumber] " +
            ", [ProductIncome].FromDate AS [IncomeDocumentFromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentType] " +
            ", [TaxFree].Number AS [DocumentNumber] " +
            ", [TaxFreePackList].FromDate AS [DocumentFromDate] " +
            ", [Client].FullName AS [ClientName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [User].LastName AS [Responsible] " +
            ", ( " +
            "CASE " +
            "WHEN [SupplyOrderUkraineCartItem].ID IS NOT NULL " +
            "THEN ROUND([SupplyOrderUkraineCartItem].UnitPrice + [SupplyOrderUkraineCartItem].UnitPrice * [TaxFreePackList].MarginAmount / 100, 2) " +
            "ELSE [OrderItem].PricePerItem " +
            "END " +
            ") AS [Price] " +
            ", 0 AS [Discount] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [IncomeQty] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [OutcomeQty] " +
            ", N'' AS [Comment] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 9 " +
            ") " +
            "LEFT JOIN [TaxFreeItem] " +
            "ON [TaxFreeItem].ID = [ConsignmentItemMovement].TaxFreeItemID " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].ID = [TaxFreeItem].SupplyOrderUkraineCartItemID " +
            "LEFT JOIN [TaxFreePackListOrderItem] " +
            "ON [TaxFreePackListOrderItem].ID = [TaxFreeItem].TaxFreePackListOrderItemID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [TaxFreePackListOrderItem].OrderItemID " +
            "LEFT JOIN [TaxFree] " +
            "ON [TaxFree].ID = [TaxFreeItem].TaxFreeID " +
            "LEFT JOIN [TaxFreePackList] " +
            "ON [TaxFreePackList].ID = [TaxFree].TaxFreePackListID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [TaxFreePackList].ClientID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [TaxFree].ResponsibleID " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 9 " +
            "AND [ConsignmentItemMovement].ConsignmentItemID IN ( " +
            "SELECT [HierarchyItems].ID FROM @ConsignmentHierarchy AS [HierarchyItems] " +
            ") " +
            "UNION ALL " +
            "SELECT N'' AS [IncomeDocumentNumber] " +
            ", NULL AS [IncomeDocumentFromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentType] " +
            ", [ProductIncome].Number AS [DocumentNumber] " +
            ", [ProductIncome].FromDate AS [DocumentFromDate] " +
            ", N'' AS [ClientName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [User].LastName AS [Responsible] " +
            ", [ConsignmentItem].Price AS [Price] " +
            ", 0 AS [Discount] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [IncomeQty] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [OutcomeQty] " +
            ", [ProductCapitalization].Comment AS [Comment] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 11 " +
            ") " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ID = [ConsignmentItem].ProductIncomeItemID " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductCapitalization].ResponsibleID " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 11 " +
            "AND [ConsignmentItemMovement].ConsignmentItemID IN ( " +
            "SELECT [HierarchyItems].ID FROM @ConsignmentHierarchy AS [HierarchyItems] " +
            ") " +
            "UNION ALL " +
            "SELECT [ProductIncome].Number AS [IncomeDocumentNumber] " +
            ", [ProductIncome].FromDate AS [IncomeDocumentFromDate] " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentType] " +
            ", [SaleNumber].[Value] AS [DocumentNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [Sale].ChangedToInvoice IS NOT NULL " +
            "THEN [Sale].ChangedToInvoice " +
            "ELSE [Sale].Updated " +
            "END " +
            ") AS [DocumentFromDate] " +
            ", [Client].FullName AS [ClientName] " +
            ", [Storage].[Name] AS [StorageName] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [User].LastName AS [Responsible] " +
            ", [OrderItem].PricePerItem AS [Price] " +
            ", ROUND([OrderItem].OneTimeDiscount + [OrderItem].DiscountAmount, 2) AS [Discount] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 1 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [IncomeQty] " +
            ", ( " +
            "CASE " +
            "WHEN [ConsignmentItemMovement].IsIncomeMovement = 0 " +
            "THEN [ConsignmentItemMovement].Qty " +
            "ELSE 0 " +
            "END " +
            ") AS [OutcomeQty] " +
            ", [OrderItemBaseShiftStatus].Comment AS [Comment] " +
            "FROM [ConsignmentItemMovement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ConsignmentItem].ProductID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "LEFT JOIN [ProductIncome] " +
            "ON [ProductIncome].ID = [Consignment].ProductIncomeID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Consignment].OrganizationID " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [Consignment].StorageID " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].MovementType = 12 " +
            ") " +
            "LEFT JOIN [OrderItemBaseShiftStatus] " +
            "ON [OrderItemBaseShiftStatus].ID = [ConsignmentItemMovement].OrderItemBaseShiftStatusID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [OrderItemBaseShiftStatus].OrderItemID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [OrderItem].OrderID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OrderItemBaseShiftStatus].UserID " +
            "WHERE [ConsignmentItemMovement].Deleted = 0 " +
            "AND [ConsignmentItemMovement].MovementType = 12 " +
            "AND [ConsignmentItemMovement].ConsignmentItemID IN ( " +
            "SELECT [HierarchyItems].ID FROM @ConsignmentHierarchy AS [HierarchyItems] " +
            ") " +
            ") " +
            "SELECT * " +
            "FROM [MovementConsignmentInfo_CTE] " +
            "ORDER BY [MovementConsignmentInfo_CTE].DocumentFromDate ",
            new {
                ConsignmentItemNetId = consignmentItemNetId,
                From = from,
                To = to,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );
    }

    public IEnumerable<ClientMovementConsignmentInfo> GetClientMovementConsignmentInfoFiltered(
        Guid clientNetId,
        DateTime from,
        DateTime to,
        int limit,
        int offset,
        long[] organizationIds,
        string article) {
        List<ClientMovementConsignmentInfo> consignmentInfos = new();

        _connection.Query<ClientMovementConsignmentInfo, ClientMovementConsignmentInfoItem, Product, ClientMovementConsignmentInfo>(
            ";WITH [CombinedData_CTE] " +
            "AS ( " +
            "SELECT [Sale].ID AS [DocumentID] " +
            ", ( " +
            "CASE " +
            "WHEN [Order].OrderSource = 0 " +
            "THEN 13 " +
            "WHEN [Order].OrderSource = 1 " +
            "THEN CASE " +
            "WHEN [BaseLifeCycleStatus].SaleLifeCycleType = 0 THEN 15 " +
            "ELSE 0 " +
            "END " +
            "ELSE 14 " +
            "END " +
            ") AS [DocumentType] " +
            ", ISNULL([Sale].ChangedToInvoice, [Sale].Created) AS [DocumentFromDate] " +
            ", ISNULL([Sale].[ChangedToInvoice], [Sale].[Updated]) AS [DocumentUpdatedDate] " +
            "FROM [Sale] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [Sale].BaseLifeCycleStatusID = [BaseLifeCycleStatus].ID " +
            "WHERE [Sale].IsMerged = 0 " +
            "AND [Sale].Deleted = 0 " +
            "AND [Client].NetUID = @ClientNetId " +
            "AND [Organization].ID IN @OrganizationIds " +
            "AND ISNULL([Sale].ChangedToInvoice, [Sale].Created) >= @From " +
            "AND ISNULL([Sale].ChangedToInvoice, [Sale].Created) <= @To " +
            "AND " +
            "( " +
            "PATINDEX('%' + @Value + '%', [Product].VendorCode) > 0 " +
            "OR PATINDEX('%' + @Value + '%', [Product].MainOriginalNumber) > 0 " +
            "OR CASE WHEN @Culture = 'pl' " +
            "THEN PATINDEX('%' + @Value + '%', [Product].[NameUA]) " +
            "ELSE PATINDEX('%' + @Value + '%', [Product].[NameUA]) " +
            "END > 0 " +
            ") " +
            "GROUP BY [Sale].ID, [Order].OrderSource, [Sale].ChangedToInvoice, [Sale].Created, [Sale].Updated, [BaseLifeCycleStatus].SaleLifeCycleType " +
            "UNION ALL " +
            "SELECT [SaleReturn].ID " +
            ", 1 AS [DocumentType] " +
            ", [SaleReturn].FromDate AS [DocumentFromDate] " +
            ", [SaleReturn].[Updated] AS [DocumentUpdatedDate] " +
            "FROM [SaleReturn] " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].SaleReturnID = [SaleReturn].ID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SaleReturn].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "WHERE [SaleReturn].Deleted = 0 " +
            "AND [SaleReturn].IsCanceled = 0 " +
            "AND [Client].NetUID = @ClientNetId " +
            "AND [Organization].ID IN @OrganizationIds " +
            "AND [SaleReturn].FromDate >= @From " +
            "AND [SaleReturn].FromDate <= @To " +
            "AND " +
            "( " +
            "PATINDEX('%' + @Value + '%', [Product].VendorCode) > 0 " +
            "OR PATINDEX('%' + @Value + '%', [Product].MainOriginalNumber) > 0 " +
            "OR CASE WHEN @Culture = 'pl' " +
            "THEN PATINDEX('%' + @Value + '%', [Product].[NameUA]) " +
            "ELSE PATINDEX('%' + @Value + '%', [Product].[NameUA]) " +
            "END > 0 " +
            ") " +
            "GROUP BY [SaleReturn].ID, [SaleReturn].FromDate, [SaleReturn].[Updated] " +
            "), " +
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT COUNT(*) OVER() [TotalRowsQty] " +
            ", [CombinedData_CTE].DocumentID " +
            ", [CombinedData_CTE].DocumentType " +
            ", [CombinedData_CTE].DocumentFromDate " +
            ", [CombinedData_CTE].DocumentUpdatedDate " +
            ", ROW_NUMBER() OVER(ORDER BY [CombinedData_CTE].DocumentFromDate) AS [RowNumber] " +
            "FROM [CombinedData_CTE] " +
            ") " +
            "SELECT [InfoRow].TotalRowsQty AS [TotalRowsQty] " +
            ", [InfoRow].DocumentID AS [DocumentId] " +
            ", [InfoRow].DocumentFromDate AS [DocumentFromDate] " +
            ", [InfoRow].DocumentUpdatedDate AS DocumentUpdatedDate " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentTypeName] " +
            ", ( " +
            "CASE " +
            "WHEN [SaleNumber].ID IS NOT NULL " +
            "THEN [SaleNumber].[Value] " +
            "ELSE [SaleReturn].Number " +
            "END " +
            ") AS [DocumentNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [User].LastName AS [Responsible] " +
            ", ( " +
            "CASE " +
            "WHEN [OrderItem].ID IS NOT NULL " +
            "THEN [OrderItem].Qty " +
            "ELSE [SaleReturnItem].Qty " +
            "END " +
            ") AS [ItemQty] " +
            ", ( " +
            "CASE " +
            "WHEN [OrderItem].ID IS NOT NULL " +
            "THEN (CASE " +
            "WHEN [OrderItem].PricePerItem <> 0 " +
            "THEN [OrderItem].PricePerItem " +
            "ELSE dbo.GetCalculatedProductPriceWithSharesAndVat(dbo.[Product].NetUID, [ClientAgreement].NetUID, @Culture, [Agreement].WithVATAccounting, [OrderItem].ID) " +
            "END) " +
            "ELSE ROUND([SaleReturnItem].Amount / CAST([SaleReturnItem].Qty AS money), 4) " +
            "END " +
            ") AS [PricePerItem] " +
            ", ( " +
            "CASE " +
            "WHEN [OrderItem].ID IS NOT NULL " +
            "THEN (CASE " +
            "WHEN [OrderItem].PricePerItem <> 0 " +
            "THEN ROUND([OrderItem].PricePerItem * CAST([OrderItem].Qty AS money), 4) " +
            "ELSE ROUND(dbo.GetCalculatedProductPriceWithSharesAndVat(dbo.[Product].NetUID, [ClientAgreement].NetUID, @Culture, [Agreement].WithVATAccounting, [OrderItem].ID) * CAST([OrderItem].Qty AS money), 4) " +
            "END)" +
            "ELSE [SaleReturnItem].Amount " +
            "END " +
            ") AS [TotalAmount] " +
            ", [ProductSpecification].SpecificationCode AS [ProductSpecificationCode] " +
            ", [ItemResponsible].LastName AS [Responsible] " +
            ", [Product].* " +
            "FROM [Rowed_CTE] AS [InfoRow] " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].Deleted = 0 " +
            "AND [JoinTypeName].MovementType = [InfoRow].DocumentType " +
            ") " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [InfoRow].DocumentID " +
            "AND [InfoRow].DocumentType <> 1 " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Sale].OrderID " +
            "AND [OrderItem].Deleted = 0 " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [InfoRow].DocumentID " +
            "AND [InfoRow].DocumentType = 1 " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].SaleReturnID = [SaleReturn].ID " +
            "AND [SaleReturnItem].Deleted = 0 " +
            "LEFT JOIN [OrderItem] AS [SaleReturnItemOrderItem] " +
            "ON [SaleReturnItemOrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = ( " +
            "CASE " +
            "WHEN [Sale].ID IS NOT NULL " +
            "THEN [Agreement].OrganizationID " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN ( " +
            "SELECT TOP(1) [JoinAgreement].OrganizationID " +
            "FROM [Sale] AS [JoinSale] " +
            "LEFT JOIN [ClientAgreement] AS [JoinClientAgreement] " +
            "ON [JoinClientAgreement].ID = [JoinSale].ClientAgreementID " +
            "LEFT JOIN [Agreement] AS [JoinAgreement] " +
            "ON [JoinAgreement].ID = [JoinClientAgreement].AgreementID " +
            "WHERE [JoinSale].OrderID = [SaleReturnItemOrderItem].OrderID " +
            ") " +
            "ELSE 0 " +
            "END " +
            ") " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = ( " +
            "CASE " +
            "WHEN [SaleReturn].UpdatedByID IS NOT NULL " +
            "THEN [SaleReturn].UpdatedByID " +
            "WHEN [SaleReturn].CreatedByID IS NOT NULL " +
            "THEN [SaleReturn].CreatedByID " +
            "WHEN [Sale].ChangedToInvoiceByID IS NOT NULL " +
            "THEN [Sale].ChangedToInvoiceByID " +
            "ELSE [Sale].UserID " +
            "END " +
            ") " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = ( " +
            "CASE " +
            "WHEN [OrderItem].ID IS NOT NULL " +
            "THEN [OrderItem].ProductID " +
            "ELSE [SaleReturnItemOrderItem].ProductID " +
            "END " +
            ") " +
            "LEFT JOIN [User] AS [ItemResponsible] " +
            "ON [ItemResponsible].ID = ( " +
            "CASE " +
            "WHEN [OrderItem].ID IS NOT NULL " +
            "THEN [OrderItem].UserId " +
            "WHEN [SaleReturnItem].UpdatedByID IS NOT NULL " +
            "THEN [SaleReturnItem].UpdatedByID " +
            "ELSE [SaleReturnItem].CreatedByID " +
            "END " +
            ") " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ID = ( " +
            "CASE " +
            "WHEN [OrderItem].ID IS NOT NULL " +
            "THEN [OrderItem].AssignedSpecificationID " +
            "ELSE [SaleReturnItemOrderItem].AssignedSpecificationID " +
            "END " +
            ") " +
            "WHERE [InfoRow].RowNumber > @Offset " +
            "AND [InfoRow].RowNumber <= @Limit + @Offset ",
            (clientMovementConsignmentInfo, clientMovementConsignmentInfoItem, product) => {
                if (consignmentInfos.Any(i => i.DocumentId.Equals(clientMovementConsignmentInfo.DocumentId)))
                    clientMovementConsignmentInfo = consignmentInfos.First(i => i.DocumentId.Equals(clientMovementConsignmentInfo.DocumentId));
                else
                    consignmentInfos.Add(clientMovementConsignmentInfo);

                if (clientMovementConsignmentInfoItem == null) return clientMovementConsignmentInfo;

                clientMovementConsignmentInfoItem.Product = product;

                clientMovementConsignmentInfo.InfoItems.Add(clientMovementConsignmentInfoItem);

                clientMovementConsignmentInfo.TotalQty += clientMovementConsignmentInfoItem.ItemQty;
                clientMovementConsignmentInfo.TotalEuroAmount += clientMovementConsignmentInfoItem.TotalAmount;

                return clientMovementConsignmentInfo;
            },
            new {
                ClientNetId = clientNetId,
                From = from,
                To = to,
                Limit = limit,
                Offset = offset,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Value = article,
                OrganizationIds = organizationIds
            },
            splitOn: "ItemQty,ID"
        );

        return consignmentInfos.OrderByDescending(e => e.DocumentFromDate);
    }

    public IEnumerable<ClientMovementConsignmentInfo> GetClientMovementConsignmentInfoFilteredFoxDocumentExport(
        Guid clientNetId,
        DateTime from,
        DateTime to) {
        List<ClientMovementConsignmentInfo> consignmentInfos = new();

        _connection.Query<ClientMovementConsignmentInfo, ClientMovementConsignmentInfoItem, Product, ClientMovementConsignmentInfo>(
            ";WITH [CombinedData_CTE] " +
            "AS ( " +
            "SELECT [Sale].ID AS [DocumentID] " +
            ", ( " +
            "CASE " +
            "WHEN [Order].OrderSource = 0 " +
            "THEN 13 " +
            "WHEN [Order].OrderSource = 1 " +
            "THEN 0 " +
            "ELSE 14 " +
            "END " +
            ") AS [DocumentType] " +
            ", ISNULL([Sale].ChangedToInvoice, [Sale].Created) AS [DocumentFromDate] " +
            ", ISNULL([Sale].[ChangedToInvoice], [Sale].[Updated]) AS [DocumentUpdatedDate] " +
            "FROM [Sale] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "WHERE [Sale].IsMerged = 0 " +
            "AND [Sale].Deleted = 0 " +
            "AND [Client].NetUID = @ClientNetId " +
            "AND ISNULL([Sale].ChangedToInvoice, [Sale].Created) >= @From " +
            "AND ISNULL([Sale].ChangedToInvoice, [Sale].Created) <= @To " +
            "UNION ALL " +
            "SELECT [SaleReturn].ID " +
            ", 1 AS [DocumentType] " +
            ", [SaleReturn].FromDate AS [DocumentFromDate] " +
            ", [SaleReturn].[Updated] AS [DocumentUpdatedDate] " +
            "FROM [SaleReturn] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "WHERE [SaleReturn].Deleted = 0 " +
            "AND [SaleReturn].IsCanceled = 0 " +
            "AND [Client].NetUID = @ClientNetId " +
            "AND [SaleReturn].FromDate >= @From " +
            "AND [SaleReturn].FromDate <= @To " +
            "), " +
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT [CombinedData_CTE].DocumentID " +
            ", [CombinedData_CTE].DocumentType " +
            ", [CombinedData_CTE].DocumentFromDate " +
            ", [CombinedData_CTE].DocumentUpdatedDate " +
            ", ROW_NUMBER() OVER(ORDER BY [CombinedData_CTE].DocumentFromDate) AS [RowNumber] " +
            "FROM [CombinedData_CTE] " +
            ") " +
            "SELECT [InfoRow].DocumentID AS [DocumentId] " +
            ", [InfoRow].DocumentFromDate AS [DocumentFromDate] " +
            ", [InfoRow].DocumentUpdatedDate AS DocumentUpdatedDate " +
            ", ( " +
            "CASE " +
            "WHEN @Culture = N'uk' " +
            "THEN [ConsignmentItemMovementTypeName].NameUa " +
            "ELSE [ConsignmentItemMovementTypeName].NamePl " +
            "END " +
            ") AS [DocumentTypeName] " +
            ", ( " +
            "CASE " +
            "WHEN [SaleNumber].ID IS NOT NULL " +
            "THEN [SaleNumber].[Value] " +
            "ELSE [SaleReturn].Number " +
            "END " +
            ") AS [DocumentNumber] " +
            ", ( " +
            "CASE " +
            "WHEN [OrganizationTranslation].[Name] IS NOT NULL " +
            "THEN [OrganizationTranslation].[Name] " +
            "ELSE [Organization].[Name] " +
            "END " +
            ") AS [OrganizationName] " +
            ", [User].LastName AS [Responsible] " +
            ", ( " +
            "CASE " +
            "WHEN [OrderItem].ID IS NOT NULL " +
            "THEN [OrderItem].Qty " +
            "ELSE [SaleReturnItem].Qty " +
            "END " +
            ") AS [ItemQty] " +
            ", ( " +
            "CASE " +
            "WHEN [OrderItem].ID IS NOT NULL " +
            "THEN [OrderItem].PricePerItem " +
            "ELSE ROUND([SaleReturnItem].Amount / CAST([SaleReturnItem].Qty AS money), 4) " +
            "END " +
            ") AS [PricePerItem] " +
            ", ( " +
            "CASE " +
            "WHEN [OrderItem].ID IS NOT NULL " +
            "THEN ROUND([OrderItem].PricePerItem * CAST([OrderItem].Qty AS money), 4) " +
            "ELSE [SaleReturnItem].Amount " +
            "END " +
            ") AS [TotalAmount] " +
            ", [ProductSpecification].SpecificationCode AS [ProductSpecificationCode] " +
            ", [ItemResponsible].LastName AS [Responsible] " +
            ", [Product].* " +
            "FROM [Rowed_CTE] AS [InfoRow] " +
            "LEFT JOIN [ConsignmentItemMovementTypeName] " +
            "ON [ConsignmentItemMovementTypeName].ID = ( " +
            "SELECT TOP(1) [JoinTypeName].ID " +
            "FROM [ConsignmentItemMovementTypeName] AS [JoinTypeName] " +
            "WHERE [JoinTypeName].Deleted = 0 " +
            "AND [JoinTypeName].MovementType = [InfoRow].DocumentType " +
            ") " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [InfoRow].DocumentID " +
            "AND [InfoRow].DocumentType <> 1 " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [Sale].SaleNumberID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Sale].OrderID " +
            "AND [OrderItem].Deleted = 0 " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [InfoRow].DocumentID " +
            "AND [InfoRow].DocumentType = 1 " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].SaleReturnID = [SaleReturn].ID " +
            "AND [SaleReturnItem].Deleted = 0 " +
            "LEFT JOIN [OrderItem] AS [SaleReturnItemOrderItem] " +
            "ON [SaleReturnItemOrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = ( " +
            "CASE " +
            "WHEN [Sale].ID IS NOT NULL " +
            "THEN [Agreement].OrganizationID " +
            "WHEN [SaleReturn].ID IS NOT NULL " +
            "THEN ( " +
            "SELECT TOP(1) [JoinAgreement].OrganizationID " +
            "FROM [Sale] AS [JoinSale] " +
            "LEFT JOIN [ClientAgreement] AS [JoinClientAgreement] " +
            "ON [JoinClientAgreement].ID = [JoinSale].ClientAgreementID " +
            "LEFT JOIN [Agreement] AS [JoinAgreement] " +
            "ON [JoinAgreement].ID = [JoinClientAgreement].AgreementID " +
            "WHERE [JoinSale].OrderID = [SaleReturnItemOrderItem].OrderID " +
            ") " +
            "ELSE 0 " +
            "END " +
            ") " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "LEFT JOIN [User] " +
            "ON [User].ID = ( " +
            "CASE " +
            "WHEN [SaleReturn].UpdatedByID IS NOT NULL " +
            "THEN [SaleReturn].UpdatedByID " +
            "WHEN [SaleReturn].CreatedByID IS NOT NULL " +
            "THEN [SaleReturn].CreatedByID " +
            "WHEN [Sale].ChangedToInvoiceByID IS NOT NULL " +
            "THEN [Sale].ChangedToInvoiceByID " +
            "ELSE [Sale].UserID " +
            "END " +
            ") " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = ( " +
            "CASE " +
            "WHEN [OrderItem].ID IS NOT NULL " +
            "THEN [OrderItem].ProductID " +
            "ELSE [SaleReturnItemOrderItem].ProductID " +
            "END " +
            ") " +
            "LEFT JOIN [User] AS [ItemResponsible] " +
            "ON [ItemResponsible].ID = ( " +
            "CASE " +
            "WHEN [OrderItem].ID IS NOT NULL " +
            "THEN [OrderItem].UserId " +
            "WHEN [SaleReturnItem].UpdatedByID IS NOT NULL " +
            "THEN [SaleReturnItem].UpdatedByID " +
            "ELSE [SaleReturnItem].CreatedByID " +
            "END " +
            ") " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ID = ( " +
            "CASE " +
            "WHEN [OrderItem].ID IS NOT NULL " +
            "THEN [OrderItem].AssignedSpecificationID " +
            "ELSE [SaleReturnItemOrderItem].AssignedSpecificationID " +
            "END " +
            ") ",
            (clientMovementConsignmentInfo, clientMovementConsignmentInfoItem, product) => {
                if (consignmentInfos.Any(i => i.DocumentId.Equals(clientMovementConsignmentInfo.DocumentId))) {
                    clientMovementConsignmentInfo = consignmentInfos.First(i => i.DocumentId.Equals(clientMovementConsignmentInfo.DocumentId));
                } else {
                    consignmentInfos.Add(clientMovementConsignmentInfo);

                    clientMovementConsignmentInfo.TotalEuroAmount += decimal.Round(
                        Convert.ToDecimal(clientMovementConsignmentInfoItem.ItemQty) * clientMovementConsignmentInfoItem.PricePerItem,
                        2,
                        MidpointRounding.AwayFromZero);
                }

                if (clientMovementConsignmentInfoItem == null) return clientMovementConsignmentInfo;

                clientMovementConsignmentInfoItem.Product = product;

                clientMovementConsignmentInfo.InfoItems.Add(clientMovementConsignmentInfoItem);

                return clientMovementConsignmentInfo;
            },
            new {
                ClientNetId = clientNetId,
                From = from,
                To = to,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            },
            splitOn: "ItemQty,ID"
        );

        return consignmentInfos;
    }
}