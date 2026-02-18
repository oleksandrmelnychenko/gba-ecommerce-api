using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.Repositories.DataSync.Contracts;

namespace GBA.Domain.Repositories.DataSync;

public sealed class DocumentsAfterSyncRepository : IDocumentsAfterSyncRepository {
    private const string FROM_ONE_C = "Ввід боргів з 1С";

    private readonly IDbConnection _connection;

    public DocumentsAfterSyncRepository(IDbConnection connection) {
        _connection = connection;
    }

    public List<GenericDocument> GetMappedDocumentsFiltered(
        DateTime from,
        DateTime to,
        int limit,
        int offset,
        string name,
        ContractorType contractorType) {
        List<GenericDocument> documents = new();

        string filteredSqlExpression =
            ";WITH [Search_CTE] AS ( " +
            "SELECT [ProductIncome].ID, [ProductIncome].Created, 0 AS [DocumentType] " +
            "FROM [ProductIncome] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [PackingListPackage] " +
            "ON [PackingListPackage].ID = [PackingListPackageOrderItem].PackingListPackageID " +
            "LEFT JOIN [PackingList] AS [PackagePackingList] " +
            "ON [PackagePackingList].ID = [PackingListPackage].PackingListID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [SupplyInvoiceOrderItem].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [ActReconciliationItem] " +
            "ON [ProductIncomeItem].ActReconciliationItemID = [ActReconciliationItem].ID " +
            "LEFT JOIN [ActReconciliation] " +
            "ON [ActReconciliationItem].ActReconciliationID = [ActReconciliation].ID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "OR [Client].[ID] = [SupplyOrder].[ClientID] " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "AND [ClientInRole].Deleted = 0 " +
            "LEFT JOIN [ClientType] " +
            "ON [ClientType].ID = [ClientInRole].ClientTypeID " +
            "WHERE [ProductIncome].IsFromOneC = 1 " +
            "AND [ProductIncome].Created >= @From " +
            "AND [ProductIncome].Created <= @To " +
            "AND [Client].FullName LIKE N'%' + @Name + N'%' ";
        filteredSqlExpression += contractorType switch {
            ContractorType.Client => "AND ([ClientType].Type = 0 OR [Client].IsTemporaryClient = 1) ",
            ContractorType.Supplier => "AND [ClientType].Type = 1 ",
            ContractorType.SupplyOrganization => "AND [ProductIncome].ID IS NULL ",
            _ => string.Empty
        };

        filteredSqlExpression +=
            "GROUP BY [ProductIncome].ID, [ProductIncome].Created " +
            "UNION ALL " +
            "SELECT [IncomePaymentOrder].ID, [IncomePaymentOrder].Created, 1 AS [DocumentType] FROM [IncomePaymentOrder] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [IncomePaymentOrder].ClientID " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "AND [ClientInRole].Deleted = 0 " +
            "LEFT JOIN [ClientType] " +
            "ON [ClientType].ID = [ClientInRole].ClientTypeID " +
            "WHERE [IncomePaymentOrder].Comment = @FromOneCConstant " +
            "AND [IncomePaymentOrder].Created >= @From " +
            "AND [IncomePaymentOrder].Created >= @To " +
            "AND [Client].FullName LIKE N'%' + @Name + N'%' ";
        filteredSqlExpression += contractorType switch {
            ContractorType.Client => "AND ([ClientType].Type = 0 OR [Client].IsTemporaryClient = 1) ",
            ContractorType.Supplier => "AND [ClientType].Type = 1 ",
            ContractorType.SupplyOrganization => "AND [IncomePaymentOrder].ID IS NULL ",
            _ => string.Empty
        };

        filteredSqlExpression +=
            "GROUP BY [IncomePaymentOrder].ID, [IncomePaymentOrder].Created " +
            "UNION ALL " +
            "SELECT [OutcomePaymentOrder].ID, [OutcomePaymentOrder].Created, 2 AS [DocumentType] FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [OutcomePaymentOrder].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "AND [ClientInRole].Deleted = 0 " +
            "LEFT JOIN [ClientType] " +
            "ON [ClientType].ID = [ClientInRole].ClientTypeID " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [SupplyOrganizationAgreement].SupplyOrganizationID " +
            "WHERE [OutcomePaymentOrder].Comment = @FromOneCConstant " +
            "AND [OutcomePaymentOrder].Created >= @From " +
            "AND [OutcomePaymentOrder].Created <= @To ";
        filteredSqlExpression += contractorType switch {
            ContractorType.Client => "AND ([ClientType].Type = 0 OR [Client].IsTemporaryClient = 1) ",
            ContractorType.Supplier => "AND [ClientType].Type = 1 ",
            ContractorType.SupplyOrganization => "AND [SupplyOrganization].ID IS NOT NULL ",
            _ => string.Empty
        };

        filteredSqlExpression +=
            "AND ([Client].FullName LIKE N'%' + @Name + N'%' " +
            "OR [SupplyOrganization].Name LIKE N'%' + @Name + N'%') " +
            "GROUP BY [OutcomePaymentOrder].ID, [OutcomePaymentOrder].Created " +
            "), " +
            "[Rowed_CTE] AS ( " +
            "SELECT [Search_CTE].ID, " +
            "ROW_NUMBER() OVER(ORDER BY [Search_CTE].Created DESC) AS [RowNumber], " +
            "[Search_CTE].DocumentType, " +
            "COUNT(*) OVER() [TotalQty] " +
            "FROM [Search_CTE] " +
            "GROUP BY [Search_CTE].ID, [Search_CTE].Created, [Search_CTE].DocumentType " +
            ") " +
            "SELECT " +
            "[Rowed_CTE].ID, " +
            "[Rowed_CTE].RowNumber, " +
            "[Rowed_CTE].DocumentType, " +
            "[Rowed_CTE].TotalQty " +
            "FROM [Rowed_CTE] " +
            "WHERE [Rowed_CTE].RowNumber > @Offset " +
            "AND [Rowed_CTE].RowNumber <= @Limit + @Offset ";


        IEnumerable<RowedResult> searchedResult = _connection.Query<RowedResult>(
            filteredSqlExpression,
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                From = from,
                To = to,
                Limit = limit,
                Offset = offset,
                Name = name,
                FromOneCConstant = FROM_ONE_C
            }
        );

