using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductOriginalNumberRepository : IProductOriginalNumberRepository {
    private readonly IDbConnection _connection;

    public ProductOriginalNumberRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ProductOriginalNumber productOriginalNumber) {
        return _connection.Query<long>(
            "INSERT INTO ProductOriginalNumber (OriginalNumberID, ProductID, IsMainOriginalNumber, Updated) " +
            "VALUES(@OriginalNumberId, @ProductId, @IsMainOriginalNumber, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            productOriginalNumber
        ).FirstOrDefault();
    }

    public void Add(IEnumerable<ProductOriginalNumber> productOriginalNumbers) {
        _connection.Execute(
            "INSERT INTO ProductOriginalNumber (OriginalNumberID, ProductID, IsMainOriginalNumber, Updated) " +
            "VALUES(@OriginalNumberId, @ProductId, @IsMainOriginalNumber, getutcdate())",
            productOriginalNumbers
        );
    }

    public void SetNotMainByProductId(long id) {
        _connection.Execute(
            "UPDATE ProductOriginalNumber " +
            "SET IsMainOriginalNumber = 0, Updated = getutcdate() " +
            "WHERE Deleted = 0 " +
            "AND ProductID = @Id",
            new { Id = id }
        );
    }

    public void Update(ProductOriginalNumber productOriginalNumber) {
        _connection.Execute(
            "UPDATE ProductOriginalNumber " +
            "SET OriginalNumberID = @OriginalNumberId, ProductID = @ProductId, IsMainOriginalNumber = @IsMainOriginalNumber, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            productOriginalNumber
        );
    }

    public void Update(IEnumerable<ProductOriginalNumber> productOriginalNumbers) {
        _connection.Execute(
            "UPDATE ProductOriginalNumber " +
            "SET OriginalNumberID = @OriginalNumberId, ProductID = @ProductId, IsMainOriginalNumber = @IsMainOriginalNumber, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            productOriginalNumbers
        );
    }

    public void Remove(ProductOriginalNumber productOriginalNumber) {
        _connection.Execute(
            "UPDATE ProductOriginalNumber " +
            "SET Deleted = 1 " +
            "WHERE NetUID = @NetUid",
            productOriginalNumber
        );
    }


    public void Remove(IEnumerable<ProductOriginalNumber> productOriginalNumbers) {
        _connection.Execute(
            "UPDATE ProductOriginalNumber " +
            "SET Deleted = 1 " +
            "WHERE NetUID = @NetUid",
            productOriginalNumbers
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ProductOriginalNumber] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductOriginalNumber].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RemoveAllByProductId(long productId) {
        _connection.Execute(
            "UPDATE [ProductOriginalNumber] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductOriginalNumber].ProductID = @ProductId",
            new { ProductId = productId }
        );
    }

    public ProductOriginalNumber GetMainByProductId(long productId) {
        return _connection.Query<ProductOriginalNumber>(
            "SELECT * FROM [ProductOriginalNumber] " +
            "WHERE [ProductOriginalNumber].[IsMainOriginalNumber] = 1 " +
            "AND [ProductOriginalNumber].[ProductID] = @Id; ", new { Id = productId }).FirstOrDefault();
    }

    public IEnumerable<ProductOriginalNumber> GetByProductId(long productId) {
        return _connection.Query<ProductOriginalNumber, OriginalNumber, ProductOriginalNumber>(
                "SELECT * FROM [ProductOriginalNumber] " +
                "LEFT JOIN [OriginalNumber] " +
                "ON [OriginalNumber].[ID] = [ProductOriginalNumber].[OriginalNumberID] " +
                "WHERE [ProductOriginalNumber].[ProductID] = @Id " +
                "AND [ProductOriginalNumber].[Deleted] = 0 ",
                //"AND [OriginalNumber].[Deleted] = 0 ",
                (productOriginalNumber, originalNumber) => {
                    productOriginalNumber.OriginalNumber = originalNumber;
                    return productOriginalNumber;
                }
                , new { Id = productId })
            .AsEnumerable();
    }

    public ProductOriginalNumber GetByProductAndNumberId(long productId, long originalNumberId) {
        return _connection.Query<ProductOriginalNumber>(
                "SELECT * FROM [ProductOriginalNumber] " +
                "WHERE [ProductOriginalNumber].[ProductID] = @ProductId " +
                "AND [ProductOriginalNumber].[OriginalNumberID] = @OriginalNumberId; "
                , new { ProductId = productId, OriginalNumberId = originalNumberId })
            .FirstOrDefault();
    }

    public void RemoveByOriginalNumberId(long originalNumberId) {
        _connection.Execute(
            "UPDATE [ProductOriginalNumber] " +
            "SET [Updated] = GETUTCDATE(), [Deleted] = 1 " +
            "WHERE [ProductOriginalNumber].[OriginalNumberID] = @Id ",
            new { Id = originalNumberId });
    }

    public void DeleteAllByIds(IEnumerable<long> ids) {
        _connection.Execute("DELETE FROM [ProductOriginalNumber] WHERE ID IN @Ids",
            new { Ids = ids }
        );
    }

    public bool CheckIsProductOriginalNumberExistsByBaseProductIdAndOriginalNumber(long baseProductId, string number) {
        return _connection.Query<bool>(
            "SELECT " +
            "CASE WHEN EXISTS ( " +
            "SELECT * FROM [ProductOriginalNumber] " +
            "LEFT JOIN [OriginalNumber] " +
            "ON [OriginalNumber].ID = [ProductOriginalNumber].OriginalNumberID " +
            "WHERE [ProductOriginalNumber].ProductID = @BaseProductId " +
            "AND [OriginalNumber].MainNumber = @Number " +
            "AND [ProductOriginalNumber].Deleted = 0 " +
            ") THEN 1 " +
            "ELSE 0 " +
            "END AS Result ",
            new { BaseProductId = baseProductId, Number = number }
        ).FirstOrDefault();
    }
}