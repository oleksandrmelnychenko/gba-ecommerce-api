using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Common.Helpers;
using GBA.Common.Helpers.StockStateStorage;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.History.Contracts;

namespace GBA.Domain.Repositories.History;

public class StockStateStorageRepository : IStockStateStorageRepository {
    private readonly IDbConnection _connection;

    public StockStateStorageRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(StockStateStorage stockStateStorage) {
        return _connection.Query<long>(
            "INSERT INTO StockStateStorage (ChangeTypeOrderItem, TotalReservedUK, TotalCartReservedUK, QtyHistory, ProductId, SaleId, UserID, SaleNumberId, Updated) " +
            "VALUES (@ChangeTypeOrderItem, @TotalReservedUK, @TotalCartReservedUK, @QtyHistory, @ProductId, @SaleId,  @UserID, @SaleNumberId, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            stockStateStorage
        ).Single();
    }

    public StockStateStorage GetId(long Id) {
        throw new NotImplementedException();
    }

    public StockStateStorage GetNetId(Guid NetId) {
        throw new NotImplementedException();
    }

    public List<ProductPlacementDataHistory>
        GetVerificationAllFilteredProductPlacementHistory(long[] storageId, DateTime from, DateTime to, long limit, long offset, string value) {
        List<ProductPlacementDataHistory> productPlacementDataHistoryList = new();
        dynamic dataBaseName = NoltFolderManager.GetCrmServerDataBaseUrl();
        int[] types = new[] {
            (int)ChangeTypeOrderItem.SetLastStep,
            (int)ChangeTypeOrderItem.ActEditTheInvoice,
            (int)ChangeTypeOrderItem.Return,
            (int)ChangeTypeOrderItem.DepreciatedOrder,
            (int)ChangeTypeOrderItem.ProductPlacementUpdate,
            (int)ChangeTypeOrderItem.AddProductCapitalization,
            (int)ChangeTypeOrderItem.DepreciatedOrderFile
        };
        string sqlMapperIdsSum =
            ";WITH Search_CTE AS ( " +
            "SELECT [ProductPlacementDataHistory].ID, [ProductPlacementDataHistory].ProductId,[StockStateStorage].ID as StockStateStorageID " +
            "FROM [ProductPlacementDataHistory] " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[Product] AS ProductConcordDb " +
            "ON ProductConcordDb.ID = [ProductPlacementDataHistory].ProductId " +
            "LEFT JOIN [ProductAvailabilityDataHistory] " +
            "ON [ProductAvailabilityDataHistory].ID = [ProductPlacementDataHistory].ProductAvailabilityDataHistoryID " +
            "LEFT JOIN [StockStateStorage] " +
            "ON [StockStateStorage].ID = [ProductAvailabilityDataHistory].StockStateStorageID " +
            "WHERE [ProductPlacementDataHistory].Created <= @To " +
            "AND [ProductPlacementDataHistory].Created >= @From " +
            "AND [ProductPlacementDataHistory].StorageId IN @StorageIds " +
            "AND [StockStateStorage].ChangeTypeOrderItem IN @ChangeTypeOrderItems " +
            "AND [ProductPlacementDataHistory].ID IS NOT NULL " +
            "), " +
            "UniqueProducts_CTE AS (  " +
            "SELECT [StockStateStorage].ID, " +
            "[StockStateStorage].ProductId, " +
            "ROW_NUMBER() OVER (PARTITION BY [StockStateStorage].ProductId, [ProductPlacementDataHistory].StorageId ORDER BY [StockStateStorage].Created DESC) AS RowNum " +
            "FROM [StockStateStorage] " +
            "LEFT JOIN ProductAvailabilityDataHistory " +
            "ON ProductAvailabilityDataHistory.StockStateStorageID = StockStateStorage.ID " +
            "LEFT JOIN [ProductPlacementDataHistory] " +
            "ON [ProductPlacementDataHistory].ProductAvailabilityDataHistoryID = [ProductAvailabilityDataHistory].ID " +
            "WHERE [ProductPlacementDataHistory].Created <= @To " +
            "AND [ProductPlacementDataHistory].Created >= @From " +
            "AND [ProductPlacementDataHistory].StorageId IN @StorageIds " +
            "AND [StockStateStorage].ChangeTypeOrderItem IN @ChangeTypeOrderItems " +
            "AND [ProductPlacementDataHistory].ID IS NOT NULL " +
            "), " +
            "FilteredUnique_CTE AS ( " +
            "SELECT [UniqueProducts_CTE].ID " +
            "FROM [UniqueProducts_CTE] " +
            "WHERE [UniqueProducts_CTE].RowNum = 1 " +
            "AND [UniqueProducts_CTE].ID IN ( " +
            "SELECT [Search_CTE].StockStateStorageID " +
            "FROM [Search_CTE] " +
            ") " +
            "), " +
            "Rowed_CTE AS ( " +
            "SELECT [ProductPlacementDataHistory].ID, " +
            "ROW_NUMBER() OVER (ORDER BY [ProductPlacementDataHistory].ID DESC) AS RowNumber " +
            "FROM [ProductPlacementDataHistory] " +
            "WHERE [ProductPlacementDataHistory].ProductAvailabilityDataHistoryID IN ( " +
            "SELECT [ProductAvailabilityDataHistory].ID " +
            "FROM [ProductAvailabilityDataHistory] " +
            "WHERE [ProductAvailabilityDataHistory].StockStateStorageID IN ( " +
            "SELECT [FilteredUnique_CTE].ID FROM [FilteredUnique_CTE] " +
            ") " +
            ") " +
            ") " +
            "Select " +
            "[ProductPlacementDataHistory].ID " +
            "FROM [ProductPlacementDataHistory] " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[Product] as ProductConcordDb  " +
            "ON ProductConcordDb.ID = [ProductPlacementDataHistory].ProductId " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[Storage] as StorageConcordDb " +
            "ON StorageConcordDb.ID = [ProductPlacementDataHistory].StorageId " +
            "LEFT JOIN [ProductAvailabilityDataHistory] " +
            "ON [ProductAvailabilityDataHistory].ID = [ProductPlacementDataHistory].ProductAvailabilityDataHistoryID " +
            "LEFT JOIN [StockStateStorage] " +
            "ON [StockStateStorage].ID = [ProductAvailabilityDataHistory].StockStateStorageID " +
            "WHERE [ProductPlacementDataHistory].Created <= @To " +
            "AND [ProductPlacementDataHistory].Created >= @From " +
            "AND [ProductPlacementDataHistory].StorageId IN @StorageIds " +
            "AND [StockStateStorage].ChangeTypeOrderItem IN @ChangeTypeOrderItems " +
            "AND [ProductPlacementDataHistory].ID IN ( " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            ") " +
            "ORDER BY [ProductPlacementDataHistory].ID DESC; ";

        IEnumerable<long> productPlacementDataHistoryIdsSum =
            _connection.Query<long>(
                sqlMapperIdsSum,
                new {
                    To = to,
                    From = from,
                    Value = value != null ? value.ToLower() : null,
                    Limit = limit,
                    Offset = offset,
                    StorageIds = storageId,
                    ChangeTypeOrderItems = types
                }
            );

        string sqlMapperIds =
            ";WITH Search_CTE AS ( " +
            "SELECT [ProductPlacementDataHistory].ID, [ProductPlacementDataHistory].ProductId,[StockStateStorage].ID as StockStateStorageID " +
            "FROM [ProductPlacementDataHistory] " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[Product] AS ProductConcordDb " +
            "ON ProductConcordDb.ID = [ProductPlacementDataHistory].ProductId " +
            "LEFT JOIN [ProductAvailabilityDataHistory] " +
            "ON [ProductAvailabilityDataHistory].ID = [ProductPlacementDataHistory].ProductAvailabilityDataHistoryID " +
            "LEFT JOIN [StockStateStorage] " +
            "ON [StockStateStorage].ID = [ProductAvailabilityDataHistory].StockStateStorageID " +
            "WHERE [ProductPlacementDataHistory].Created <= @To " +
            "AND [ProductPlacementDataHistory].Created >= @From " +
            "AND [ProductPlacementDataHistory].StorageId IN @StorageIds " +
            "AND [StockStateStorage].ChangeTypeOrderItem IN @ChangeTypeOrderItems " +
            "AND [ProductPlacementDataHistory].ID IS NOT NULL " +
            "), " +
            "UniqueProducts_CTE AS (  " +
            "SELECT [StockStateStorage].ID, " +
            "[StockStateStorage].ProductId, " +
            "ROW_NUMBER() OVER (PARTITION BY [StockStateStorage].ProductId, [ProductPlacementDataHistory].StorageId ORDER BY [StockStateStorage].Created DESC) AS RowNum " +
            "FROM [StockStateStorage] " +
            "LEFT JOIN ProductAvailabilityDataHistory " +
            "ON ProductAvailabilityDataHistory.StockStateStorageID = StockStateStorage.ID " +
            "LEFT JOIN [ProductPlacementDataHistory] " +
            "ON [ProductPlacementDataHistory].ProductAvailabilityDataHistoryID = [ProductAvailabilityDataHistory].ID " +
            "WHERE [ProductPlacementDataHistory].Created <= @To " +
            "AND [ProductPlacementDataHistory].Created >= @From " +
            "AND [ProductPlacementDataHistory].StorageId IN @StorageIds " +
            "AND [StockStateStorage].ChangeTypeOrderItem IN @ChangeTypeOrderItems " +
            "AND [ProductPlacementDataHistory].ID IS NOT NULL " +
            "), " +
            "FilteredUnique_CTE AS ( " +
            "SELECT [UniqueProducts_CTE].ID " +
            "FROM [UniqueProducts_CTE] " +
            "WHERE [UniqueProducts_CTE].RowNum = 1 " +
            "AND [UniqueProducts_CTE].ID IN ( " +
            "SELECT [Search_CTE].StockStateStorageID " +
            "FROM [Search_CTE] " +
            ") " +
            "), " +
            "Rowed_CTE AS ( " +
            "SELECT [ProductPlacementDataHistory].ID, " +
            "ROW_NUMBER() OVER (ORDER BY [ProductPlacementDataHistory].ID DESC) AS RowNumber " +
            "FROM [ProductPlacementDataHistory] " +
            "WHERE [ProductPlacementDataHistory].ProductAvailabilityDataHistoryID IN ( " +
            "SELECT [ProductAvailabilityDataHistory].ID " +
            "FROM [ProductAvailabilityDataHistory] " +
            "WHERE [ProductAvailabilityDataHistory].StockStateStorageID IN ( " +
            "SELECT [FilteredUnique_CTE].ID FROM [FilteredUnique_CTE] " +
            ") " +
            ") " +
            ") " +
            "Select " +
            "[ProductPlacementDataHistory].*, " +
            "[ProductConcordDb].*, " +
            "[StorageConcordDb].* " +
            "FROM [ProductPlacementDataHistory] " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[Product] as ProductConcordDb  " +
            "ON ProductConcordDb.ID = [ProductPlacementDataHistory].ProductId " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[Storage] as StorageConcordDb " +
            "ON StorageConcordDb.ID = [ProductPlacementDataHistory].StorageId " +
            "LEFT JOIN [ProductAvailabilityDataHistory] " +
            "ON [ProductAvailabilityDataHistory].ID = [ProductPlacementDataHistory].ProductAvailabilityDataHistoryID " +
            "LEFT JOIN [StockStateStorage] " +
            "ON [StockStateStorage].ID = [ProductAvailabilityDataHistory].StockStateStorageID " +
            "WHERE [ProductPlacementDataHistory].Created <= @To " +
            "AND [ProductPlacementDataHistory].Created >= @From " +
            "AND [ProductPlacementDataHistory].StorageId IN @StorageIds " +
            "AND [StockStateStorage].ChangeTypeOrderItem IN @ChangeTypeOrderItems " +
            "AND [ProductPlacementDataHistory].ID IN ( " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] ";