        GetAllMappedIncomePaymentOrders(documents,
            searchedResult
                .Where(i => i.DocumentType.Equals(DocumentAfterSyncType.IncomePaymentOrder))
                .Select(i => i.Id));

        GetAllMappedOutcomePaymentOrders(documents,
            searchedResult
                .Where(i => i.DocumentType.Equals(DocumentAfterSyncType.OutcomePaymentOrder))
                .Select(i => i.Id));

        IEnumerable<long> productIncomeIds = searchedResult.Where(i => i.DocumentType.Equals(DocumentAfterSyncType.ProductIncome)).Select(i => i.Id);

        GetAllMappedClientReturns(documents, productIncomeIds);

        GetAllMappedActReconciliations(documents, productIncomeIds);

        // GetAllMappedProductCapitalizations(documents, productIncomeIds);

        GetAllMappedPackageOrders(documents, productIncomeIds);

        if (documents.Any())
            documents.First().TotalQty = searchedResult.First().TotalQty;

        return documents;
    }

    private void GetAllMappedIncomePaymentOrders(List<GenericDocument> documents, IEnumerable<long> ids) {
        string incomesQuery =
            "SELECT " +
            "[IncomePaymentOrder].*, " +
            "[Organization].*, " +
            "[Client].*, " +
            "[ClientAgreement].*, " +
            "[Agreement].*, " +
            "[Currency].*, " +
            "CASE WHEN ([ClientType].Type = 0 OR [Client].IsTemporaryClient = 1) THEN 0 " +
            "ELSE 1 END [ContractorType] " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [IncomePaymentOrder].CurrencyID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [IncomePaymentOrder].ClientID " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "AND [ClientInRole].Deleted = 0 " +
            "LEFT JOIN [ClientType] " +
            "ON [ClientType].ID = [ClientInRole].ClientTypeID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [IncomePaymentOrder].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "WHERE [IncomePaymentOrder].ID IN @Ids ";

        Type[] incomeTypes = {
            typeof(IncomePaymentOrder),
            typeof(Organization),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(ContractorType)
        };

        Func<object[], IncomePaymentOrder> incomesMapper = objects => {
            IncomePaymentOrder incomePaymentOrder = (IncomePaymentOrder)objects[0];
            Organization organization = (Organization)objects[1];
            Client client = (Client)objects[2];
            ClientAgreement clientAgreement = (ClientAgreement)objects[3];
            Agreement agreement = (Agreement)objects[4];
            Currency currency = (Currency)objects[5];
            ContractorType contractorType = (ContractorType)objects[6];

            clientAgreement.Agreement = agreement;

            documents.Add(new GenericDocument {
                TempId = incomePaymentOrder.Id,
                SynchronizationDate = incomePaymentOrder.Created,
                Number = incomePaymentOrder.Number,
                Type = "Введення залишків(балансу) з 1С",
                Amount = incomePaymentOrder.Amount,
                Currency = currency,
                Client = client,
                ClientAgreement = clientAgreement,
                Organization = organization,
                ContractorType = contractorType
            });

            return incomePaymentOrder;
        };

        _connection.Query(
            incomesQuery,
            incomeTypes,
            incomesMapper,
            new {
                Ids = ids
            },
            splitOn: "ID,ContractorType");
    }

    private void GetAllMappedOutcomePaymentOrders(List<GenericDocument> documents, IEnumerable<long> ids) {
        string outcomesQuery =
            "SELECT " +
            "[OutcomePaymentOrder].*, " +
            "[Organization].*, " +
            "[Client].*, " +
            "[ClientAgreement].*, " +
            "[Agreement].*, " +
            "[SupplyOrganization].*, " +
            "[SupplyOrganizationAgreement].*, " +
            "[Currency].*, " +
            "CASE " +
            "WHEN ([Client].ID IS NOT NULL AND [ClientType].Type = 0 OR [Client].IsTemporaryClient = 1) THEN 0 " +
            "WHEN [Client].ID IS NOT NULL THEN 1 " +
            "ELSE 2 END [ContractorType] " +
            "FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].ID = [OutcomePaymentOrder].PaymentCurrencyRegisterID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [OutcomePaymentOrder].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "AND [ClientInRole].Deleted = 0 " +
            "LEFT JOIN [ClientType] " +
            "ON [ClientType].ID = [ClientInRole].ClientTypeID " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [OutcomePaymentOrder].SupplyOrganizationAgreementID " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [SupplyOrganizationAgreement].SupplyOrganizationID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [OutcomePaymentOrder].OrganizationID " +
            "WHERE [OutcomePaymentOrder].ID IN @Ids ";

        Type[] outcomesTypes = {
            typeof(OutcomePaymentOrder),
            typeof(Organization),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(SupplyOrganization),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(ContractorType)
        };

        Func<object[], OutcomePaymentOrder> outcomesMapper = objects => {
            OutcomePaymentOrder outcomePaymentOrder = (OutcomePaymentOrder)objects[0];
            Organization organization = (Organization)objects[1];
            Client client = (Client)objects[2];
            ClientAgreement clientAgreement = (ClientAgreement)objects[3];
            Agreement agreement = (Agreement)objects[4];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[5];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[6];
            Currency currency = (Currency)objects[7];
            ContractorType contractorType = (ContractorType)objects[8];

            if (clientAgreement != null)
                clientAgreement.Agreement = agreement;

            documents.Add(new GenericDocument {
                TempId = outcomePaymentOrder.Id,
                SynchronizationDate = outcomePaymentOrder.Created,
                Number = outcomePaymentOrder.Number,
                Type = "Введення залишків(балансу) з 1С",
                Amount = outcomePaymentOrder.Amount,
                Currency = currency,
                Client = client,
                ClientAgreement = clientAgreement,
                Organization = organization,
                SupplyOrganization = supplyOrganization,
                SupplyOrganizationAgreement = supplyOrganizationAgreement,
                ContractorType = contractorType
            });

            return outcomePaymentOrder;
        };

        _connection.Query(
            outcomesQuery,
            outcomesTypes,
            outcomesMapper,
            new {
                Ids = ids
            },
            splitOn: "ID,ContractorType");
    }

    private void GetAllMappedClientReturns(List<GenericDocument> documents, IEnumerable<long> ids) {
        string clientReturnsQuery =
            "SELECT " +
            "[ProductIncome].*, " +
            "[ProductIncomeItem].*, " +
            "[SaleReturnItem].*, " +
            "[Client].*, " +
            "[Organization].*, " +
            "[ClientAgreement].*, " +
            "[Agreement].*, " +
            "[Currency].*, " +
            "CASE WHEN ([ClientType].Type = 0 OR [Client].IsTemporaryClient = 1) THEN 0 " +
            "ELSE 1 END [ContractorType] " +
            "FROM [ProductIncome] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturnItem].ID = [ProductIncomeItem].SaleReturnItemID " +
            "LEFT JOIN [SaleReturn] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SaleReturn].ClientID " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "AND [ClientInRole].Deleted = 0 " +
            "LEFT JOIN [ClientType] " +
            "ON [ClientType].ID = [ClientInRole].ClientTypeID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [SaleReturnItem].ID IS NOT NULL " +
            "AND [ProductIncome].ID IN @Ids ";

        Type[] clientReturnsTypes = {
            typeof(ProductIncome),
            typeof(ProductIncomeItem),
            typeof(SaleReturnItem),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(Currency),
            typeof(ContractorType)
        };

        Func<object[], ProductIncome> clientReturnsMapper = objects => {
            ProductIncome productIncome = (ProductIncome)objects[0];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[1];
            SaleReturnItem saleReturnItem = (SaleReturnItem)objects[2];
            Client client = (Client)objects[3];
            ClientAgreement clientAgreement = (ClientAgreement)objects[4];
            Agreement agreement = (Agreement)objects[5];
            Organization organization = (Organization)objects[6];
            Currency currency = (Currency)objects[7];
            ContractorType contractorType = (ContractorType)objects[8];

            if (documents.Any(i => i.TempId.Equals(productIncomeItem.ProductIncomeId))) {
                GenericDocument document = documents.First(i => i.TempId.Equals(productIncomeItem.ProductIncomeId));

                document.Amount += decimal.Round(saleReturnItem.Amount * saleReturnItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero);
            } else {
                clientAgreement.Agreement = agreement;

                documents.Add(new GenericDocument {
                    TempId = productIncome.Id,
                    SynchronizationDate = productIncome.Created,
                    Number = productIncome.Number,
                    Type = "Прихідна накладна (повернення)",
                    Amount = decimal.Round(saleReturnItem.Amount * saleReturnItem.ExchangeRateAmount, 2, MidpointRounding.AwayFromZero),
                    Currency = currency,
                    Client = client,
                    ClientAgreement = clientAgreement,
                    Organization = organization,
                    ContractorType = contractorType
                });
            }

            return productIncome;
        };

        _connection.Query(
            clientReturnsQuery,
            clientReturnsTypes,
            clientReturnsMapper,
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Ids = ids },
            splitOn: "ID,ContractorType"
        );
    }

    private void GetAllMappedProductCapitalizations(List<GenericDocument> documents, IEnumerable<long> ids) {
        Currency eur = _connection.Query<Currency>(
            "SELECT TOP 1 * FROM [Currency] " +
            "WHERE [Currency].[Deleted] = 0 " +
            "AND [Currency].[Code] = 'EUR' ").FirstOrDefault();

        string capitalizationsQuery =
            "SELECT " +
            "[ProductIncome].*, " +
            "[ProductIncomeItem].*, " +
            "[ProductCapitalizationItem].*, " +
            "[Organization].*, " +
            "[Responsible].* " +
            "FROM [ProductIncome] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ID = [ProductIncomeItem].ProductCapitalizationItemID " +
            "LEFT JOIN [ProductCapitalization] " +
            "ON [ProductCapitalization].ID = [ProductCapitalizationItem].ProductCapitalizationID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [ProductCapitalization].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [ProductCapitalization].ResponsibleID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductCapitalization].StorageID " +
            "WHERE [ProductIncome].IsFromOneC = 1 " +
            "AND [ProductCapitalizationItem].ID IS NOT NULL " +
            "AND [ProductIncome].ID IN @Ids ";

        Type[] capitalizationsTypes = {
            typeof(ProductIncome),
            typeof(ProductIncomeItem),
            typeof(ProductCapitalizationItem),
            typeof(Organization),
            typeof(User)
        };

        Func<object[], ProductIncome> capitalizationsMapper = objects => {
            ProductIncome productIncome = (ProductIncome)objects[0];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[1];
            ProductCapitalizationItem productCapitalizationItem = (ProductCapitalizationItem)objects[2];
            Organization organization = (Organization)objects[3];
            User responsible = (User)objects[4];

            if (documents.Any(i => i.TempId.Equals(productIncomeItem.ProductIncomeId))) {
                GenericDocument document = documents.First(i => i.TempId.Equals(productIncomeItem.ProductIncomeId));

                document.Amount += decimal.Round(productCapitalizationItem.UnitPrice * Convert.ToDecimal(productCapitalizationItem.Qty), 2, MidpointRounding.AwayFromZero);
            } else {
                documents.Add(new GenericDocument {
                    TempId = productIncome.Id,
                    SynchronizationDate = productIncome.Created,
                    Number = productIncome.Number,
                    Type = "Прихідна накладна (Оприходування)",
                    Amount = decimal.Round(productCapitalizationItem.UnitPrice * Convert.ToDecimal(productCapitalizationItem.Qty), 2, MidpointRounding.AwayFromZero),
                    Currency = eur,
                    Organization = organization,
                    ContractorType = ContractorType.None
                });
            }

            return productIncome;
        };

        _connection.Query(
            capitalizationsQuery,
            capitalizationsTypes,
            capitalizationsMapper,
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Ids = ids }
        );
    }

    private void GetAllMappedPackageOrders(List<GenericDocument> documents, IEnumerable<long> ids) {
        string packListsQuery =
            "SELECT " +
            "[ProductIncome].*, " +
            "[ProductIncomeItem].*, " +
            "[PackingListPackageOrderItem].*, " +
            "[Client].*, " +
            "[Organization].*, " +
            "[ClientAgreement].*, " +
            "[Agreement].*, " +
            "[Currency].*, " +
            "CASE WHEN ([ClientType].Type = 0 OR [Client].IsTemporaryClient = 1) THEN 0 " +
            "ELSE 1 END [ContractorType] " +
            "FROM [ProductIncome] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [PackingListPackage] " +
            "ON [PackingListPackage].ID = [PackingListPackageOrderItem].PackingListPackageID " +
            "LEFT JOIN [PackingList] AS [PackagePackingList] " +
            "ON [PackagePackingList].ID = [PackingListPackage].PackingListID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [SupplyInvoiceOrderItem].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "AND [ClientInRole].Deleted = 0 " +
            "LEFT JOIN [ClientType] " +
            "ON [ClientType].ID = [ClientInRole].ClientTypeID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [PackingListPackageOrderItem].ID IS NOT NULL " +
            "AND [ProductIncome].ID IN @Ids ";

        Type[] packListsTypes = {
            typeof(ProductIncome),
            typeof(ProductIncomeItem),
            typeof(PackingListPackageOrderItem),
            typeof(Client),
            typeof(Organization),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(ContractorType)
        };

        Func<object[], ProductIncome> packListsMapper = objects => {
            ProductIncome productIncome = (ProductIncome)objects[0];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[1];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[2];
            Client client = (Client)objects[3];
            Organization organization = (Organization)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Currency currency = (Currency)objects[7];
            ContractorType contractorType = (ContractorType)objects[8];

            if (documents.Any(i => i.TempId.Equals(productIncomeItem.ProductIncomeId))) {
                GenericDocument document = documents.First(i => i.TempId.Equals(productIncomeItem.ProductIncomeId));

                document.Amount += packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty);
            } else {
                clientAgreement.Agreement = agreement;

                documents.Add(new GenericDocument {
                    TempId = productIncome.Id,
                    SynchronizationDate = productIncome.Created,
                    Number = productIncome.Number,
                    Type = "Прихідна накладна від виробника",
                    Amount = packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty),
                    Currency = currency,
                    Client = client,
                    ClientAgreement = clientAgreement,
                    Organization = organization,
                    ContractorType = contractorType
                });
            }

            return productIncome;
        };

        _connection.Query(
            packListsQuery,
            packListsTypes,
            packListsMapper,
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Ids = ids },
            splitOn: "ID,ContractorType"
        );
    }

    private void GetAllMappedActReconciliations(List<GenericDocument> documents, IEnumerable<long> ids) {
        string culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        string reconciliationsSqlExpression =
            "SELECT " +
            "[ProductIncome].* " +
            ", [ProductIncomeItem].* " +
            ", [ActReconciliationItem].* " +
            ", [ActReconciliation].* " +
            ", [SupplyInvoice].* " +
            ", [SupplyOrder].* " +
            ", [SupplyOrderOrganization].* " +
            ", [Client].* " +
            ", [ClientAgreement].* " +
            ", [Agreement].* " +
            ", [Currency].* " +
            ", CASE WHEN ([ClientType].Type = 0 OR [Client].IsTemporaryClient = 1) THEN 0 " +
            "ELSE 1 END [ContractorType] " +
            "FROM [ProductIncome] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
            "LEFT JOIN [ActReconciliationItem] " +
            "ON [ProductIncomeItem].ActReconciliationItemID = [ActReconciliationItem].ID " +
            "LEFT JOIN [ActReconciliation] " +
            "ON [ActReconciliationItem].ActReconciliationID = [ActReconciliation].ID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [ActReconciliation].SupplyInvoiceID = [SupplyInvoice].ID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [views].[OrganizationView] AS [SupplyOrderOrganization] " +
            "ON [SupplyOrderOrganization].ID = [SupplyOrder].OrganizationID " +
            "AND [SupplyOrderOrganization].CultureCode = @Culture " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "LEFT JOIN [ClientType] " +
            "ON [ClientType].ID = [ClientInRole].ClientTypeID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [ProductIncome].ID IN @Ids " +
            "AND [ActReconciliationItem].ID IS NOT NULL ";

        Type[] reconciliationsTypes = {
            typeof(ProductIncome),
            typeof(ProductIncomeItem),
            typeof(ActReconciliationItem),
            typeof(ActReconciliation),
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(Organization),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency)
        };

        Func<object[], ProductIncomeItem> reconciliationsMapper = objects => {
            ProductIncome productIncome = (ProductIncome)objects[0];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[1];
            ActReconciliationItem actReconciliationItem = (ActReconciliationItem)objects[2];
            ActReconciliation actReconciliation = (ActReconciliation)objects[3];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[4];
            SupplyOrder supplyOrder = (SupplyOrder)objects[5];
            Organization supplyOrderOrganization = (Organization)objects[6];
            Client supplyOrderSupplier = (Client)objects[7];
            ClientAgreement clientAgreement = (ClientAgreement)objects[8];
            Agreement agreement = (Agreement)objects[9];
            Currency currency = (Currency)objects[10];
            ContractorType contractorType = (ContractorType)objects[11];

            if (documents.Any(i => i.TempId.Equals(productIncomeItem.ProductIncomeId))) {
                GenericDocument document = documents.First(i => i.TempId.Equals(productIncomeItem.ProductIncomeId));

                document.Amount += decimal.Round(actReconciliationItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero);
            } else {
                clientAgreement.Agreement = agreement;

                documents.Add(new GenericDocument {
                    TempId = productIncome.Id,
                    SynchronizationDate = productIncome.Created,
                    Number = productIncome.Number,
                    Type = "Прихідна накладна від виробника",
                    Amount = decimal.Round(actReconciliationItem.UnitPrice * Convert.ToDecimal(productIncomeItem.Qty), 2, MidpointRounding.AwayFromZero),
                    Currency = currency,
                    Client = supplyOrderSupplier,
                    ClientAgreement = clientAgreement,
                    Organization = supplyOrderOrganization,
                    ContractorType = contractorType
                });
            }

            return productIncomeItem;
        };

        _connection.Query(
            reconciliationsSqlExpression,
            reconciliationsTypes,
            reconciliationsMapper,
            new { Culture = culture, Ids = ids },
            splitOn: "ID,ContractorType"
        );

        // What the hell is this for ?
        // string reconciliationsOrderSqlQuery =
        //     "SELECT " +
        //     "[ProductIncome].* " +
        //     ", [SupplyOrderUkraine].* " +
        //     ", [User].* " +
        //     ", [Organization].* " +
        //     ", [Supplier].* " +
        //     ", [CurrencySupplyOrderUkraine].* " +
        //     "FROM [SupplyOrderUkraine] " +
        //     "LEFT JOIN [User] " +
        //     "ON [User].ID = [SupplyOrderUkraine].ResponsibleID " +
        //     "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
        //     "ON [Organization].ID = [SupplyOrderUkraine].OrganizationID " +
        //     "AND [Organization].CultureCode = @Culture " +
        //     "LEFT JOIN [Client] AS [Supplier] " +
        //     "ON [Supplier].ID = [SupplyOrderUkraine].SupplierID " +
        //     "LEFT JOIN [ClientAgreement] AS [ClientAgreementSupplyOrderUkraine] " +
        //     "ON [ClientAgreementSupplyOrderUkraine].[ID] = [SupplyOrderUkraine].[ClientAgreementID] " +
        //     "LEFT JOIN [Agreement] AS [AgreementSupplyOrderUkraine] " +
        //     "ON [AgreementSupplyOrderUkraine].[ID] = [ClientAgreementSupplyOrderUkraine].[AgreementID] " +
        //     "LEFT JOIN [Currency] AS [CurrencySupplyOrderUkraine] " +
        //     "ON [CurrencySupplyOrderUkraine].[ID] = [AgreementSupplyOrderUkraine].[CurrencyID] " +
        //     "LEFT JOIN [ActReconciliation] " +
        //     "ON [ActReconciliation].[SupplyOrderUkraineID] = [SupplyOrderUkraine].[ID] " +
        //     "LEFT JOIN [ActReconciliationItem] " +
        //     "ON [ActReconciliationItem].[ActReconciliationID] = [ActReconciliation].[ID] " +
        //     "LEFT JOIN [ProductIncomeItem] " +
        //     "ON  [ProductIncomeItem].[ActReconciliationItemID] = [ActReconciliationItem].[ID] " +
        //     "LEFT JOIN [ProductIncome] " +
        //     "ON [ProductIncome].[ID] = [ProductIncomeItem].[ProductIncomeID] " +
        //     "WHERE [ProductIncome].[ID] IN @Ids ";
        //
        // Type[] reconciliationsOrderOrders = {
        //     typeof(ProductIncome),
        //     typeof(SupplyOrderUkraine),
        //     typeof(User),
        //     typeof(Organization),
        //     typeof(Client),
        //     typeof(Currency)
        // };
        //
        // Func<object[], ProductIncome> reconciliationsOrderMapper = objects => {
        //     ProductIncome productIncome = (ProductIncome)objects[0];
        //     SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[1];
        //     User responsible = (User)objects[2];
        //     Organization organization = (Organization)objects[3];
        //     Client supplier = (Client)objects[4];
        //     Currency currencySupplyOrderUkraine = (Currency)objects[5];
        //
        //     ProductIncome existProductIncome = incomes.First(x => x.Id.Equals(productIncome.Id));
        //
        //     if (existProductIncome.Currency == null)
        //         existProductIncome.Currency = currencySupplyOrderUkraine;
        //
        //     supplyOrderUkraine.Organization = organization;
        //     supplyOrderUkraine.Responsible = responsible;
        //     supplyOrderUkraine.Supplier = supplier;
        //
        //     if (existProductIncome.ProductIncomeItems.Any(x =>
        //             x.SupplyOrderUkraineItem.SupplyOrderUkraineId.Equals(supplyOrderUkraine.Id)))
        //         existProductIncome.ProductIncomeItems.First(x =>
        //                 x.SupplyOrderUkraineItem.SupplyOrderUkraineId.Equals(supplyOrderUkraine.Id))
        //             .SupplyOrderUkraineItem.SupplyOrderUkraine = supplyOrderUkraine;
        //
        //     return productIncome;
        // };

        // _connection.Query(
        //     reconciliationsOrderSqlQuery,
        //     reconciliationsOrderOrders,
        //     reconciliationsOrderMapper,
        //     new {
        //         Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
        //         Ids = incomes.Select(x => x.Id)
        //     });
    }

    private struct RowedResult {
        public RowedResult(long id, int rowNumber, DocumentAfterSyncType documentType, int totalQty) {
            Id = id;
            RowNumber = rowNumber;
            DocumentType = documentType;
            TotalQty = totalQty;
        }

        public long Id { get; set; }
        public int RowNumber { get; set; }
        public DocumentAfterSyncType DocumentType { get; set; }
        public int TotalQty { get; }
    }
}