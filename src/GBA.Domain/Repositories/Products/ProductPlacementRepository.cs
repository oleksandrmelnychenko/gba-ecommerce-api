using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductPlacementRepository : IProductPlacementRepository {
    private readonly IDbConnection _connection;

    public ProductPlacementRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ProductPlacement productPlacement) {
        _connection.Execute(
            "INSERT INTO [ProductPlacement] " +
            "(Qty, StorageNumber, RowNumber, CellNumber, ProductId, StorageId, PackingListPackageOrderItemId, SupplyOrderUkraineItemId, " +
            "ProductIncomeItemId, ConsignmentItemId, IsOriginal, Updated) " +
            "VALUES " +
            "(@Qty, @StorageNumber, @RowNumber, @CellNumber, @ProductId, @StorageId, @PackingListPackageOrderItemId, @SupplyOrderUkraineItemId, " +
            "@ProductIncomeItemId, @ConsignmentItemId, @IsOriginal, GETUTCDATE())",
            productPlacement
        );
    }

    public long AddWithId(ProductPlacement productPlacement) {
        return _connection.Query<long>(
            "INSERT INTO [ProductPlacement] " +
            "(Qty, StorageNumber, RowNumber, CellNumber, ProductId, StorageId, PackingListPackageOrderItemId, SupplyOrderUkraineItemId, " +
            "ProductIncomeItemId, ConsignmentItemId, IsOriginal, Updated) " +
            "VALUES " +
            "(@Qty, @StorageNumber, @RowNumber, @CellNumber, @ProductId, @StorageId, @PackingListPackageOrderItemId, @SupplyOrderUkraineItemId, " +
            "@ProductIncomeItemId, @ConsignmentItemId, @IsOriginal, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            productPlacement
        ).Single();
    }

    public void Add(IEnumerable<ProductPlacement> productPlacements) {
        _connection.Execute(
            "INSERT INTO [ProductPlacement] " +
            "(Qty, StorageNumber, RowNumber, CellNumber, ProductId, StorageId, PackingListPackageOrderItemId, SupplyOrderUkraineItemId, " +
            "ProductIncomeItemId, ConsignmentItemId, Updated) " +
            "VALUES " +
            "(@Qty, @StorageNumber, @RowNumber, @CellNumber, @ProductId, @StorageId, @PackingListPackageOrderItemId, @SupplyOrderUkraineItemId, " +
            "@ProductIncomeItemId, @ConsignmentItemId, GETUTCDATE())",
            productPlacements
        );
    }

    public void UpdateQty(ProductPlacement productPlacement) {
        _connection.Execute(
            "UPDATE [ProductPlacement] " +
            "SET Qty = @Qty, Updated = GETUTCDATE() " +
            "WHERE [ProductPlacement].ID = @Id",
            productPlacement
        );
    }

    public void UpdateReferences(ProductPlacement productPlacement) {
        _connection.Execute(
            "UPDATE [ProductPlacement] " +
            "SET ProductIncomeItemID = @ProductIncomeItemId, ConsignmentItemID = @ConsignmentItemId, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            productPlacement
        );
    }

    public void Update(ProductPlacement productPlacement) {
        _connection.Execute(
            "UPDATE [ProductPlacement] " +
            "SET CellNumber = @CellNumber, StorageNumber = @StorageNumber, RowNumber = @RowNumber, ProductId = @ProductId, StorageId = @StorageId, Qty = @Qty, ConsignmentItemId = @ConsignmentItemId, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            productPlacement
        );
    }

    public void Update(List<ProductPlacement> productPlacements) {
        _connection.Execute(
            "UPDATE [ProductPlacement] " +
            "SET CellNumber = @CellNumber, StorageNumber = @StorageNumber, RowNumber = @RowNumber, ProductId = @ProductId, StorageId = @StorageId, Qty = @Qty, ConsignmentItemId = @ConsignmentItemId, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            productPlacements
        );
    }

    public void Restore(ProductPlacement productPlacement) {
        _connection.Query<ProductPlacement>(
            "UPDATE [ProductPlacement] " +
            "SET Qty = @Qty, Deleted = 0, Updated = GETUTCDATE() " +
            "WHERE [ProductPlacement].ID = @Id",
            productPlacement
        );
    }

    public void Remove(ProductPlacement productPlacement) {
        _connection.Query<ProductPlacement>(
            "UPDATE [ProductPlacement] " +
            "SET Qty = 0, IsHistorySet = 1, Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductPlacement].ID = @Id",
            productPlacement
        );
    }

    public void RemoveWithoutQty(ProductPlacement productPlacement) {
        _connection.Query<ProductPlacement>(
            "UPDATE [ProductPlacement] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductPlacement].ID = @Id",
            productPlacement
        );
    }

    public void ReAssignProductPlacementFromProductIncomeItemToConsignmentItemByIds(long productIncomeItemId, long consignmentItemId) {
        _connection.Execute(
            "UPDATE [ProductPlacement] " +
            "SET ProductIncomeItemID = NULL, ConsignmentItemID = @ConsignmentItemId " +
            "WHERE [ProductPlacement].Deleted = 0 " +
            "AND [ProductPlacement].ProductIncomeItemID = @ProductIncomeItemId",
            new { ProductIncomeItemId = productIncomeItemId, ConsignmentItemId = consignmentItemId }
        );
    }

    public ProductPlacement GetIfExists(
        string rowNumber,
        string cellNumber,
        string storageNumber,
        long productId,
        long storageId,
        long? productIncomeId = null,
        long? consignmentItemId = null) {
        return _connection.Query<ProductPlacement>(
            "SELECT TOP(1) * " +
            "FROM [ProductPlacement] " +
            "WHERE [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
            "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
            "AND [ProductPlacement].ProductIncomeItemID = @ProductIncomeId " +
            "AND [ProductPlacement].ConsignmentItemID = @ConsignmentItemId " +
            "AND [ProductPlacement].RowNumber = @RowNumber " +
            "AND [ProductPlacement].CellNumber = @CellNumber " +
            "AND [ProductPlacement].StorageNumber = @StorageNumber " +
            "AND [ProductPlacement].ProductID = @ProductId " +
            "AND [ProductPlacement].StorageID = @StorageId",
            new {
                RowNumber = rowNumber,
                CellNumber = cellNumber,
                StorageNumber = storageNumber,
                ProductId = productId,
                StorageId = storageId,
                ProductIncomeId = productIncomeId,
                ConsignmentItemId = consignmentItemId
            }
        ).SingleOrDefault();
    }

    public ProductPlacement GetById(long id) {
        return _connection.Query<ProductPlacement>(
            "SELECT * " +
            "FROM [ProductPlacement] " +
            "WHERE [ProductPlacement].ID = @Id " +
            "AND [ProductPlacement].Deleted = 0",
            new { Id = id }
        ).SingleOrDefault();
    }

    public ProductPlacement GetByIdDeleted(long id) {
        return _connection.Query<ProductPlacement>(
            "SELECT * " +
            "FROM [ProductPlacement] " +
            "WHERE [ProductPlacement].ID = @Id ",
            new { Id = id }
        ).SingleOrDefault();
    }

    public ProductPlacement GetByIdWithStorage(long id) {
        return _connection.Query<ProductPlacement, Storage, Organization, ProductPlacement>(
            "SELECT * " +
            "FROM [ProductPlacement] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductPlacement].StorageID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Storage].OrganizationID " +
            "WHERE [ProductPlacement].ID = @Id " +
            "AND [ProductPlacement].Deleted = 0",
            (placement, storage, organization) => {
                storage.Organization = organization;

                placement.Storage = storage;

                return placement;
            },
            new { Id = id }
        ).SingleOrDefault();
    }

    public ProductPlacement GetLastByProductAndStorageId(long productId, long storageId) {
        return _connection.Query<ProductPlacement>(
            "SELECT TOP(1) [ProductPlacement].* " +
            "FROM [ProductPlacement] " +
            "WHERE [ProductPlacement].Deleted = 0 " +
            "AND [ProductPlacement].ProductID = @ProductId " +
            "AND [ProductPlacement].StorageID = @StorageId " +
            "AND [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
            "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
            "AND [ProductPlacement].ProductIncomeItemID IS NULL " +
            "AND [ProductPlacement].ConsignmentItemID IS NULL " +
            "AND [ProductPlacement].StorageNumber <> 'N' " +
            "AND [ProductPlacement].RowNumber <> 'N' " +
            "AND [ProductPlacement].CellNumber <> 'N' " +
            "ORDER BY [ProductPlacement].ID DESC",
            new { ProductId = productId, StorageId = storageId }
        ).SingleOrDefault();
    }

    public ProductPlacement GetLastByProductId(long productId) {
        return _connection.Query<ProductPlacement>(
            "SELECT TOP(1) [ProductPlacement].* " +
            "FROM [ProductPlacement] " +
            "WHERE [ProductPlacement].Deleted = 0 " +
            "AND [ProductPlacement].ProductID = @ProductId " +
            "AND [ProductPlacement].StorageNumber <> 'N' " +
            "AND [ProductPlacement].RowNumber <> 'N' " +
            "AND [ProductPlacement].CellNumber <> 'N' " +
            "ORDER BY [ProductPlacement].ID DESC",
            new { ProductId = productId }
        ).SingleOrDefault();
    }

    public ProductPlacement GetNonByProductAndStorageId(long productId, long storageId) {
        return _connection.Query<ProductPlacement>(
            "SELECT TOP(1) [ProductPlacement].* " +
            "FROM [ProductPlacement] " +
            "WHERE [ProductPlacement].Deleted = 0 " +
            "AND [ProductPlacement].ProductID = @ProductId " +
            "AND [ProductPlacement].StorageID = @StorageId " +
            "AND [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
            "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
            "AND [ProductPlacement].ProductIncomeItemID IS NULL " +
            "AND [ProductPlacement].ConsignmentItemID IS NULL " +
            "ORDER BY [ProductPlacement].ID DESC",
            new { ProductId = productId, StorageId = storageId }
        ).SingleOrDefault();
    }

    public IEnumerable<ProductPlacement> GetAllByProductAndStorageId(long productId, long storageId) {
        return _connection.Query<ProductPlacement>(
            "SELECT [ProductPlacement].* " +
            "FROM [ProductPlacement] " +
            "WHERE [ProductPlacement].Deleted = 0 " +
            "AND [ProductPlacement].ProductID = @ProductId " +
            "AND [ProductPlacement].StorageID = @StorageId " +
            "AND [ProductPlacement].ProductIncomeItemID IS NULL " +
            "AND [ProductPlacement].ConsignmentItemID IS NULL " +
            "AND [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
            "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL",
            new { ProductId = productId, StorageId = storageId }
        );
    }

    public IEnumerable<ProductPlacement> GetAllByProductAndStorageIds(long productId, long storageId) {
        return _connection.Query<ProductPlacement>(
            "SELECT [ProductPlacement].* " +
            "FROM [ProductPlacement] " +
            "WHERE [ProductPlacement].Deleted = 0 " +
            "AND [ProductPlacement].ProductID = @ProductId " +
            "AND [ProductPlacement].StorageID = @StorageId ",
            new { ProductId = productId, StorageId = storageId }
        );
    }

    public IEnumerable<ProductPlacement> GetAllByProductIncomeItemId(long productIncomeItemId) {
        return _connection.Query<ProductPlacement>(
            "SELECT * " +
            "FROM [ProductPlacement] " +
            "WHERE [ProductPlacement].Deleted = 0 " +
            "AND [ProductPlacement].ProductIncomeItemID = @ProductIncomeItemId",
            new { ProductIncomeItemId = productIncomeItemId }
        );
    }

    public IEnumerable<ProductPlacement> GetAllByConsignmentItemId(long consignmentItemId) {
        return _connection.Query<ProductPlacement>(
            "SELECT * " +
            "FROM [ProductPlacement] " +
            "WHERE [ProductPlacement].Deleted = 0 " +
            "AND [ProductPlacement].ConsignmentItemID = @ConsignmentItemId",
            new { ConsignmentItemId = consignmentItemId }
        );
    }

    public IEnumerable<ProductPlacement> GetAllFilteredAndOrderedByWriteOffRule(
        long storageId,
        long productId,
        string rowNumber,
        string cellNumber,
        string storageNumber,
        ProductWriteOffRuleType ruleType) {
        string sqlExpression =
            "SELECT [ProductPlacement].* " +
            "FROM [ProductPlacement] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ProductPlacement].ConsignmentItemID " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].ID = [ConsignmentItem].ConsignmentID " +
            "WHERE [ProductPlacement].Deleted = 0 " +
            "AND [ProductPlacement].Qty <> 0 " +
            "AND [ProductPlacement].StorageID = @StorageId " +
            "AND [ProductPlacement].ProductID = @ProductId " +
            "AND [ProductPlacement].RowNumber = @RowNumber " +
            "AND [ProductPlacement].CellNumber = @CellNumber " +
            "AND [ProductPlacement].StorageNumber = @StorageNumber " +
            "ORDER BY (CASE WHEN [ProductPlacement].ConsignmentItemID IS NOT NULL THEN 0 ELSE 1 END) ";

        switch (ruleType) {
            case ProductWriteOffRuleType.ByWeight:
                sqlExpression += ", [ConsignmentItem].Weight DESC";

                break;
            case ProductWriteOffRuleType.ByPrice:
                sqlExpression += ", [ConsignmentItem].Price DESC";

                break;
            case ProductWriteOffRuleType.ByFromDate:
                sqlExpression += ", [Consignment].FromDate DESC";

                break;
            case ProductWriteOffRuleType.ByDutyRate:
                sqlExpression += ", [ConsignmentItem].DutyPercent DESC";

                break;
        }

        return _connection.Query<ProductPlacement>(
            sqlExpression,
            new {
                StorageId = storageId,
                ProductId = productId,
                RowNumber = rowNumber,
                CellNumber = cellNumber,
                StorageNumber = storageNumber
            }
        );
    }

    public ProductPlacement Get(string rowNumber, string cellNumber, string storageNumber, long productId, long storageId) {
        return _connection.Query<ProductPlacement>(
            "SELECT TOP(1) * " +
            "FROM [ProductPlacement] " +
            "WHERE [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
            "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
            "AND [ProductPlacement].Deleted = 0 " +
            "AND [ProductPlacement].RowNumber = @RowNumber " +
            "AND [ProductPlacement].CellNumber = @CellNumber " +
            "AND [ProductPlacement].StorageNumber = @StorageNumber " +
            "AND [ProductPlacement].ProductID = @ProductId " +
            "AND [ProductPlacement].StorageID = @StorageId",
            new {
                RowNumber = rowNumber,
                CellNumber = cellNumber,
                StorageNumber = storageNumber,
                ProductId = productId,
                StorageId = storageId
            }
        ).SingleOrDefault();
    }

    public ProductPlacement GetQty(int qty, long productId, long storageId) {
        return _connection.Query<ProductPlacement>(
            "SELECT TOP(1) * " +
            "FROM [ProductPlacement] " +
            "WHERE [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
            "AND [ProductPlacement].Deleted = 0 " +
            "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
            "AND [ProductPlacement].ProductID = @ProductId " +
            "AND [ProductPlacement].StorageID = @StorageId " +
            "AND [ProductPlacement].Qty = @Qty",
            new {
                ProductId = productId,
                StorageId = storageId,
                Qty = qty
            }
        ).SingleOrDefault();
    }

    public void RemoveFromProductIdToStorageId(long productId, long storageId) {
        _connection.Execute(
            "UPDATE [ProductPlacement] " +
            "SET Qty = 0, Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductPlacement].ProductID = @ProductId " +
            "AND [ProductPlacement].StorageID = @StorageId",
            new { ProductId = productId, StorageId = storageId }
        );
    }

    public IEnumerable<ProductPlacement> GetIsHistorySet(long productId, long storageId) {
        return _connection.Query<ProductPlacement>(
            "SELECT [ProductPlacement].* " +
            "FROM [ProductPlacement] " +
            "WHERE [ProductPlacement].Deleted = 1 " +
            "AND [ProductPlacement].ProductID = @ProductId " +
            "AND [ProductPlacement].StorageID = @StorageId " +
            "AND [ProductPlacement].IsHistorySet = 1",
            new { ProductId = productId, StorageId = storageId }
        );
    }

    public void RemoveIsHistorySet(ProductPlacement productPlacement) {
        _connection.Query<ProductPlacement>(
            "UPDATE [ProductPlacement] " +
            "SET IsHistorySet = 0, Updated = GETUTCDATE() " +
            "WHERE [ProductPlacement].ID = @Id",
            productPlacement
        );
    }

    public IEnumerable<ProductPlacement> GetAllByConsignmentItemProductAndStorageIds(long consignmentItemId, long productId, long storageId) {
        return _connection.Query<ProductPlacement>(
            "SELECT * " +
            "FROM [ProductPlacement] " +
            "WHERE [ProductPlacement].Deleted = 0 " +
            "AND [ProductPlacement].ProductID = @ProductId " +
            "AND [ProductPlacement].StorageID = @StorageId ",
            new { ConsignmentItemId = consignmentItemId, ProductId = productId, StorageId = storageId }
        );
    }

    public ProductPlacement GetLastProductId(long productId) {
        return _connection.Query<ProductPlacement>(
            "SELECT TOP(1) [ProductPlacement].* " +
            "FROM [ProductPlacement] " +
            "WHERE [ProductPlacement].ProductID = @ProductId " +
            "ORDER BY [ProductPlacement].ID DESC",
            new { ProductId = productId }
        ).SingleOrDefault();
    }
}