        if (limit != 0)
            sqlMapperIds +=
                "WHERE RowNumber > @Offset " +
                "AND RowNumber <= @Limit + @Offset ";
        sqlMapperIds += ")" +
                        "ORDER BY [ProductPlacementDataHistory].ID DESC; ";

        IEnumerable<ProductPlacementDataHistory> productPlacementDataHistories =
            _connection.Query<ProductPlacementDataHistory, Product, Storage, ProductPlacementDataHistory>(
                sqlMapperIds,
                (productPlacementDataHistory, product, storage) => {
                    if (!productPlacementDataHistoryList.Any(x => x.Id.Equals(productPlacementDataHistory.Id))) {
                        productPlacementDataHistory.Product = product;
                        productPlacementDataHistory.Storage = storage;
                        productPlacementDataHistoryList.Add(productPlacementDataHistory);
                    } else {
                        productPlacementDataHistory = productPlacementDataHistoryList.First(x => x.Id.Equals(productPlacementDataHistory.Id));
                    }

                    return productPlacementDataHistory;
                },
                new {
                    To = to,
                    From = from,
                    Value = value != null ? value.ToLower() : null,
                    Limit = limit + 1,
                    Offset = offset,
                    StorageIds = storageId,
                    ChangeTypeOrderItems = types
                }
            );

