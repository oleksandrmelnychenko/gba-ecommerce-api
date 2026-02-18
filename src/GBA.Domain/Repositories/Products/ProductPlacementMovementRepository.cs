using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductPlacementMovementRepository : IProductPlacementMovementRepository {
    private readonly IDbConnection _connection;

    public ProductPlacementMovementRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ProductPlacementMovement productPlacementMovement) {
        return _connection.Query<long>(
            "INSERT INTO [ProductPlacementMovement] (Qty, Number, Comment, FromProductPlacementId, ToProductPlacementId, ResponsibleId, Updated) " +
            "VALUES (@Qty, @Number, @Comment, @FromProductPlacementId, @ToProductPlacementId, @ResponsibleId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            productPlacementMovement
        ).Single();
    }

    public ProductPlacementMovement GetLastRecord(string locale) {
        string sqlExpression =
            "SELECT TOP(1) * " +
            "FROM [ProductPlacementMovement] ";

        sqlExpression +=
            locale.ToLower().Equals("pl")
                ? "WHERE [ProductPlacementMovement].Number like N'P%' "
                : "WHERE [ProductPlacementMovement].Number NOT like N'P%' ";

        sqlExpression += "ORDER BY [ProductPlacementMovement].ID DESC";

        return _connection.Query<ProductPlacementMovement>(
            sqlExpression
        ).FirstOrDefault();
    }

    public ProductPlacementMovement GetById(long id) {
        return _connection.Query<ProductPlacementMovement, ProductPlacement, ProductPlacement, User, Product, Storage, ProductPlacementMovement>(
            "SELECT * " +
            "FROM [ProductPlacementMovement] " +
            "LEFT JOIN [ProductPlacement] AS [FromProductPlacement] " +
            "ON [FromProductPlacement].ID = [ProductPlacementMovement].FromProductPlacementID " +
            "LEFT JOIN [ProductPlacement] AS [ToProductPlacement] " +
            "ON [ToProductPlacement].ID = [ProductPlacementMovement].ToProductPlacementID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [ProductPlacementMovement].ResponsibleID " +
            "LEFT JOIN [Product] " +
            "ON [FromProductPlacement].ProductID = [Product].ID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [FromProductPlacement].StorageID " +
            "WHERE [ProductPlacementMovement].ID = @Id",
            (movement, fromPlacement, toPlacement, responsible, product, storage) => {
                fromPlacement.Product = product;
                toPlacement.Product = product;

                fromPlacement.Storage = storage;
                toPlacement.Storage = storage;

                movement.FromProductPlacement = fromPlacement;
                movement.ToProductPlacement = toPlacement;
                movement.Responsible = responsible;

                return movement;
            },
            new { Id = id }
        ).SingleOrDefault();
    }

    public IEnumerable<ProductPlacementMovement> GetAllFiltered(
        Guid storageNetId,
        string value,
        DateTime from,
        DateTime to,
        long limit,
        long offset) {
        return _connection.Query<ProductPlacementMovement, ProductPlacement, ProductPlacement, User, Product, Storage, ProductPlacementMovement>(
            ";WITH [Search_CTE] " +
            "AS ( " +
            "SELECT [ProductPlacementMovement].ID " +
            "FROM [ProductPlacementMovement] " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ID = [ProductPlacementMovement].FromProductPlacementID " +
            "LEFT JOIN [Product] " +
            "ON [ProductPlacement].ProductID = [Product].ID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductPlacement].StorageID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [ProductPlacementMovement].ResponsibleID " +
            "WHERE [Storage].NetUID = @StorageNetId " +
            "AND ( " +
            "PATINDEX('%' + @Value + '%', [Product].SearchVendorCode) > 0 " +
            "OR " +
            "PATINDEX('%' + @Value + '%', [Responsible].LastName) > 0 " +
            "OR " +
            "PATINDEX('%' + @Value + '%', [ProductPlacementMovement].Number) > 0 " +
            ") " +
            "AND [ProductPlacementMovement].Created >= @From " +
            "AND [ProductPlacementMovement].Created <= @To " +
            "), " +
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT [Search_CTE].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [Search_CTE].ID DESC) AS [RowNumber] " +
            "FROM [Search_CTE] " +
            ") " +
            "SELECT * " +
            "FROM [ProductPlacementMovement] " +
            "LEFT JOIN [ProductPlacement] AS [FromProductPlacement] " +
            "ON [FromProductPlacement].ID = [ProductPlacementMovement].FromProductPlacementID " +
            "LEFT JOIN [ProductPlacement] AS [ToProductPlacement] " +
            "ON [ToProductPlacement].ID = [ProductPlacementMovement].ToProductPlacementID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [ProductPlacementMovement].ResponsibleID " +
            "LEFT JOIN [Product] " +
            "ON [FromProductPlacement].ProductID = [Product].ID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [FromProductPlacement].StorageID " +
            "WHERE [ProductPlacementMovement].ID IN ( " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            "WHERE [Rowed_CTE].RowNumber > @Offset " +
            "AND [Rowed_CTE].RowNumber <= @Limit + @Offset " +
            ") " +
            "ORDER BY [ProductPlacementMovement].ID DESC",
            (movement, fromPlacement, toPlacement, responsible, product, storage) => {
                fromPlacement.Product = product;
                toPlacement.Product = product;

                fromPlacement.Storage = storage;
                toPlacement.Storage = storage;

                movement.FromProductPlacement = fromPlacement;
                movement.ToProductPlacement = toPlacement;
                movement.Responsible = responsible;

                return movement;
            },
            new {
                StorageNetId = storageNetId,
                Value = value,
                Limit = limit,
                Offset = offset,
                From = from,
                To = to
            }
        );
    }

    public ProductPlacementMovement GetLastRecord(long organizationId) {
        return _connection.Query<ProductPlacementMovement>(
            "SELECT [ProductPlacementMovement].* " +
            "FROM [ProductPlacementMovement] " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ID = [ProductPlacementMovement].FromProductPlacementID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductPlacement].StorageID " +
            "WHERE [ProductPlacementMovement].Deleted = 0 " +
            "AND [Storage].OrganizationID = @OrganizationId " +
            "ORDER BY [ProductPlacementMovement].ID DESC",
            new { OrganizationId = organizationId }
        ).SingleOrDefault();
    }
}