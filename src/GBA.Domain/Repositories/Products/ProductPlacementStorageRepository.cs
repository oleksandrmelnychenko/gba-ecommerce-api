using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductPlacementStorageRepository : IProductPlacementStorageRepository {
    private readonly IDbConnection _connection;

    public ProductPlacementStorageRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ProductPlacementStorage productPlacementStorage) {
        return _connection.Query<long>(
            "INSERT INTO [ProductPlacementStorage] (Qty, Placement, VendorCode, ProductPlacementId, ProductId, StorageId, Updated) " +
            "VALUES (@Qty, @Placement, @VendorCode, @ProductPlacementId, @ProductId, @StorageId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            productPlacementStorage
        ).Single();
    }

    public IEnumerable<ProductPlacementStorage> GetAll() {
        IEnumerable<ProductPlacementStorage> productPlacementStorages = _connection.Query<ProductPlacementStorage, ProductPlacement, Product, Storage, ProductPlacementStorage>(
            ";WITH [Search_CTE] " +
            "AS ( " +
            "SELECT [ProductPlacementStorage].ID " +
            "FROM [ProductPlacementStorage] " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ID = [ProductPlacementStorage].ProductPlacementId " +
            "LEFT JOIN [Product] " +
            "ON [ProductPlacement].ProductID = [Product].ID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductPlacement].StorageID " +
            "), " +
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT [Search_CTE].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [Search_CTE].ID DESC) AS [RowNumber] " +
            "FROM [Search_CTE] " +
            ") " +
            "SELECT * " +
            "FROM [ProductPlacementStorage] " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ID = [ProductPlacementStorage].ProductPlacementId " +
            "LEFT JOIN [Product] " +
            "ON [ProductPlacementStorage].ProductID = [Product].ID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductPlacementStorage].StorageID " +
            "WHERE [ProductPlacementStorage].ID IN ( " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            ") " +
            "ORDER BY [ProductPlacementStorage].ID DESC ",
            (productPlacementStorage, placement, product, storage) => {
                productPlacementStorage.Product = product;

                productPlacementStorage.Storage = storage;

                productPlacementStorage.ProductPlacement = placement;

                return productPlacementStorage;
            });

        int TotalCount = _connection.ExecuteScalar<int>(
            ";WITH [Search_CTE] AS ( " +
            "SELECT [ProductPlacementStorage].ID " +
            "FROM [ProductPlacementStorage] " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ID = [ProductPlacementStorage].ProductPlacementId " +
            "LEFT JOIN [Product] " +
            "ON [ProductPlacement].ProductID = [Product].ID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductPlacement].StorageID " +
            ") " +
            "SELECT COUNT(*) AS TotalCount " +
            "FROM [Search_CTE] ");

        foreach (ProductPlacementStorage productPlacementStorage in productPlacementStorages) productPlacementStorage.TotalRowsQty = TotalCount;

        return productPlacementStorages;
    }

    public IEnumerable<ProductPlacementStorage> GetAllFiltered(long[] storageId, string value, DateTime to, long limit, long offset) {
        string sqlMapper =
            ";WITH [Search_CTE] " +
            "AS ( " +
            "SELECT [ProductPlacementStorage].ID " +
            "FROM [ProductPlacementStorage] " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ID = [ProductPlacementStorage].ProductPlacementId " +
            "LEFT JOIN [Product] " +
            "ON [ProductPlacement].ProductID = [Product].ID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductPlacement].StorageID " +
            "WHERE [Storage].ID IN @StorageId " +
            "AND [ProductPlacementStorage].Created <= @To " +
            "), " +
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT [Search_CTE].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [Search_CTE].ID DESC) AS [RowNumber] " +
            "FROM [Search_CTE] " +
            ") " +
            "SELECT * " +
            "FROM [ProductPlacementStorage] " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ID = [ProductPlacementStorage].ProductPlacementId " +
            "LEFT JOIN [Product] " +
            "ON [ProductPlacementStorage].ProductID = [Product].ID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductPlacementStorage].StorageID " +
            "WHERE [ProductPlacementStorage].ID IN ( " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            "WHERE [Rowed_CTE].RowNumber > @Offset " +
            "AND [Rowed_CTE].RowNumber <= @Limit + @Offset " +
            ") ";

        if (!string.IsNullOrEmpty(value))
            sqlMapper +=
                "AND " +
                "( " +
                "PATINDEX('%' + @Value + '%', [Product].VendorCode) > 0 " +
                "OR PATINDEX('%' + @Value + '%', [Product].MainOriginalNumber) > 0 " +
                "OR PATINDEX('%' + @Value + '%', [Product].NameUA) > 0 " +
                ") ";

        sqlMapper += "ORDER BY [ProductPlacementStorage].ID DESC ";

        IEnumerable<ProductPlacementStorage> productPlacementStorages =
            _connection.Query<ProductPlacementStorage, ProductPlacement, Product, Storage, ProductPlacementStorage>(
                sqlMapper,
                (productPlacementStorage, placement, product, storage) => {
                    productPlacementStorage.Product = product;

                    productPlacementStorage.Storage = storage;

                    productPlacementStorage.ProductPlacement = placement;

                    return productPlacementStorage;
                },
                new {
                    StorageId = storageId,
                    Limit = limit,
                    Offset = offset,
                    To = to,
                    Value = value
                }
            );

        string sqlMapperTotalCount =
            ";WITH [Search_CTE] AS ( " +
            "SELECT [ProductPlacementStorage].ID " +
            "FROM [ProductPlacementStorage] " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ID = [ProductPlacementStorage].ProductPlacementId " +
            "LEFT JOIN [Product] " +
            "ON [ProductPlacement].ProductID = [Product].ID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductPlacement].StorageID " +
            "WHERE [Storage].ID IN @StorageId " +
            "AND [ProductPlacementStorage].Created <= @To ";

        if (!string.IsNullOrEmpty(value))
            sqlMapperTotalCount +=
                "AND " +
                "( " +
                "PATINDEX('%' + @Value + '%', [ProductPlacementStorage].VendorCode) > 0 " +
                "OR PATINDEX('%' + @Value + '%', [Product].SearchVendorCode) > 0  " +
                "OR PATINDEX('%' + @Value + '%', [Product].NameUA) > 0  " +
                "OR PATINDEX('%' + @Value + '%', [ProductPlacementStorage].Placement) > 0 " +
                ") ";

        sqlMapperTotalCount +=
            ") " +
            "SELECT COUNT(*) AS TotalCount " +
            "FROM [Search_CTE] ";

        int totalCount = _connection.ExecuteScalar<int>(
            sqlMapperTotalCount,
            new {
                Value = value,
                StorageId = storageId,
                To = to
            });

        foreach (ProductPlacementStorage productPlacementStorage in productPlacementStorages) productPlacementStorage.TotalRowsQty = totalCount;

        return productPlacementStorages;
    }

    public ProductPlacementStorage GetById(long id) {
        throw new NotImplementedException();
    }
}