        foreach (ProductPlacementDataHistory productPlacementDataHistory in productPlacementDataHistoryList)
            productPlacementDataHistory.TotalRowQty = productPlacementDataHistoryIdsSum.Count();
        return productPlacementDataHistoryList;
    }

    public List<StockStateStorage> GetVerificationAllFiltered(long[] storageId, DateTime from, DateTime to, long limit, long offset, string value) {
        List<StockStateStorage> stockStateStorageList = new();
        dynamic dataBaseName = NoltFolderManager.GetCrmServerDataBaseUrl();
        int[] types = new[] {
            (int)ChangeTypeOrderItem.SetLastStep,
            (int)ChangeTypeOrderItem.ActEditTheInvoice,
            (int)ChangeTypeOrderItem.Return,
            (int)ChangeTypeOrderItem.DepreciatedOrder,
            (int)ChangeTypeOrderItem.AddProductCapitalization
        };
        string sqlMapperIdsSum =
            ";WITH Search_CTE AS (" +
            "SELECT [StockStateStorage].ID " +
            "FROM [StockStateStorage] " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[Product] as ProductConcordDb " +
            "ON ProductConcordDb.ID = StockStateStorage.ProductId " +
            "LEFT JOIN ProductAvailabilityDataHistory " +
            "ON ProductAvailabilityDataHistory.StockStateStorageID = StockStateStorage.ID " +
            "LEFT JOIN [ProductPlacementDataHistory] " +
            "ON [ProductPlacementDataHistory].ProductAvailabilityDataHistoryID = [ProductAvailabilityDataHistory].ID " +
            "WHERE [StockStateStorage].Created <= @To " +
            "AND [StockStateStorage].Created >= @from " +
            "AND StockStateStorage.ChangeTypeOrderItem IN @ChangeTypeOrderItems " +
            "AND ProductAvailabilityDataHistory.StorageId IN @StorageIds " +
            "AND [ProductPlacementDataHistory].ID IS NOT NULL";
        if (!string.IsNullOrEmpty(value))
            sqlMapperIdsSum +=
                "AND " +
                "( " +
                "PATINDEX('%' + @Value + '%', ProductConcordDb.VendorCode) > 0 " +
                "OR PATINDEX('%' + @Value + '%', ProductConcordDb.MainOriginalNumber) > 0 " +
                "OR PATINDEX('%' + @Value + '%', ProductConcordDb.NameUA) > 0 " +
                ") ";

        sqlMapperIdsSum +=
            "), " +
            "UniqueProducts_CTE AS ( " +
            "SELECT [StockStateStorage].ID, " +
            "[StockStateStorage].ProductId, " +
            "ROW_NUMBER() OVER (PARTITION BY [StockStateStorage].ProductId ORDER BY [StockStateStorage].Created DESC) AS RowNum " +
            "FROM [StockStateStorage] " +
            "WHERE [StockStateStorage].Created <= @To " +
            "AND [StockStateStorage].Created >= @from " +
            "), " +
            "FilteredUnique_CTE AS ( " +
            "SELECT [UniqueProducts_CTE].ID " +
            "FROM [UniqueProducts_CTE] " +
            "WHERE [UniqueProducts_CTE].RowNum = 1 " +
            "AND [UniqueProducts_CTE].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            ") " +
            "), " +
            "Rowed_CTE AS ( " +
            "SELECT [FilteredUnique_CTE].ID, " +
            "ROW_NUMBER() OVER (ORDER BY [FilteredUnique_CTE].ID DESC) AS RowNumber " +
            "FROM [FilteredUnique_CTE] " +
            ") " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            "ORDER BY [Rowed_CTE].ID DESC;";
        IEnumerable<long> stockStateStorageIdsSum =
            _connection.Query<long>(
                sqlMapperIdsSum,
                new {
                    To = to,
                    From = from,
                    Value = value != null ? value.ToLower() : null,
                    Limit = limit,
                    Offset = offset,
                    StorageIds = storageId,
                    ChangeTypeOrderItems = types
                }
            );

        string sqlMapperIds =
            ";WITH Search_CTE AS (" +
            "SELECT [StockStateStorage].ID " +
            "FROM [StockStateStorage] " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[Product] as ProductConcordDb " +
            "ON ProductConcordDb.ID = StockStateStorage.ProductId " +
            "LEFT JOIN [ProductAvailabilityDataHistory] " +
            "ON [ProductAvailabilityDataHistory].StockStateStorageID = [StockStateStorage].ID " +
            "LEFT JOIN [ProductPlacementDataHistory] " +
            "ON [ProductPlacementDataHistory].ProductAvailabilityDataHistoryID = [ProductAvailabilityDataHistory].ID " +
            "WHERE [StockStateStorage].Created <= @To " +
            "AND [StockStateStorage].Created >= @From " +
            "AND StockStateStorage.ChangeTypeOrderItem IN @ChangeTypeOrderItems " +
            "AND [ProductAvailabilityDataHistory].StorageId IN @StorageIds " +
            "AND [ProductPlacementDataHistory].ID IS NOT NULL";
        if (!string.IsNullOrEmpty(value))
            sqlMapperIds +=
                "AND " +
                "( " +
                "PATINDEX('%' + @Value + '%', ProductConcordDb.VendorCode) > 0 " +
                "OR PATINDEX('%' + @Value + '%', ProductConcordDb.MainOriginalNumber) > 0 " +
                "OR PATINDEX('%' + @Value + '%', ProductConcordDb.NameUA) > 0 " +
                ") ";

        sqlMapperIds +=
            "), " +
            "UniqueProducts_CTE AS ( " +
            "SELECT [StockStateStorage].ID, " +
            "[StockStateStorage].ProductId, " +
            "ROW_NUMBER() OVER (PARTITION BY [StockStateStorage].ProductId ORDER BY [StockStateStorage].Created DESC) AS RowNum " +
            "FROM [StockStateStorage] " +
            "WHERE [StockStateStorage].Created <= @To " +
            "AND [StockStateStorage].Created >= @From " +
            "), " +
            "FilteredUnique_CTE AS ( " +
            "SELECT [UniqueProducts_CTE].ID " +
            "FROM [UniqueProducts_CTE] " +
            "WHERE [UniqueProducts_CTE].RowNum = 1 " +
            "AND [UniqueProducts_CTE].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            ") " +
            "), " +
            "Rowed_CTE AS ( " +
            "SELECT [FilteredUnique_CTE].ID, " +
            "ROW_NUMBER() OVER (ORDER BY [FilteredUnique_CTE].ID DESC) AS RowNumber " +
            "FROM [FilteredUnique_CTE] " +
            ") " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] ";
        if (limit != 0)
            sqlMapperIds += "WHERE  [Rowed_CTE].RowNumber > @Offset " +
                            "AND [Rowed_CTE].RowNumber <= @Limit + @Offset ";
        sqlMapperIds += "ORDER BY [Rowed_CTE].ID DESC;";

        IEnumerable<long> stockStateStorageIds =
            _connection.Query<long>(
                sqlMapperIds,
                new {
                    To = to,
                    From = from,
                    Value = value != null ? value.ToLower() : null,
                    Limit = limit,
                    Offset = offset,
                    StorageIds = storageId,
                    ChangeTypeOrderItems = types
                }
            );

        string sqlMapper =
            "SELECT * " +
            "FROM [StockStateStorage] " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[Product] as ProductConcordDb " +
            "ON ProductConcordDb.ID = StockStateStorage.ProductId " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[Sale] as SaleConcordDb " +
            "ON SaleConcordDb.ID = StockStateStorage.SaleId " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[User] as UserConcordDb " +
            "ON UserConcordDb.ID = StockStateStorage.UserID " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[SaleNumber] as SaleNumberConcordDb " +
            "ON SaleNumberConcordDb.ID = StockStateStorage.SaleNumberId " +
            "LEFT JOIN [ProductAvailabilityDataHistory] " +
            "ON [ProductAvailabilityDataHistory].StockStateStorageID = [StockStateStorage].ID " +
            "LEFT JOIN [ProductPlacementDataHistory] " +
            "ON [ProductPlacementDataHistory].ProductAvailabilityDataHistoryID = [ProductAvailabilityDataHistory].ID " +
            "WHERE [StockStateStorage].ID IN @StockStateStorageIds " +
            "AND [ProductAvailabilityDataHistory].StorageId IN @StorageIds " +
            "ORDER BY [StockStateStorage].ID DESC; ";
        IEnumerable<StockStateStorage> StockStateStorages =
            _connection.Query<StockStateStorage, Product, Sale, User, SaleNumber, ProductAvailabilityDataHistory, ProductPlacementDataHistory, StockStateStorage>(
                sqlMapper,
                (stockStateStorage, product, sale, user, saleNumber, productAvailabilityDataHistory, productPlacementDataHistory) => {
                    if (!stockStateStorageList.Any(x => x.Id.Equals(stockStateStorage.Id))) {
                        stockStateStorage.User = user;
                        stockStateStorage.Sale = sale;
                        stockStateStorage.Product = product;
                        stockStateStorage.SaleNumber = saleNumber;
                        stockStateStorageList.Add(stockStateStorage);
                    } else {
                        stockStateStorage = stockStateStorageList.First(x => x.Id.Equals(stockStateStorage.Id));
                    }

                    if (productAvailabilityDataHistory != null) {
                        if (!stockStateStorage.ProductAvailabilityDataHistory.Any(x => x.Id.Equals(productAvailabilityDataHistory.Id)))
                            stockStateStorage.ProductAvailabilityDataHistory.Add(productAvailabilityDataHistory);
                        else
                            productAvailabilityDataHistory = stockStateStorage.ProductAvailabilityDataHistory.First(x => x.Id.Equals(productAvailabilityDataHistory.Id));
                    }

                    if (productPlacementDataHistory != null) {
                        if (!productAvailabilityDataHistory.ProductPlacementDataHistory.Any(x => x.Id.Equals(productPlacementDataHistory.Id)))
                            productAvailabilityDataHistory.ProductPlacementDataHistory.Add(productPlacementDataHistory);
                        else
                            productPlacementDataHistory = productAvailabilityDataHistory.ProductPlacementDataHistory.First(x => x.Id.Equals(productPlacementDataHistory.Id));
                    }

                    return stockStateStorage;
                },
                new {
                    StockStateStorageIds = stockStateStorageIds,
                    StorageIds = storageId,
                    To = to,
                    Value = value != null ? value.ToLower() : null
                }
            );


        List<long> storageIds = new();
        stockStateStorageList.ForEach(x => {
            storageIds.AddRange(x.ProductAvailabilityDataHistory.Select(p => (long)p.StorageId).ToList());
        });

        IEnumerable<Storage> storage =
            _connection.Query<Storage>(
                "SELECT * " +
                $"FROM [{dataBaseName}].[dbo].[Storage] " +
                $"WHERE [{dataBaseName}].[dbo].[Storage].ID IN @Ids",
                new { Ids = storageIds.Distinct() }
            );
        foreach (StockStateStorage stockStateStorage in stockStateStorageList) {
            foreach (ProductAvailabilityDataHistory item in stockStateStorage.ProductAvailabilityDataHistory) item.Storage = storage.FirstOrDefault(x => x.Id == item.StorageId);
            stockStateStorage.TotalRowQty = stockStateStorageIdsSum.Count();
        }

        return stockStateStorageList;
    }

    public List<StockStateStorage> GetAllFiltered(long[] storageId, DateTime from, DateTime to, long limit, long offset, string value) {
        List<StockStateStorage> stockStateStorageList = new();
        dynamic dataBaseName = NoltFolderManager.GetCrmServerDataBaseUrl();

        string sqlMapperIdsSum =
            ";WITH Search_CTE AS (" +
            "SELECT [StockStateStorage].ID " +
            "FROM [StockStateStorage] " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[Product] as ProductConcordDb " +
            "ON ProductConcordDb.ID = StockStateStorage.ProductId " +
            "LEFT JOIN ProductAvailabilityDataHistory " +
            "ON ProductAvailabilityDataHistory.StockStateStorageID = StockStateStorage.ID " +
            "WHERE [StockStateStorage].Created <= @To " +
            "AND ProductAvailabilityDataHistory.StorageId IN @StorageIds ";
        if (!string.IsNullOrEmpty(value))
            sqlMapperIdsSum +=
                "AND " +
                "( " +
                "PATINDEX('%' + @Value + '%', ProductConcordDb.VendorCode) > 0 " +
                "OR PATINDEX('%' + @Value + '%', ProductConcordDb.MainOriginalNumber) > 0 " +
                "OR PATINDEX('%' + @Value + '%', ProductConcordDb.NameUA) > 0 " +
                ") ";

        sqlMapperIdsSum +=
            "), " +
            "UniqueProducts_CTE AS ( " +
            "SELECT [StockStateStorage].ID, " +
            "[StockStateStorage].ProductId, " +
            "ROW_NUMBER() OVER (PARTITION BY [StockStateStorage].ProductId ORDER BY [StockStateStorage].Created DESC) AS RowNum " +
            "FROM [StockStateStorage] " +
            "WHERE [StockStateStorage].Created <= @To " +
            "), " +
            "FilteredUnique_CTE AS ( " +
            "SELECT [UniqueProducts_CTE].ID " +
            "FROM [UniqueProducts_CTE] " +
            "WHERE [UniqueProducts_CTE].RowNum = 1 " +
            "AND [UniqueProducts_CTE].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            ") " +
            "), " +
            "Rowed_CTE AS ( " +
            "SELECT [FilteredUnique_CTE].ID, " +
            "ROW_NUMBER() OVER (ORDER BY [FilteredUnique_CTE].ID DESC) AS RowNumber " +
            "FROM [FilteredUnique_CTE] " +
            ") " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            "ORDER BY [Rowed_CTE].ID DESC;";
        IEnumerable<long> stockStateStorageIdsSum =
            _connection.Query<long>(
                sqlMapperIdsSum,
                new {
                    To = to,
                    Value = value != null ? value.ToLower() : null,
                    Limit = limit,
                    Offset = offset,
                    StorageIds = storageId
                }
            );

        string sqlMapperIds =
            ";WITH Search_CTE AS (" +
            "SELECT [StockStateStorage].ID " +
            "FROM [StockStateStorage] " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[Product] as ProductConcordDb " +
            "ON ProductConcordDb.ID = StockStateStorage.ProductId " +
            "LEFT JOIN [ProductAvailabilityDataHistory] " +
            "ON [ProductAvailabilityDataHistory].StockStateStorageID = [StockStateStorage].ID " +
            "WHERE [StockStateStorage].Created <= @To " +
            "AND [ProductAvailabilityDataHistory].StorageId IN @StorageIds ";
        if (!string.IsNullOrEmpty(value))
            sqlMapperIds +=
                "AND " +
                "( " +
                "PATINDEX('%' + @Value + '%', ProductConcordDb.VendorCode) > 0 " +
                "OR PATINDEX('%' + @Value + '%', ProductConcordDb.MainOriginalNumber) > 0 " +
                "OR PATINDEX('%' + @Value + '%', ProductConcordDb.NameUA) > 0 " +
                ") ";

        sqlMapperIds +=
            "), " +
            "UniqueProducts_CTE AS ( " +
            "SELECT [StockStateStorage].ID, " +
            "[StockStateStorage].ProductId, " +
            "ROW_NUMBER() OVER (PARTITION BY [StockStateStorage].ProductId ORDER BY [StockStateStorage].Created DESC) AS RowNum " +
            "FROM [StockStateStorage] " +
            "WHERE [StockStateStorage].Created <= @To " +
            "), " +
            "FilteredUnique_CTE AS ( " +
            "SELECT [UniqueProducts_CTE].ID " +
            "FROM [UniqueProducts_CTE] " +
            "WHERE [UniqueProducts_CTE].RowNum = 1 " +
            "AND [UniqueProducts_CTE].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            ") " +
            "), " +
            "Rowed_CTE AS ( " +
            "SELECT [FilteredUnique_CTE].ID, " +
            "ROW_NUMBER() OVER (ORDER BY [FilteredUnique_CTE].ID DESC) AS RowNumber " +
            "FROM [FilteredUnique_CTE] " +
            ") " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            "WHERE  [Rowed_CTE].RowNumber > @Offset " +
            "AND [Rowed_CTE].RowNumber <= @Limit + @Offset " +
            "ORDER BY [Rowed_CTE].ID DESC;";

        IEnumerable<long> stockStateStorageIds =
            _connection.Query<long>(
                sqlMapperIds,
                new {
                    To = to,
                    Value = value != null ? value.ToLower() : null,
                    Limit = limit,
                    Offset = offset,
                    StorageIds = storageId
                }
            );


        Type[] types = {
            typeof(StockStateStorage),
            typeof(Product),
            typeof(Sale),
            typeof(User),
            typeof(SaleNumber),
            typeof(ProductAvailabilityDataHistory),
            typeof(ProductPlacementDataHistory),
            typeof(ConsignmentItem),
            typeof(Consignment),
            typeof(ProductIncome)
        };
        string sqlMapper =
            "SELECT * " +
            "FROM [StockStateStorage] " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[Product] as ProductConcordDb " +
            "ON ProductConcordDb.ID = StockStateStorage.ProductId " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[Sale] as SaleConcordDb " +
            "ON SaleConcordDb.ID = StockStateStorage.SaleId " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[User] as UserConcordDb " +
            "ON UserConcordDb.ID = StockStateStorage.UserID " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[SaleNumber] as SaleNumberConcordDb " +
            "ON SaleNumberConcordDb.ID = StockStateStorage.SaleNumberId " +
            "LEFT JOIN [ProductAvailabilityDataHistory] " +
            "ON [ProductAvailabilityDataHistory].StockStateStorageID = [StockStateStorage].ID " +
            "LEFT JOIN [ProductPlacementDataHistory] " +
            "ON [ProductPlacementDataHistory].ProductAvailabilityDataHistoryID = [ProductAvailabilityDataHistory].ID " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[ConsignmentItem] as ConsignmentItemDb " +
            "ON ConsignmentItemDb.ID = ProductPlacementDataHistory.ConsignmentItemId " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[Consignment] as ConsignmentDb " +
            "ON ConsignmentDb.ID = ConsignmentItemDb.ConsignmentID " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[ProductIncome] AS ProductIncomeDb " +
            "ON ProductIncomeDb.ID = ConsignmentDb.ProductIncomeID " +
            "WHERE [StockStateStorage].ID IN @StockStateStorageIds " +
            "ORDER BY [StockStateStorage].ID DESC; ";

        Func<object[], StockStateStorage> mapper = objects => {
            StockStateStorage stockStateStorage = (StockStateStorage)objects[0];
            Product product = (Product)objects[1];
            Sale sale = (Sale)objects[2];
            User user = (User)objects[3];
            SaleNumber saleNumber = (SaleNumber)objects[4];
            ProductAvailabilityDataHistory productAvailabilityDataHistory = (ProductAvailabilityDataHistory)objects[5];
            ProductPlacementDataHistory productPlacementDataHistory = (ProductPlacementDataHistory)objects[6];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[7];
            Consignment consignment = (Consignment)objects[8];
            ProductIncome productIncome = (ProductIncome)objects[9];
            if (!stockStateStorageList.Any(x => x.Id.Equals(stockStateStorage.Id))) {
                stockStateStorage.User = user;
                stockStateStorage.Sale = sale;
                stockStateStorage.Product = product;
                stockStateStorage.SaleNumber = saleNumber;
                stockStateStorageList.Add(stockStateStorage);
            } else {
                stockStateStorage = stockStateStorageList.First(x => x.Id.Equals(stockStateStorage.Id));
            }

            if (productAvailabilityDataHistory != null) {
                if (!stockStateStorage.ProductAvailabilityDataHistory.Any(x => x.Id.Equals(productAvailabilityDataHistory.Id)))
                    stockStateStorage.ProductAvailabilityDataHistory.Add(productAvailabilityDataHistory);
                else
                    productAvailabilityDataHistory = stockStateStorage.ProductAvailabilityDataHistory.First(x => x.Id.Equals(productAvailabilityDataHistory.Id));
            }

            if (productPlacementDataHistory != null) {
                if (!productAvailabilityDataHistory.ProductPlacementDataHistory.Any(x => x.Id.Equals(productPlacementDataHistory.Id))) {
                    if (consignmentItem != null) {
                        consignment.ProductIncome = productIncome;
                        consignmentItem.Consignment = consignment;
                        productPlacementDataHistory.ConsignmentItem = consignmentItem;
                    }

                    productAvailabilityDataHistory.ProductPlacementDataHistory.Add(productPlacementDataHistory);
                } else {
                    productPlacementDataHistory = productAvailabilityDataHistory.ProductPlacementDataHistory.First(x => x.Id.Equals(productPlacementDataHistory.Id));
                }
            }

            return stockStateStorage;
        };

        _connection.Query(sqlMapper, types, mapper,
            new {
                StockStateStorageIds = stockStateStorageIds,
                StorageIds = storageId,
                To = to,
                Value = value != null ? value.ToLower() : null
            }
        );


        List<long> storageIds = new();
        stockStateStorageList.ForEach(x => {
            storageIds.AddRange(x.ProductAvailabilityDataHistory.Select(p => (long)p.StorageId).ToList());
        });

        IEnumerable<Storage> storage =
            _connection.Query<Storage>(
                "SELECT * " +
                $"FROM [{dataBaseName}].[dbo].[Storage] " +
                $"WHERE [{dataBaseName}].[dbo].[Storage].ID IN @Ids",
                new { Ids = storageIds.Distinct() }
            );
        foreach (StockStateStorage stockStateStorage in stockStateStorageList) {
            foreach (ProductAvailabilityDataHistory item in stockStateStorage.ProductAvailabilityDataHistory) item.Storage = storage.FirstOrDefault(x => x.Id == item.StorageId);
            stockStateStorage.TotalRowQty = stockStateStorageIdsSum.Count();
        }

        return stockStateStorageList;
    }

    public List<StockStateStorage> GetAll(long[] storageId, DateTime to, string value) {
        List<StockStateStorage> stockStateStorageList = new();
        dynamic dataBaseName = NoltFolderManager.GetCrmServerDataBaseUrl();

        string sqlMapper =
            ";WITH Search_CTE AS ( " +
            "SELECT [StockStateStorage].ID " +
            "FROM [StockStateStorage] " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[Product] as ProductConcordDb " +
            "ON ProductConcordDb.ID = StockStateStorage.ProductId " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[Sale] as SaleConcordDb " +
            "ON SaleConcordDb.ID = StockStateStorage.SaleId " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[User] as UserConcordDb " +
            "ON UserConcordDb.ID = StockStateStorage.UserID " +
            $"LEFT JOIN [{dataBaseName}].[dbo].[SaleNumber] as SaleNumberConcordDb " +
            "ON SaleNumberConcordDb.ID = StockStateStorage.SaleNumberId " +
            "LEFT JOIN [ProductAvailabilityDataHistory] " +
            "ON [ProductAvailabilityDataHistory].StockStateStorageID = [StockStateStorage].ID " +
            "LEFT JOIN [ProductPlacementDataHistory] " +
            "ON [ProductPlacementDataHistory].ProductAvailabilityDataHistoryID = [ProductAvailabilityDataHistory].ID " +
            "WHERE [StockStateStorage].Created <= @To " +
            "AND ProductAvailabilityDataHistory.StorageId IN @StorageIds ";
        if (!string.IsNullOrEmpty(value))
            sqlMapper +=
                "AND " +
                "( " +
                "PATINDEX('%' + @Value + '%', ProductConcordDb.VendorCode) > 0 " +
                "OR PATINDEX('%' + @Value + '%', ProductConcordDb.MainOriginalNumber) > 0 " +
                "OR PATINDEX('%' + @Value + '%', ProductConcordDb.NameUA) > 0 " +
                ") ";
        sqlMapper += "), " +
                     "UniqueProducts_CTE AS ( " +
                     "SELECT [StockStateStorage].ID, " +
                     "[StockStateStorage].ProductId, " +
                     "ROW_NUMBER() OVER (PARTITION BY [StockStateStorage].ProductId ORDER BY [StockStateStorage].Created DESC) AS RowNum " +
                     "FROM [StockStateStorage] " +
                     "), " +
                     "FilteredUnique_CTE AS ( " +
                     "SELECT [UniqueProducts_CTE].ID " +
                     "FROM [UniqueProducts_CTE] " +
                     "WHERE [UniqueProducts_CTE].RowNum = 1 " +
                     "AND [UniqueProducts_CTE].ID IN ( " +
                     "SELECT [Search_CTE].ID " +
                     "FROM [Search_CTE] " +
                     ") " +
                     "), " +
                     "Rowed_CTE AS ( " +
                     "SELECT [FilteredUnique_CTE].ID, " +
                     "ROW_NUMBER() OVER (ORDER BY [FilteredUnique_CTE].ID DESC) AS RowNumber " +
                     "FROM [FilteredUnique_CTE] " +
                     ") " +
                     "SELECT * " +
                     "FROM StockStateStorage StockStateStorageDb " +
                     "JOIN Rowed_CTE ON StockStateStorageDb.ID = Rowed_CTE.ID " +
                     $"LEFT JOIN [{dataBaseName}].[dbo].[Product] AS ProductConcordDb " +
                     "ON ProductConcordDb.ID = StockStateStorageDb.ProductId " +
                     $"LEFT JOIN [{dataBaseName}].[dbo].[Sale] AS SaleConcordDb " +
                     "ON SaleConcordDb.ID = StockStateStorageDb.SaleId " +
                     $"LEFT JOIN [{dataBaseName}].[dbo].[User] AS UserConcordDb " +
                     "ON UserConcordDb.ID = StockStateStorageDb.UserID " +
                     $"LEFT JOIN [{dataBaseName}].[dbo].[SaleNumber] AS SaleNumberConcordDb " +
                     "ON SaleNumberConcordDb.ID = StockStateStorageDb.SaleNumberId " +
                     "LEFT JOIN ProductAvailabilityDataHistory " +
                     "ON ProductAvailabilityDataHistory.StockStateStorageID = StockStateStorageDb.ID " +
                     "LEFT JOIN ProductPlacementDataHistory " +
                     "ON ProductPlacementDataHistory.ProductAvailabilityDataHistoryID = ProductAvailabilityDataHistory.ID " +
                     "WHERE ProductAvailabilityDataHistory.StorageId IN @StorageIds " +
                     "ORDER BY Rowed_CTE.ID DESC;";
        IEnumerable<StockStateStorage> StockStateStorages =
            _connection.Query<StockStateStorage, Product, Sale, User, SaleNumber, ProductAvailabilityDataHistory, ProductPlacementDataHistory, StockStateStorage>(
                sqlMapper,
                (stockStateStorage, product, sale, user, saleNumber, productAvailabilityDataHistory, productPlacementDataHistory) => {
                    if (!stockStateStorageList.Any(x => x.Id.Equals(stockStateStorage.Id))) {
                        stockStateStorage.User = user;
                        stockStateStorage.Sale = sale;
                        stockStateStorage.Product = product;
                        stockStateStorage.SaleNumber = saleNumber;
                        stockStateStorageList.Add(stockStateStorage);
                    } else {
                        stockStateStorage = stockStateStorageList.First(x => x.Id.Equals(stockStateStorage.Id));
                    }

                    if (productAvailabilityDataHistory != null) {
                        if (!stockStateStorage.ProductAvailabilityDataHistory.Any(x => x.Id.Equals(productAvailabilityDataHistory.Id)))
                            stockStateStorage.ProductAvailabilityDataHistory.Add(productAvailabilityDataHistory);
                        else
                            productAvailabilityDataHistory = stockStateStorage.ProductAvailabilityDataHistory.First(x => x.Id.Equals(productAvailabilityDataHistory.Id));
                    }

                    if (productPlacementDataHistory != null) {
                        if (!productAvailabilityDataHistory.ProductPlacementDataHistory.Any(x => x.Id.Equals(productPlacementDataHistory.Id)))
                            productAvailabilityDataHistory.ProductPlacementDataHistory.Add(productPlacementDataHistory);
                        else
                            productPlacementDataHistory = productAvailabilityDataHistory.ProductPlacementDataHistory.First(x => x.Id.Equals(productPlacementDataHistory.Id));
                    }

                    return stockStateStorage;
                }, new {
                    StorageIds = storageId,
                    To = to,
                    Value = value != null ? value.ToLower() : null
                }
            );


        List<long> storageIds = new();
        stockStateStorageList.ForEach(x => {
            storageIds.AddRange(x.ProductAvailabilityDataHistory.Select(p => (long)p.StorageId).ToList());
        });

        IEnumerable<Storage> storage =
            _connection.Query<Storage>(
                "SELECT * " +
                $"FROM [{dataBaseName}].[dbo].[Storage] " +
                $"WHERE [{dataBaseName}].[dbo].[Storage].ID IN @Ids",
                new { Ids = storageIds.Distinct() }
            );
        foreach (StockStateStorage stockStateStorage in stockStateStorageList)
        foreach (ProductAvailabilityDataHistory item in stockStateStorage.ProductAvailabilityDataHistory)
            item.Storage = storage.FirstOrDefault(x => x.Id == item.StorageId);

        return stockStateStorageList;
    }
}