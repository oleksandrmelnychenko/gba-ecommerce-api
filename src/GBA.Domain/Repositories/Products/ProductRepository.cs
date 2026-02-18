using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductRepository : IProductRepository {
    private readonly IDbConnection _connection;

    public ProductRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(Product product) {
        return _connection.Query<long>(
            "INSERT INTO Product (Description, HasAnalogue, HasComponent, HasImage, IsForSale, IsForWeb, IsForZeroSale, MainOriginalNumber, MeasureUnitID, " +
            "Name, OrderStandard, PackingStandard, Size, UCGFEA, VendorCode, Volume, Weight, [Top], NameUA, NamePL, DescriptionUA, DescriptionPL, NotesPL, NotesUA, " +
            "SynonymsUA, SynonymsPL, SearchSynonymsUA, SearchSynonymsPL, SearchName, SearchNameUA, SearchDescription, SearchDescriptionUA, SearchSize, " +
            "SearchVendorCode, SearchNamePL, SearchDescriptionPL, Updated, [Standard]) " +
            "VALUES(@Description, @HasAnalogue, @HasComponent, @HasImage, @IsForSale, @IsForWeb, @IsForZeroSale, @MainOriginalNumber, @MeasureUnitID, " +
            "@Name, @OrderStandard, @PackingStandard, @Size, @UCGFEA, @VendorCode, @Volume, @Weight, @Top, @NameUA, @NamePL, @DescriptionUA, @DescriptionPL, " +
            "@NotesPL, @NotesUA, @SynonymsUA, @SynonymsPL, @SearchSynonymsUA, @SearchSynonymsPL, @SearchName, @SearchNameUA, @SearchDescription, @SearchDescriptionUA, " +
            "@SearchSize, @SearchVendorCode, @SearchNamePL, @SearchDescriptionPL, getutcdate(), @Standard); " +
            "SELECT SCOPE_IDENTITY()",
            product
        ).Single();
    }

    public void UpdateMainOriginalNumber(string mainOriginalNumber, Guid netUid) {
        _connection.Execute(
            "UPDATE Product " +
            "SET MainOriginalNumber = @MainOriginalNumber, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            new {
                MainOriginalNumber = mainOriginalNumber,
                NetUid = netUid
            });
    }

    public void Update(Product product) {
        _connection.Execute(
            "UPDATE Product " +
            "SET Description = @Description, HasAnalogue = @HasAnalogue, HasComponent = @HasComponent, HasImage = @HasImage, IsForSale = @IsForSale, " +
            "IsForWeb = @IsForWeb, IsForZeroSale = @IsForZeroSale, MainOriginalNumber = @MainOriginalNumber, MeasureUnitID = @MeasureUnitID, " +
            "Name = @Name, OrderStandard = @OrderStandard, PackingStandard = @PackingStandard, Size = @Size, UCGFEA = @UCGFEA, VendorCode = @VendorCode, " +
            "Volume = @Volume, Weight = @Weight, [Top] = @Top, NameUA = @NameUA, NamePL = @NamePL, DescriptionUA = @DescriptionUA, DescriptionPL = @DescriptionPL, " +
            "Image = @Image, NotesPL = @NotesPL, NotesUA = @NotesUA, SynonymsUA = @SynonymsUA, SynonymsPL = @SynonymsPL, SearchSynonymsUA = @SearchSynonymsUA, " +
            "SearchSynonymsPL = @SearchSynonymsPL, SearchName = @SearchName, SearchNameUA = @SearchNameUA, SearchDescription = @SearchDescription, " +
            "SearchDescriptionUA = @SearchDescriptionUA, SearchSize = @SearchSize, SearchVendorCode = @SearchVendorCode, SearchNamePL = @SearchNamePL, " +
            "SearchDescriptionPL = @SearchDescriptionPL, [Standard] = @Standard, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            new {
                product.Description,
                product.HasAnalogue,
                product.HasComponent,
                product.HasImage,
                product.IsForSale,
                product.IsForWeb,
                product.IsForZeroSale,
                product.MainOriginalNumber,
                MeasureUnitID = product.MeasureUnitId,
                product.Name,
                product.OrderStandard,
                product.PackingStandard,
                product.Size,
                product.UCGFEA,
                product.VendorCode,
                product.Volume,
                product.Weight,
                product.Top,
                product.NameUA,
                product.NamePL,
                product.DescriptionUA,
                product.DescriptionPL,
                product.Image,
                product.NotesPL,
                product.NotesUA,
                product.SynonymsUA,
                product.SynonymsPL,
                product.SearchSynonymsUA,
                product.SearchSynonymsPL,
                product.SearchName,
                product.SearchNameUA,
                product.SearchDescription,
                product.SearchDescriptionUA,
                product.SearchSize,
                product.SearchVendorCode,
                product.SearchNamePL,
                product.SearchDescriptionPL,
                product.Standard,
                product.NetUid
            }
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE Product SET Deleted = 1 WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE Product SET Deleted = 1 WHERE ID = @Id",
            new { Id = id }
        );
    }


    public void UpdateProductHasAnalogue() {
        _connection.Execute("UPDATE [Product] " +
                            "SET [HasAnalogue] = 1 " +
                            "WHERE ID IN ( " +
                            "SELECT [ProductAnalogue].[BaseProductID] FROM [ProductAnalogue] " +
                            "WHERE  [ProductAnalogue].[BaseProductID] = [Product].[ID] " +
                            ") ");
    }

    public void UpdateProductHasComponent() {
        _connection.Execute("UPDATE Product " +
                            "SET HasComponent = 1 " +
                            "WHERE ID IN ( " +
                            "SELECT ProductSet.BaseProductID FROM ProductSet " +
                            "WHERE  ProductSet.BaseProductID = Product.[ID] " +
                            ") ");
    }

    public void UpdateProductHasAnalogue(long baseProductId) {
        _connection.Execute(
            "UPDATE [Product] " +
            "SET [HasAnalogue] = 1 " +
            "WHERE ID = Id ",
            new { Id = baseProductId });
    }

    public void UpdateProductHasComponent(long baseProductId) {
        _connection.Execute(
            "UPDATE [Product] " +
            "SET [HasComponent] = 1 " +
            "WHERE ID = @Id ",
            new { Id = baseProductId });
    }

    public void UncheckProductHasAnalogue(IEnumerable<long> baseProductIds) {
        _connection.Execute(
            "UPDATE [Product] " +
            "SET [HasAnalogue] = 0 " +
            "WHERE ID IN @Ids ",
            new { Ids = baseProductIds });
    }

    public void UncheckProductHasComponent(IEnumerable<long> baseProductIds) {
        _connection.Execute(
            "UPDATE [Product] " +
            "SET [HasComponent] = 0 " +
            "WHERE ID IN @Ids ",
            new { Ids = baseProductIds });
    }
}