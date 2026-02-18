using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductPlacementHistoryRepository : IProductPlacementHistoryRepository {
    private readonly IDbConnection _connection;

    public ProductPlacementHistoryRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ProductPlacementHistory productPlacementHistory) {
        _connection.Execute(
            "INSERT INTO [ProductPlacementHistory] " +
            "(Qty, Placement, ProductId, StorageId, UserId, StorageLocationType, AdditionType, Updated) " +
            "VALUES " +
            "(@Qty, @Placement, @ProductId, @StorageId, @UserId, @StorageLocationType, @AdditionType, GETUTCDATE())",
            productPlacementHistory
        );
    }

    public long AddWithId(ProductPlacementHistory productPlacementHistory) {
        return _connection.Query<long>(
            "INSERT INTO [ProductPlacementHistory] " +
            "(Qty, Placement, ProductId, StorageId, StorageLocationType, AdditionType, Updated) " +
            "VALUES " +
            "(@Qty, @Placement, @ProductId, @StorageId, @StorageLocationType, @AdditionType, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            productPlacementHistory
        ).Single();
    }

    public IEnumerable<ProductPlacementHistory> GetAllByProductAndStorageId(long productId) {
        return _connection.Query<ProductPlacementHistory>(
            "SELECT [ProductPlacementHistory].* " +
            "FROM [ProductPlacementHistory] " +
            "WHERE [ProductPlacementHistory].Deleted = 0 " +
            "AND [ProductPlacementHistory].ProductID = @ProductId " +
            new { ProductId = productId }
        );
    }

    public IEnumerable<ProductPlacementHistory> GetAllByProductId(long productId, DateTime from, DateTime to, long limit, long offset) {
        List<ProductPlacementHistory> productPlacementStorages = new();

        double totalRows = _connection.Query<double>(";WITH [Search_CTE] " +
                                                     "AS ( " +
                                                     "SELECT [ProductPlacementHistory].ID " +
                                                     "FROM [ProductPlacementHistory] " +
                                                     "LEFT JOIN [Product] " +
                                                     "ON [Product].ID = [ProductPlacementHistory].ProductId " +
                                                     "LEFT JOIN [Storage] " +
                                                     "ON [Storage].ID = [ProductPlacementHistory].StorageId " +
                                                     "LEFT JOIN [User] " +
                                                     "ON [User].ID = [ProductPlacementHistory].UserId " +
                                                     "WHERE [ProductPlacementHistory].Deleted = 0 " +
                                                     "AND [ProductPlacementHistory].Created >= @From " +
                                                     "AND [ProductPlacementHistory].Created <= @To " +
                                                     "), " +
                                                     "[Rowed_CTE] " +
                                                     "AS ( " +
                                                     "SELECT [Search_CTE].ID " +
                                                     ", ROW_NUMBER() OVER(ORDER BY [Search_CTE].ID DESC) AS [RowNumber] " +
                                                     "FROM [Search_CTE] " +
                                                     ") " +
                                                     "SELECT COUNT(*) AS [TotalRowQty] " +
                                                     "FROM [ProductPlacementHistory] " +
                                                     "LEFT JOIN [Product] " +
                                                     "ON [Product].ID = [ProductPlacementHistory].ProductId " +
                                                     "LEFT JOIN [Storage] " +
                                                     "ON [Storage].ID = [ProductPlacementHistory].StorageId " +
                                                     "LEFT JOIN [User] " +
                                                     "ON [User].ID = [ProductPlacementHistory].UserId " +
                                                     "WHERE [ProductPlacementHistory].ID IN ( " +
                                                     "SELECT [Rowed_CTE].ID " +
                                                     "FROM [Rowed_CTE] " +
                                                     ") " +
                                                     "AND [ProductPlacementHistory].ProductId = @ProductId"
            , new {
                ProductId = productId,
                From = from,
                To = to
            }
        ).FirstOrDefault();

        _connection.Query<ProductPlacementHistory, Product, Storage, User, ProductPlacementHistory>(
            ";WITH [Search_CTE] " +
            "AS ( " +
            "SELECT [ProductPlacementHistory].ID " +
            "FROM [ProductPlacementHistory] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductPlacementHistory].ProductId " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductPlacementHistory].StorageId " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductPlacementHistory].UserId " +
            "WHERE [ProductPlacementHistory].Deleted = 0 " +
            "AND [ProductPlacementHistory].Created >= @From " +
            "AND [ProductPlacementHistory].Created <= @To " +
            "AND [ProductPlacementHistory].ProductId = @ProductId " +
            "), " +
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT [Search_CTE].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [Search_CTE].ID DESC) AS [RowNumber] " +
            "FROM [Search_CTE] " +
            ") " +
            "SELECT [ProductPlacementHistory].* " +
            ",[Product].* " +
            ",[Storage].* " +
            ",[User].* " +
            "FROM [ProductPlacementHistory] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductPlacementHistory].ProductId " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductPlacementHistory].StorageId " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductPlacementHistory].UserId " +
            "WHERE [ProductPlacementHistory].ID IN ( " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            "WHERE [Rowed_CTE].RowNumber > @Offset " +
            "AND [Rowed_CTE].RowNumber <= @Limit + @Offset " +
            ") " +
            "AND [ProductPlacementHistory].ProductId = @ProductId",
            (productPlacementStorage, product, storage, user) => {
                if (!productPlacementStorages.Any(x => x.Id.Equals(productPlacementStorage.Id))) {
                    productPlacementStorages.Add(productPlacementStorage);
                    productPlacementStorage.TotalRowsQty = totalRows;
                    productPlacementStorage.Product = product;
                    productPlacementStorage.User = user;
                    productPlacementStorage.Storage = storage;
                } else {
                    productPlacementStorage.TotalRowsQty = totalRows;
                    productPlacementStorage.Product = product;
                    productPlacementStorage.User = user;
                    productPlacementStorage.Storage = storage;
                }

                return productPlacementStorage;
            }, new {
                ProductId = productId,
                From = from,
                To = to,
                Limit = limit,
                Offset = offset
            });
        return productPlacementStorages;
    }

    public ProductPlacementHistory GetById(long id) {
        return _connection.Query<ProductPlacementHistory>(
            "SELECT * " +
            "FROM [ProductPlacementHistory] " +
            "WHERE [ProductPlacementHistory].ID = @Id " +
            "AND [ProductPlacementHistory].Deleted = 0",
            new { Id = id }
        ).SingleOrDefault();
    }

    public ProductPlacementHistory GetLastByProductAndStorageId(long productId, long storageId) {
        return _connection.Query<ProductPlacementHistory>(
            "SELECT [ProductPlacementHistory].* " +
            "FROM [ProductPlacementHistory] " +
            "WHERE [ProductPlacementHistory].Deleted = 0 " +
            "AND [ProductPlacementHistory].ProductID = @ProductId " +
            "AND [ProductPlacementHistory].StorageID = @StorageId ",
            new { ProductId = productId, StorageId = storageId }
        ).SingleOrDefault();
    }

    public ProductPlacementHistory GetNonByProductAndStorageId(long productId, long storageId) {
        throw new NotImplementedException();
    }
}