using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductAvailabilityRepository : IProductAvailabilityRepository {
    private readonly IDbConnection _connection;

    public ProductAvailabilityRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ProductAvailability productAvailability) {
        _connection.Execute(
            "INSERT INTO [ProductAvailability] (ProductId, StorageId, Amount, Updated) " +
            "VALUES (@ProductId, @StorageId, @Amount, GETUTCDATE())",
            productAvailability
        );
    }

    public long AddWithId(ProductAvailability productAvailability) {
        return _connection.Query<long>(
            "INSERT INTO [ProductAvailability] (ProductId, StorageId, Amount, Updated) " +
            "VALUES (@ProductId, @StorageId, @Amount, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            productAvailability
        ).Single();
    }

    public void Update(ProductAvailability productAvailability) {
        _connection.Execute(
            "UPDATE ProductAvailability " +
            "SET Amount = @Amount, StorageID = @StorageID, ProductID = @ProductID, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            productAvailability
        );
    }

    public void Update(List<ProductAvailability> productAvailabilities) {
        _connection.Execute(
            "UPDATE ProductAvailability " +
            "SET Amount = @Amount, StorageID = @StorageID, ProductID = @ProductID, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            productAvailabilities
        );
    }

    public ProductAvailability GetByProductAndStorageIds(long productId, long storageId) {
        return _connection.Query<ProductAvailability>(
            "SELECT TOP(1) * " +
            "FROM [ProductAvailability] " +
            "WHERE [ProductAvailability].Deleted = 0 " +
            "AND [ProductAvailability].StorageID = @StorageId " +
            "AND [ProductAvailability].ProductID = @ProductId",
            new { ProductId = productId, StorageId = storageId }
        ).FirstOrDefault();
    }

    public IEnumerable<ProductAvailability> GetAllByProductAndStorageIds(long productId, List<long> storageIds) {
        return _connection.Query<ProductAvailability, Storage, ProductAvailability>(
            "SELECT * " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].Deleted = 0 " +
            "AND [ProductAvailability].StorageID IN @StorageIds " +
            "AND [ProductAvailability].ProductID = @ProductId " +
            "ORDER BY [Storage].ForVatProducts ASC  ",
            (productAvailability, storage) => {
                productAvailability.Storage = storage;

                return productAvailability;
            },
            new { ProductId = productId, StorageIds = storageIds }
        );
    }

    public IEnumerable<ProductAvailability> GetByProductAndOrganizationIds(long productId, long organizationId, bool vatStorage, bool withReSale = false, long? storageId = null) {
        return _connection.Query<ProductAvailability, Storage, ProductAvailability>(
            "SELECT [ProductAvailability].* " +
            ", 0 AS [IsReSaleAvailability] " +
            ", [Storage].* " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = @ProductId " +
            "AND [Storage].OrganizationID = @OrganizationId " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].ForVatProducts = @VatStorage " +
            "AND [ProductAvailability].Deleted = 0 " +
            (
                storageId.HasValue
                    ? "UNION " +
                      "SELECT [ProductAvailability].* " +
                      ", 0 AS [IsReSaleAvailability] " +
                      ", [Storage].* " +
                      "FROM [ProductAvailability] " +
                      "LEFT JOIN [Storage] " +
                      "ON [Storage].ID = [ProductAvailability].StorageID " +
                      "WHERE [ProductAvailability].ProductID = @ProductId " +
                      "AND [Storage].ForDefective = 0 " +
                      "AND [Storage].ID = @StorageId " +
                      "AND [ProductAvailability].Deleted = 0 "
                    : string.Empty
            ) +
            (
                withReSale && !vatStorage
                    ? "UNION " +
                      "SELECT [ProductAvailability].* " +
                      ", 1 AS [IsReSaleAvailability] " +
                      ", [Storage].* " +
                      "FROM [ProductAvailability] " +
                      "LEFT JOIN [Storage] " +
                      "ON [Storage].ID = [ProductAvailability].StorageID " +
                      "WHERE [ProductAvailability].ProductID = @ProductId " +
                      "AND [Storage].ForDefective = 0 " +
                      "AND [Storage].AvailableForReSale = 1 " +
                      "AND [ProductAvailability].Deleted = 0 "
                    : string.Empty
            ) +
            "ORDER BY [IsReSaleAvailability]",
            (availability, storage) => {
                availability.Storage = storage;

                return availability;
            },
            new { ProductId = productId, OrganizationId = organizationId, VatStorage = vatStorage, StorageId = storageId }
        );
    }

    public IEnumerable<ProductAvailability> GetByProductAndCultureIds(long productId, string culture) {
        return _connection.Query<ProductAvailability, Storage, ProductAvailability>(
            "SELECT * " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = @ProductId " +
            "AND [Storage].Locale = @Culture " +
            "AND [Storage].ForDefective = 0 " +
            "AND [ProductAvailability].[Amount] > 0 " +
            "AND [ProductAvailability].Deleted = 0",
            (availability, storage) => {
                availability.Storage = storage;

                return availability;
            },
            new { ProductId = productId, Culture = culture }
        );
    }

    public ProductAvailability GetByProductIdForCulture(long id, string culture) {
        return _connection.Query<ProductAvailability>(
            "SELECT [ProductAvailability].* " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "AND [Storage].Deleted = 0 " +
            "WHERE [Storage].Locale = @Culture " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [ProductAvailability].ProductID = @Id",
            new { Id = id, Culture = culture }
        ).FirstOrDefault();
    }

    public List<ProductAvailability> GetAllOnDefectiveStoragesByProductId(long id) {
        return _connection.Query<ProductAvailability, Storage, ProductAvailability>(
            "SELECT * " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "AND [Storage].Deleted = 0 " +
            "WHERE [Storage].Deleted = 0 " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 1 " +
            "AND [ProductAvailability].ProductID = @Id",
            (availability, storage) => {
                availability.Storage = storage;

                return availability;
            },
            new { Id = id }
        ).ToList();
    }

    public List<ProductAvailability> GetAllByStorageNetIdFiltered(Guid netId, long limit, long offset, string value) {
        List<ProductAvailability> availabilities = new();

        _connection.Query<ProductAvailability, Storage, Product, MeasureUnit, ProductPlacement, ProductAvailability>(
            ";WITH [Search_CTE] " +
            "AS ( " +
            "SELECT [ProductAvailability].ID " +
            ", [Product].SearchVendorCode AS [VendorCode] " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductAvailability].ProductID " +
            "WHERE [ProductAvailability].Deleted = 0 " +
            "AND [ProductAvailability].Amount > 0 " +
            "AND [Storage].NetUID = @NetId " +
            "AND ( " +
            "PATINDEX('%' + @Value + '%', [Product].SearchVendorCode) > 0 " +
            "OR " +
            "PATINDEX('%' + @Value + '%', [Product].SearchNameUA) > 0 " +
            ") " +
            "), " +
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT [Search_CTE].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [Search_CTE].[VendorCode]) AS [RowNumber] " +
            "FROM [Search_CTE] " +
            ") " +
            "SELECT * " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductAvailability].ProductID " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ProductID = [Product].ID " +
            "AND [ProductPlacement].StorageID = [Storage].ID " +
            "AND [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
            "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
            "AND [ProductPlacement].ProductIncomeItemID IS NULL " +
            "AND [ProductPlacement].Deleted = 0 " +
            "WHERE [ProductAvailability].ID IN ( " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            "WHERE [Rowed_CTE].RowNumber > @Offset " +
            "AND [Rowed_CTE].RowNumber <= @Limit + @Offset " +
            ") " +
            "ORDER BY [Product].VendorCode",
            (availability, storage, product, measureUnit, placement) => {
                if (availabilities.Any(a => a.Id.Equals(availability.Id))) {
                    availability = availabilities.First(a => a.Id.Equals(availability.Id));
                } else {
                    product.MeasureUnit = measureUnit;

                    availability.Storage = storage;
                    availability.Product = product;

                    availabilities.Add(availability);
                }

                if (placement == null) return availability;

                if (!availability
                        .Product
                        .ProductPlacements
                        .Any(p => p.RowNumber.Equals(placement.RowNumber) &&
                                  p.CellNumber.Equals(placement.CellNumber) &&
                                  p.StorageNumber.Equals(placement.StorageNumber)))
                    availability.Product.ProductPlacements.Add(placement);
                else
                    availability
                        .Product
                        .ProductPlacements
                        .First(p => p.RowNumber.Equals(placement.RowNumber) &&
                                    p.CellNumber.Equals(placement.CellNumber) &&
                                    p.StorageNumber.Equals(placement.StorageNumber)).Qty += placement.Qty;

                return availability;
            },
            new { NetId = netId, Limit = limit, Offset = offset, Value = value, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return availabilities;
    }

    public List<ProductAvailability> GetAllProductsByStorageNetId(Guid netId) {
        List<ProductAvailability> availabilities = new();

        _connection.Query<ProductAvailability, Storage, Product, MeasureUnit, ProductPlacement, ProductAvailability>(
            ";WITH [Search_CTE] " +
            "AS ( " +
            "SELECT [ProductAvailability].ID " +
            ", [Product].SearchVendorCode AS [VendorCode] " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductAvailability].ProductID " +
            "WHERE [ProductAvailability].Deleted = 0 " +
            "AND [ProductAvailability].Amount > 0 " +
            "AND [Storage].NetUID = @NetId " +
            "), " +
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT [Search_CTE].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [Search_CTE].[VendorCode]) AS [RowNumber] " +
            "FROM [Search_CTE] " +
            ") " +
            "SELECT * " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductAvailability].ProductID " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ProductID = [Product].ID " +
            "AND [ProductPlacement].StorageID = [Storage].ID " +
            "AND [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
            "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
            "AND [ProductPlacement].Deleted = 0 " +
            "WHERE [ProductAvailability].ID IN ( " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            ") " +
            "ORDER BY [Product].VendorCode",
            (availability, storage, product, measureUnit, placement) => {
                if (!availabilities.Any(a => a.Id.Equals(availability.Id))) {
                    if (placement != null) product.ProductPlacements.Add(placement);

                    product.MeasureUnit = measureUnit;

                    availability.Storage = storage;
                    availability.Product = product;

                    availabilities.Add(availability);
                } else if (placement != null) {
                    ProductAvailability availabilityFromList = availabilities.First(a => a.Id.Equals(availability.Id));

                    availabilityFromList.Product.ProductPlacements.Add(placement);
                }

                return availability;
            },
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return availabilities;
    }

    public void RemoveById(long id) {
        _connection.Execute(
            "UPDATE [ProductAvailability] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductAvailability].ID = @Id",
            new { Id = id }
        );
    }
}