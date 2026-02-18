using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class DynamicProductPlacementRowRepository : IDynamicProductPlacementRowRepository {
    private readonly IDbConnection _connection;

    public DynamicProductPlacementRowRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(DynamicProductPlacementRow row) {
        return _connection.Query<long>(
                "INSERT INTO [DynamicProductPlacementRow] (SupplyOrderUkraineItemId, PackingListPackageOrderItemId, DynamicProductPlacementColumnId, Qty, Updated) " +
                "VALUES (@SupplyOrderUkraineItemId, @PackingListPackageOrderItemId, @DynamicProductPlacementColumnId, @Qty, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                row
            )
            .Single();
    }

    public void Add(IEnumerable<DynamicProductPlacementRow> rows) {
        _connection.Execute(
            "INSERT INTO [DynamicProductPlacementRow] (SupplyOrderUkraineItemId, PackingListPackageOrderItemId, DynamicProductPlacementColumnId, Qty, Updated) " +
            "VALUES (@SupplyOrderUkraineItemId, @PackingListPackageOrderItemId, @DynamicProductPlacementColumnId, @Qty, GETUTCDATE())",
            rows
        );
    }

    public void Update(DynamicProductPlacementRow row) {
        _connection.Execute(
            "UPDATE [DynamicProductPlacementRow] " +
            "SET SupplyOrderUkraineItemId = @SupplyOrderUkraineItemId, DynamicProductPlacementColumnId = @DynamicProductPlacementColumnId, Qty = @Qty, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            row
        );
    }

    public void Update(IEnumerable<DynamicProductPlacementRow> rows) {
        _connection.Execute(
            "UPDATE [DynamicProductPlacementRow] " +
            "SET SupplyOrderUkraineItemId = @SupplyOrderUkraineItemId, DynamicProductPlacementColumnId = @DynamicProductPlacementColumnId, Qty = @Qty, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            rows
        );
    }

    public void RemoveAllByColumnIdExceptProvided(long columnId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [DynamicProductPlacementRow] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE DynamicProductPlacementColumnId = @ColumnId " +
            "AND ID NOT IN @Ids " +
            "AND Qty = 0",
            new { ColumnId = columnId, Ids = ids }
        );
    }

    public void RemoveById(long id) {
        _connection.Execute(
            "UPDATE [DynamicProductPlacementRow] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public DynamicProductPlacementRow GetByIdWithoutIncludes(long id) {
        return _connection.Query<DynamicProductPlacementRow>(
                "SELECT * " +
                "FROM [DynamicProductPlacementRow] " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public DynamicProductPlacementRow GetById(long id) {
        Type[] types = {
            typeof(DynamicProductPlacementRow),
            typeof(SupplyOrderUkraineItem),
            typeof(Product),
            typeof(DynamicProductPlacementColumn),
            typeof(SupplyOrderUkraine),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(PackingList),
            typeof(SupplyInvoice)
        };

        Func<object[], DynamicProductPlacementRow> mapper = objects => {
            DynamicProductPlacementRow row = (DynamicProductPlacementRow)objects[0];
            SupplyOrderUkraineItem item = (SupplyOrderUkraineItem)objects[1];
            Product product = (Product)objects[2];
            DynamicProductPlacementColumn column = (DynamicProductPlacementColumn)objects[3];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[4];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[5];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[6];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[7];
            Product supplyOrderItemProduct = (Product)objects[8];
            PackingList packingList = (PackingList)objects[9];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[10];

            if (packingList != null) packingList.SupplyInvoice = supplyInvoice;

            column.SupplyOrderUkraine = supplyOrderUkraine;
            column.PackingList = packingList;

            if (item != null) item.Product = product;

            if (packingListPackageOrderItem != null) {
                if (supplyOrderItem != null)
                    supplyOrderItem.Product = supplyOrderItemProduct;

                supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                supplyInvoiceOrderItem.Product = supplyOrderItemProduct;

                packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;
            }

            row.SupplyOrderUkraineItem = item;
            row.PackingListPackageOrderItem = packingListPackageOrderItem;
            row.DynamicProductPlacementColumn = column;

            return row;
        };

        DynamicProductPlacementRow toReturn =
            _connection.Query(
                    "SELECT * " +
                    "FROM [DynamicProductPlacementRow] " +
                    "LEFT JOIN [SupplyOrderUkraineItem] " +
                    "ON [SupplyOrderUkraineItem].ID = [DynamicProductPlacementRow].SupplyOrderUkraineItemID " +
                    "LEFT JOIN [Product] " +
                    "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
                    "LEFT JOIN [DynamicProductPlacementColumn] " +
                    "ON [DynamicProductPlacementColumn].ID = [DynamicProductPlacementRow].DynamicProductPlacementColumnID " +
                    "LEFT JOIN [SupplyOrderUkraine] " +
                    "ON [SupplyOrderUkraine].ID = [DynamicProductPlacementColumn].SupplyOrderUkraineID " +
                    "LEFT JOIN [PackingListPackageOrderItem] " +
                    "ON [PackingListPackageOrderItem].ID = [DynamicProductPlacementRow].PackingListPackageOrderItemID " +
                    "LEFT JOIN [SupplyInvoiceOrderItem] " +
                    "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
                    "LEFT JOIN [SupplyOrderItem] " +
                    "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
                    "LEFT JOIN [Product] AS [PackListProduct] " +
                    "ON [PackListProduct].ID = [SupplyInvoiceOrderItem].ProductID " +
                    "LEFT JOIN [PackingList] " +
                    "ON [DynamicProductPlacementColumn].PackingListID = [PackingList].ID " +
                    "LEFT JOIN [SupplyInvoice] " +
                    "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
                    "WHERE [DynamicProductPlacementRow].ID = @Id",
                    types,
                    mapper,
                    new { Id = id }
                )
                .SingleOrDefault();

        if (toReturn != null) {
            toReturn.DynamicProductPlacements =
                _connection.Query<DynamicProductPlacement>(
                    "SELECT * " +
                    "FROM [DynamicProductPlacement] " +
                    "WHERE [DynamicProductPlacement].Deleted = 0 " +
                    "AND [DynamicProductPlacement].DynamicProductPlacementRowID = @Id",
                    new { toReturn.Id }
                ).ToList();

            _connection.Query<ProductPlacement, Storage, ProductPlacement>(
                ";WITH [Search_CTE] " +
                "AS ( " +
                "SELECT MAX([ID]) AS [ID] " +
                "FROM [ProductPlacement] " +
                "WHERE [ProductPlacement].ProductID = @ProductId " +
                "AND [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
                "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
                "GROUP BY [ProductPlacement].CellNumber " +
                ", [ProductPlacement].RowNumber " +
                ", [ProductPlacement].StorageNumber " +
                ", [ProductPlacement].StorageID " +
                ") " +
                "SELECT * " +
                "FROM [ProductPlacement] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductPlacement].StorageID " +
                "WHERE [ProductPlacement].ID IN ( " +
                "SELECT [ID] " +
                "FROM [Search_CTE] " +
                ")",
                (placement, storage) => {
                    placement.Storage = storage;

                    if (toReturn.SupplyOrderUkraineItem != null)
                        toReturn.SupplyOrderUkraineItem.Product.ProductPlacements.Add(placement);
                    else
                        toReturn.PackingListPackageOrderItem.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);

                    return placement;
                },
                new {
                    ProductId =
                        toReturn.SupplyOrderUkraineItem != null
                            ? toReturn.SupplyOrderUkraineItem.ProductId
                            : toReturn.PackingListPackageOrderItem.SupplyInvoiceOrderItem.ProductId
                }
            );
        }

        return toReturn;
    }

    public DynamicProductPlacementRow GetByNetId(Guid netId) {
        Type[] types = {
            typeof(DynamicProductPlacementRow),
            typeof(SupplyOrderUkraineItem),
            typeof(Product),
            typeof(DynamicProductPlacementColumn),
            typeof(SupplyOrderUkraine),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(PackingList),
            typeof(SupplyInvoice)
        };

        Func<object[], DynamicProductPlacementRow> mapper = objects => {
            DynamicProductPlacementRow row = (DynamicProductPlacementRow)objects[0];
            SupplyOrderUkraineItem item = (SupplyOrderUkraineItem)objects[1];
            Product product = (Product)objects[2];
            DynamicProductPlacementColumn column = (DynamicProductPlacementColumn)objects[3];
            SupplyOrderUkraine supplyOrderUkraine = (SupplyOrderUkraine)objects[4];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[5];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[6];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[7];
            Product supplyOrderItemProduct = (Product)objects[8];
            PackingList packingList = (PackingList)objects[9];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[10];

            if (packingList != null) packingList.SupplyInvoice = supplyInvoice;

            column.SupplyOrderUkraine = supplyOrderUkraine;
            column.PackingList = packingList;

            if (item != null) item.Product = product;

            if (packingListPackageOrderItem != null) {
                if (supplyOrderItem != null)
                    supplyOrderItem.Product = supplyOrderItemProduct;

                supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                supplyInvoiceOrderItem.Product = product;

                packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;
            }

            row.SupplyOrderUkraineItem = item;
            row.PackingListPackageOrderItem = packingListPackageOrderItem;
            row.DynamicProductPlacementColumn = column;

            return row;
        };

        DynamicProductPlacementRow toReturn =
            _connection.Query(
                    "SELECT * " +
                    "FROM [DynamicProductPlacementRow] " +
                    "LEFT JOIN [SupplyOrderUkraineItem] " +
                    "ON [SupplyOrderUkraineItem].ID = [DynamicProductPlacementRow].SupplyOrderUkraineItemID " +
                    "LEFT JOIN [Product] " +
                    "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
                    "LEFT JOIN [DynamicProductPlacementColumn] " +
                    "ON [DynamicProductPlacementColumn].ID = [DynamicProductPlacementRow].DynamicProductPlacementColumnID " +
                    "LEFT JOIN [SupplyOrderUkraine] " +
                    "ON [SupplyOrderUkraine].ID = [DynamicProductPlacementColumn].SupplyOrderUkraineID " +
                    "WHERE [DynamicProductPlacementRow].NetUID = @NetId",
                    types,
                    mapper,
                    new { NetId = netId }
                )
                .SingleOrDefault();

        if (toReturn != null) {
            toReturn.DynamicProductPlacements =
                _connection.Query<DynamicProductPlacement>(
                    "SELECT * " +
                    "FROM [DynamicProductPlacement] " +
                    "WHERE [DynamicProductPlacement].Deleted = 0 " +
                    "AND [DynamicProductPlacement].DynamicProductPlacementRowID = @Id",
                    new { toReturn.Id }
                ).ToList();

            _connection.Query<ProductPlacement, Storage, ProductPlacement>(
                ";WITH [Search_CTE] " +
                "AS ( " +
                "SELECT MAX([ID]) AS [ID] " +
                "FROM [ProductPlacement] " +
                "WHERE [ProductPlacement].ProductID = @ProductId " +
                "AND [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
                "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
                "GROUP BY [ProductPlacement].CellNumber " +
                ", [ProductPlacement].RowNumber " +
                ", [ProductPlacement].StorageNumber " +
                ", [ProductPlacement].StorageID " +
                ") " +
                "SELECT * " +
                "FROM [ProductPlacement] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductPlacement].StorageID " +
                "WHERE [ProductPlacement].ID IN ( " +
                "SELECT [ID] " +
                "FROM [Search_CTE] " +
                ")",
                (placement, storage) => {
                    placement.Storage = storage;

                    if (toReturn.SupplyOrderUkraineItem != null)
                        toReturn.SupplyOrderUkraineItem.Product.ProductPlacements.Add(placement);
                    else
                        toReturn.PackingListPackageOrderItem.SupplyInvoiceOrderItem.Product.ProductPlacements.Add(placement);

                    return placement;
                },
                new {
                    ProductId =
                        toReturn.SupplyOrderUkraineItem != null
                            ? toReturn.SupplyOrderUkraineItem.ProductId
                            : toReturn.PackingListPackageOrderItem.SupplyInvoiceOrderItem.ProductId
                }
            );
        }

        return toReturn;
    }

    public List<DynamicProductPlacementRow> GetAllByColumnIdExceptProvidedIds(long columnId, IEnumerable<long> ids) {
        List<DynamicProductPlacementRow> rows = new();

        _connection.Query<DynamicProductPlacementRow, DynamicProductPlacement, DynamicProductPlacementRow>(
            "SELECT * " +
            "FROM [DynamicProductPlacementRow] " +
            "LEFT JOIN [DynamicProductPlacement] " +
            "ON [DynamicProductPlacement].DynamicProductPlacementRowID = [DynamicProductPlacementRow].ID " +
            "AND [DynamicProductPlacement].Deleted = 0 " +
            "WHERE [DynamicProductPlacementRow].Deleted = 0 " +
            "AND [DynamicProductPlacementRow].DynamicProductPlacementColumnID = @ColumnId " +
            "AND [DynamicProductPlacementRow].ID NOT IN @Ids",
            (row, placement) => {
                if (!rows.Any(r => r.Id.Equals(row.Id))) {
                    if (placement != null) row.DynamicProductPlacements.Add(placement);

                    rows.Add(row);
                } else if (placement != null) {
                    rows.First(r => r.Id.Equals(row.Id)).DynamicProductPlacements.Add(placement);
                }

                return row;
            },
            new { ColumnId = columnId, Ids = ids }
        );

        return rows;
    }
}