using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductAnalogueRepository : IProductAnalogueRepository {
    private readonly IDbConnection _connection;

    public ProductAnalogueRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ProductAnalogue productAnalogue) {
        return _connection.Query<long>(
            "INSERT INTO ProductAnalogue(AnalogueProductID, BaseProductID, Updated) " +
            "VALUES(@AnalogueProductId, @BaseProductId, getutcdate());" +
            "SELECT SCOPE_IDENTITY()",
            productAnalogue
        ).FirstOrDefault();
    }

    public void Add(IEnumerable<ProductAnalogue> productAnalogues) {
        _connection.Execute(
            "INSERT INTO ProductAnalogue(AnalogueProductID, BaseProductID, Updated) " +
            "VALUES(@AnalogueProductId, @BaseProductId, getutcdate())",
            productAnalogues
        );
    }

    public void Remove(ProductAnalogue productAnalogue) {
        _connection.Execute(
            "UPDATE ProductAnalogue " +
            "SET Deleted = 1 " +
            "WHERE NetUID = @NetUid",
            productAnalogue
        );
    }

    public void Remove(IEnumerable<ProductAnalogue> productAnalogues) {
        _connection.Execute(
            "UPDATE ProductAnalogue " +
            "SET Deleted = 1 " +
            "WHERE NetUID = @NetUid",
            productAnalogues
        );
    }

    public void Update(ProductAnalogue productAnalogue) {
        _connection.Execute(
            "UPDATE ProductAnalogue " +
            "SET AnalogueProductID = @AnalogueProductId, BaseProductID = @BaseProductId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            productAnalogue
        );
    }

    public void Update(IEnumerable<ProductAnalogue> productAnalogues) {
        _connection.Execute(
            "UPDATE ProductAnalogue " +
            "SET AnalogueProductID = @AnalogueProductId, BaseProductID = @BaseProductId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            productAnalogues
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ProductAnalogue] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductAnalogue].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RemoveAllByProductId(long productId) {
        _connection.Execute(
            "UPDATE [ProductAnalogue] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductAnalogue].BaseProductID = @ProductId",
            new { ProductId = productId }
        );
    }

    public void DeleteByBaseProductAndAnalogueNetIds(Guid baseProductNetId, Guid analogueNetId) {
        _connection.Execute(
            "DELETE FROM [ProductAnalogue] " +
            "WHERE ProductAnalogue.ID = ( " +
            "SELECT [ProductAnalogue].ID FROM [ProductAnalogue] " +
            "LEFT JOIN [Product] [BaseProduct] " +
            "ON [BaseProduct].ID = [ProductAnalogue].BaseProductID " +
            "LEFT JOIN [Product] [Analogue] " +
            "ON [Analogue].ID = [ProductAnalogue].AnalogueProductID " +
            "WHERE [BaseProduct].NetUID = @BaseProductNetId " +
            "AND [Analogue].NetUID = @AnalogueNetId )",
            new { BaseProductNetId = baseProductNetId, AnalogueNetId = analogueNetId });
    }

    public void DeleteAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "DELETE FROM [ProductAnalogue] WHERE [ProductAnalogue].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public bool CheckIsProductAnalogueExistsByBaseProductAndAnalogueIds(long baseProductId, long analogueId) {
        return _connection.Query<bool>(
            "SELECT " +
            "CASE WHEN EXISTS ( " +
            "SELECT * FROM ProductAnalogue " +
            "WHERE BaseProductID = @BaseProductId " +
            "AND AnalogueProductID = @AnalogueProductId " +
            "AND Deleted = 0 " +
            ") THEN 1 " +
            "ELSE 0 " +
            "END AS Result ",
            new { BaseProductId = baseProductId, AnalogueProductId = analogueId }
        ).FirstOrDefault();
    }

    public bool CheckIsProductAnalogueExistsByBaseProductAndAnalogueNetIds(Guid baseProductNetId, Guid analogueNetId) {
        return _connection.Query<bool>(
            "SELECT " +
            "CASE WHEN EXISTS ( " +
            "SELECT * FROM [ProductAnalogue] " +
            "LEFT JOIN [Product] [BaseProduct] " +
            "ON [BaseProduct].ID = [ProductAnalogue].BaseProductID " +
            "LEFT JOIN [Product] [Analogue] " +
            "ON [Analogue].ID = [ProductAnalogue].[AnalogueProductID] " +
            "WHERE [BaseProduct].NetUID = @BaseProductNetId " +
            "AND [Analogue].NetUID = @AnalogueNetId " +
            "AND [ProductAnalogue].Deleted = 0 " +
            ") THEN 1 " +
            "ELSE 0 " +
            "END AS Result ",
            new { BaseProductNetId = baseProductNetId, AnalogueNetId = analogueNetId }
        ).FirstOrDefault();
    }

    public List<ProductAnalogue> GetAllProductAnaloguesByBaseProductVendorCode(string vendorCode) {
        return _connection.Query<ProductAnalogue, Product, Product, ProductAnalogue>(
            "SELECT * FROM [ProductAnalogue] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductAnalogue].BaseProductID " +
            "LEFT JOIN [Product] [Analogue] " +
            "ON [Analogue].ID = [ProductAnalogue].AnalogueProductID " +
            "WHERE [Product].VendorCode = @VendorCode " +
            "AND [Product].Deleted = 0 " +
            "AND [Analogue].Deleted = 0 ",
            (productAnalogue, product, analogue) => {
                productAnalogue.BaseProduct = product;
                productAnalogue.AnalogueProduct = analogue;

                return productAnalogue;
            },
            new { VendorCode = vendorCode }
        ).ToList();
    }

    public List<ProductAnalogue> GetAllProductAnaloguesByAnalogueVendorCode(string vendorCode) {
        return _connection.Query<ProductAnalogue, Product, Product, ProductAnalogue>(
            "SELECT * FROM [ProductAnalogue] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductAnalogue].BaseProductID " +
            "LEFT JOIN [Product] [Analogue] " +
            "ON [Analogue].ID = [ProductAnalogue].AnalogueProductID " +
            "WHERE [Analogue].VendorCode = @VendorCode " +
            "AND [Product].Deleted = 0 " +
            "AND [Analogue].Deleted = 0 ",
            (productAnalogue, product, analogue) => {
                productAnalogue.BaseProduct = product;
                productAnalogue.AnalogueProduct = analogue;

                return productAnalogue;
            },
            new { VendorCode = vendorCode }
        ).ToList();
    }
}