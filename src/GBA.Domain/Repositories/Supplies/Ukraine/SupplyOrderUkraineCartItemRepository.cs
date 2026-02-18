using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class SupplyOrderUkraineCartItemRepository : ISupplyOrderUkraineCartItemRepository {
    private readonly IDbConnection _connection;

    public SupplyOrderUkraineCartItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupplyOrderUkraineCartItem item) {
        return _connection.Query<long>(
            "INSERT INTO [SupplyOrderUkraineCartItem] " +
            "(Comment, UploadedQty, ItemPriority, ProductId, CreatedById, UpdatedById, ResponsibleId, ReservedQty, FromDate, TaxFreePackListId, UnpackedQty, " +
            "NetWeight, UnitPrice, SupplierId, PackingListPackageOrderItemId, MaxQtyPerTF, IsRecommended, Deleted, Updated) " +
            "VALUES " +
            "(@Comment, @UploadedQty, @ItemPriority, @ProductId, @CreatedById, @UpdatedById, @ResponsibleId, @ReservedQty, @FromDate, @TaxFreePackListId, @UnpackedQty, " +
            "@NetWeight, @UnitPrice, @SupplierId, @PackingListPackageOrderItemId, @MaxQtyPerTF, @IsRecommended, @Deleted, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            item
        ).Single();
    }

    public void Add(IEnumerable<SupplyOrderUkraineCartItem> items) {
        _connection.Execute(
            "INSERT INTO [SupplyOrderUkraineCartItem] " +
            "(Comment, UploadedQty, ItemPriority, ProductId, CreatedById, UpdatedById, ResponsibleId, ReservedQty, FromDate, TaxFreePackListId, UnpackedQty, " +
            "NetWeight, UnitPrice, SupplierId, PackingListPackageOrderItemId, MaxQtyPerTF, IsRecommended, Updated) " +
            "VALUES " +
            "(@Comment, @UploadedQty, @ItemPriority, @ProductId, @CreatedById, @UpdatedById, @ResponsibleId, @ReservedQty, @FromDate, @TaxFreePackListId, @UnpackedQty, " +
            "@NetWeight, @UnitPrice, @SupplierId, @PackingListPackageOrderItemId, @MaxQtyPerTF, @IsRecommended, GETUTCDATE())",
            items
        );
    }

    public void Update(SupplyOrderUkraineCartItem item) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineCartItem] " +
            "SET UploadedQty = @UploadedQty, ReservedQty = @ReservedQty, FromDate = @FromDate, UpdatedById = @UpdatedById, Comment = @Comment, " +
            "TaxFreePackListId = @TaxFreePackListId, ResponsibleId = @ResponsibleId, UnpackedQty = @UnpackedQty, Deleted = @Deleted, " +
            "IsRecommended = @IsRecommended, UnitPrice = @UnitPrice, NetWeight = @NetWeight, Updated = GETUTCDATE() " +
            "WHERE [SupplyOrderUkraineCartItem].ID = @Id",
            item
        );
    }

    public void Update(IEnumerable<SupplyOrderUkraineCartItem> items) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineCartItem] " +
            "SET UploadedQty = @UploadedQty, ReservedQty = @ReservedQty, FromDate = @FromDate, UpdatedById = @UpdatedById, Comment = @Comment, " +
            "TaxFreePackListId = @TaxFreePackListId, ResponsibleId = @ResponsibleId, UnpackedQty = @UnpackedQty, IsRecommended = @IsRecommended, Updated = GETUTCDATE() " +
            "WHERE [SupplyOrderUkraineCartItem].ID = @Id",
            items
        );
    }

    public void Remove(long id) {
        _connection.Query<SupplyOrderUkraineCartItem>(
            "UPDATE [SupplyOrderUkraineCartItem] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public void ReturnItemsToCartFromTaxFreePackListExceptProvided(long packListId, long updatedById, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineCartItem] " +
            "SET UpdatedByID = @UpdatedById, TaxFreePackListID = NULL, ResponsibleID = NULL, UnpackedQty = 0, Updated = GETUTCDATE() " +
            "WHERE TaxFreePackListID = @PackingListId " +
            "AND ID NOT IN @Ids",
            new { PackingListId = packListId, UpdatedById = updatedById, Ids = ids }
        );
    }

    public void RestoreUnpackedQtyByTaxFreeItemsIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineCartItem] " +
            "SET UnpackedQty = UnpackedQty + Qty, Updated = GETUTCDATE() " +
            "FROM [SupplyOrderUkraineCartItem] " +
            "LEFT JOIN [TaxFreeItem] " +
            "ON [TaxFreeItem].SupplyOrderUkraineCartItemID = [SupplyOrderUkraineCartItem].ID " +
            "WHERE [TaxFreeItem].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RestoreUnpackedQtyByTaxFreeIdExceptProvidedIds(long taxFreeId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineCartItem] " +
            "SET UnpackedQty = UnpackedQty + Qty, Updated = GETUTCDATE() " +
            "FROM [SupplyOrderUkraineCartItem] " +
            "LEFT JOIN [TaxFreeItem] " +
            "ON [TaxFreeItem].SupplyOrderUkraineCartItemID = [SupplyOrderUkraineCartItem].ID " +
            "WHERE [TaxFreeItem].TaxFreeID = @TaxFreeId " +
            "AND [TaxFreeItem].ID NOT IN @Ids " +
            "AND [TaxFreeItem].Deleted = 0",
            new { TaxFreeId = taxFreeId, Ids = ids }
        );
    }

    public void UpdateUnpackedQty(long id, double qty) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineCartItem] " +
            "SET UnpackedQty = UnpackedQty + @Qty, Updated = GETUTCDATE() " +
            "WHERE [SupplyOrderUkraineCartItem].ID = @Id",
            new { Id = id, Qty = qty }
        );
    }

    public void UpdateMaxQtyPerTf(SupplyOrderUkraineCartItem item) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineCartItem] " +
            "SET MaxQtyPerTF = @MaxQtyPerTF, Updated = GETUTCDATE() " +
            "WHERE [SupplyOrderUkraineCartItem].ID = @Id",
            item
        );
    }

    public void DecreaseUnpackedQtyById(long id, double qty) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineCartItem] " +
            "SET UnpackedQty = UnpackedQty - @Qty, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            new { Id = id, Qty = qty }
        );
    }

    public SupplyOrderUkraineCartItem GetByProductIdIfExists(long id) {
        return _connection.Query<SupplyOrderUkraineCartItem>(
            "SELECT TOP(1) [SupplyOrderUkraineCartItem].* " +
            "FROM [SupplyOrderUkraineCartItem] " +
            "LEFT JOIN [SadItem] " +
            "ON [SadItem].SupplyOrderUkraineCartItemID = [SupplyOrderUkraineCartItem].ID " +
            "WHERE [SupplyOrderUkraineCartItem].ProductID = @Id " +
            "AND [SupplyOrderUkraineCartItem].TaxFreePackListID IS NULL " +
            "AND [SadItem].ID IS NULL",
            new { Id = id }
        ).SingleOrDefault();
    }

    public SupplyOrderUkraineCartItem GetByProductAndTaxFreePackListIdsIfExists(long productId, long packListId) {
        return _connection.Query<SupplyOrderUkraineCartItem>(
            "SELECT TOP(1) * " +
            "FROM [SupplyOrderUkraineCartItem] " +
            "WHERE [SupplyOrderUkraineCartItem].Deleted = 0 " +
            "AND [SupplyOrderUkraineCartItem].TaxFreePackListID = @PackListId " +
            "AND [SupplyOrderUkraineCartItem].ProductID = @ProductId",
            new { ProductId = productId, PackListId = packListId }
        ).SingleOrDefault();
    }

    public SupplyOrderUkraineCartItem GetById(long id) {
        SupplyOrderUkraineCartItem toReturn = null;

        _connection.Query<SupplyOrderUkraineCartItem, Product, MeasureUnit, User, User, User, SupplyOrderUkraineCartItem>(
            "SELECT [SupplyOrderUkraineCartItem].* " +
            ", (" +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].Deleted = 0 " +
            "AND [ProductAvailability].ProductID = [SupplyOrderUkraineCartItem].ProductID " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'pl'" +
            ") AS [AvailableQty] " +
            ",[Product].* " +
            ",[MeasureUnit].* " +
            ",[CreatedBy].* " +
            ",[UpdatedBy].* " +
            ",[Responsible].* " +
            "FROM [SupplyOrderUkraineCartItem] " +
            "LEFT JOIN [Product] " +
            "ON [SupplyOrderUkraineCartItem].ProductID = [Product].ID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [SupplyOrderUkraineCartItem].CreatedByID = [CreatedBy].ID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [SupplyOrderUkraineCartItem].UpdatedByID = [UpdatedBy].ID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [SupplyOrderUkraineCartItem].ResponsibleID = [Responsible].ID " +
            "WHERE [SupplyOrderUkraineCartItem].ID = @Id",
            (item, product, measureUnit, createdBy, updatedBy, responsible) => {
                if (toReturn != null) return item;

                product.MeasureUnit = measureUnit;

                item.Product = product;
                item.CreatedBy = createdBy;
                item.UpdatedBy = updatedBy;
                item.Responsible = responsible;

                toReturn = item;

                return item;
            },
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public SupplyOrderUkraineCartItem GetAssignedItemByTaxFreePackListAndConsignmentItemIfExists(long taxFreePackListId, long consignmentItemId) {
        return _connection.Query<SupplyOrderUkraineCartItem>(
            "SELECT * " +
            "FROM [SupplyOrderUkraineCartItem] " +
            "WHERE [SupplyOrderUkraineCartItem].ID = (" +
            "SELECT TOP(1) [CartItem].ID " +
            "FROM [SupplyOrderUkraineCartItem] AS [CartItem] " +
            "LEFT JOIN [SupplyOrderUkraineCartItemReservation] AS [CartItemReservation] " +
            "ON [CartItemReservation].SupplyOrderUkraineCartItemID = [CartItem].ID " +
            "AND [CartItemReservation].Deleted = 0 " +
            "WHERE [CartItem].Deleted = 0 " +
            "AND [CartItem].TaxFreePackListID = @TaxFreePackListId " +
            "AND [CartItemReservation].ConsignmentItemID = @ConsignmentItemId " +
            ")",
            new { TaxFreePackListId = taxFreePackListId, ConsignmentItemId = consignmentItemId }
        ).SingleOrDefault();
    }

    public SupplyOrderUkraineCartItem GetByIdWithReservations(long id) {
        SupplyOrderUkraineCartItem toReturn = null;

        Type[] types = {
            typeof(SupplyOrderUkraineCartItem),
            typeof(SupplyOrderUkraineCartItemReservation),
            typeof(ProductAvailability)
        };

        Func<object[], SupplyOrderUkraineCartItem> mapper = objects => {
            SupplyOrderUkraineCartItem cartItem = (SupplyOrderUkraineCartItem)objects[0];
            SupplyOrderUkraineCartItemReservation reservation = (SupplyOrderUkraineCartItemReservation)objects[1];
            ProductAvailability availability = (ProductAvailability)objects[2];

            if (toReturn == null)
                toReturn = cartItem;

            if (reservation == null) return cartItem;

            reservation.ProductAvailability = availability;

            toReturn.SupplyOrderUkraineCartItemReservations.Add(reservation);

            return cartItem;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [SupplyOrderUkraineCartItem] " +
            "LEFT JOIN [SupplyOrderUkraineCartItemReservation] " +
            "ON [SupplyOrderUkraineCartItemReservation].SupplyOrderUkraineCartItemID = [SupplyOrderUkraineCartItem].ID " +
            "AND [SupplyOrderUkraineCartItemReservation].Deleted = 0 " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = [SupplyOrderUkraineCartItemReservation].ProductAvailabilityID " +
            "WHERE [SupplyOrderUkraineCartItem].ID = @Id",
            types,
            mapper,
            new { Id = id }
        );

        return toReturn;
    }

    public SupplyOrderUkraineCartItem GetByIdWithoutMovementInfo(long id) {
        return _connection.Query<SupplyOrderUkraineCartItem, Product, MeasureUnit, User, User, User, SupplyOrderUkraineCartItem>(
            "SELECT [SupplyOrderUkraineCartItem].* " +
            ", (" +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].Deleted = 0 " +
            "AND [ProductAvailability].ProductID = [SupplyOrderUkraineCartItem].ProductID " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'pl'" +
            ") AS [AvailableQty] " +
            ",[Product].* " +
            ",[MeasureUnit].* " +
            ",[CreatedBy].* " +
            ",[UpdatedBy].* " +
            ",[Responsible].* " +
            "FROM [SupplyOrderUkraineCartItem] " +
            "LEFT JOIN [Product] " +
            "ON [SupplyOrderUkraineCartItem].ProductID = [Product].ID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [SupplyOrderUkraineCartItem].CreatedByID = [CreatedBy].ID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [SupplyOrderUkraineCartItem].UpdatedByID = [UpdatedBy].ID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [SupplyOrderUkraineCartItem].ResponsibleID = [Responsible].ID " +
            "WHERE [SupplyOrderUkraineCartItem].ID = @Id",
            (item, product, measureUnit, createdBy, updatedBy, responsible) => {
                product.MeasureUnit = measureUnit;

                item.Product = product;
                item.CreatedBy = createdBy;
                item.UpdatedBy = updatedBy;
                item.Responsible = responsible;

                return item;
            },
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).SingleOrDefault();
    }

    public IEnumerable<SupplyOrderUkraineCartItem> GetAll() {
        return _connection.Query<SupplyOrderUkraineCartItem, Product, MeasureUnit, User, User, User, SupplyOrderUkraineCartItem>(
            "SELECT [SupplyOrderUkraineCartItem].* " +
            ", (" +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].Deleted = 0 " +
            "AND [ProductAvailability].ProductID = [SupplyOrderUkraineCartItem].ProductID " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'pl'" +
            ") AS [AvailableQty] " +
            ",[Product].* " +
            ",[MeasureUnit].* " +
            ",[CreatedBy].* " +
            ",[UpdatedBy].* " +
            ",[Responsible].* " +
            "FROM [SupplyOrderUkraineCartItem] " +
            "LEFT JOIN [Product] " +
            "ON [SupplyOrderUkraineCartItem].ProductID = [Product].ID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [SupplyOrderUkraineCartItem].CreatedByID = [CreatedBy].ID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [SupplyOrderUkraineCartItem].UpdatedByID = [UpdatedBy].ID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [SupplyOrderUkraineCartItem].ResponsibleID = [Responsible].ID " +
            "LEFT JOIN [SadItem] " +
            "ON [SadItem].SupplyOrderUkraineCartItemID = [SupplyOrderUkraineCartItem].ID " +
            "WHERE [SupplyOrderUkraineCartItem].Deleted = 0 " +
            "AND [SupplyOrderUkraineCartItem].TaxFreePackListID IS NULL " +
            "AND [SadItem].ID IS NULL " +
            "ORDER BY [SupplyOrderUkraineCartItem].FromDate, [Product].VendorCode, [AvailableQty], [SupplyOrderUkraineCartItem].ReservedQty",
            (item, product, measureUnit, createdBy, updatedBy, responsible) => {
                product.MeasureUnit = measureUnit;

                item.Product = product;
                item.CreatedBy = createdBy;
                item.UpdatedBy = updatedBy;
                item.Responsible = responsible;

                return item;
            },
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public IEnumerable<SupplyOrderUkraineCartItem> GetAllByPackListIdExceptProvided(long packListId, IEnumerable<long> ids) {
        List<SupplyOrderUkraineCartItem> items = new();

        Type[] types = {
            typeof(SupplyOrderUkraineCartItem),
            typeof(SupplyOrderUkraineCartItemReservation),
            typeof(ProductAvailability),
            typeof(ConsignmentItem)
        };

        Func<object[], SupplyOrderUkraineCartItem> mapper = objects => {
            SupplyOrderUkraineCartItem cartItem = (SupplyOrderUkraineCartItem)objects[0];
            SupplyOrderUkraineCartItemReservation cartItemReservation = (SupplyOrderUkraineCartItemReservation)objects[1];
            ProductAvailability availability = (ProductAvailability)objects[2];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[3];

            if (items.Any(i => i.Id.Equals(cartItem.Id)))
                cartItem = items.First(i => i.Id.Equals(cartItem.Id));
            else
                items.Add(cartItem);

            if (cartItemReservation == null) return cartItem;

            cartItemReservation.ProductAvailability = availability;
            cartItemReservation.ConsignmentItem = consignmentItem;

            cartItem.SupplyOrderUkraineCartItemReservations.Add(cartItemReservation);

            return cartItem;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [SupplyOrderUkraineCartItem] " +
            "LEFT JOIN [SupplyOrderUkraineCartItemReservation] " +
            "ON [SupplyOrderUkraineCartItemReservation].SupplyOrderUkraineCartItemID = [SupplyOrderUkraineCartItem].ID " +
            "AND [SupplyOrderUkraineCartItemReservation].Deleted = 0 " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = [SupplyOrderUkraineCartItemReservation].ProductAvailabilityID " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [SupplyOrderUkraineCartItemReservation].ConsignmentItemID " +
            "WHERE [SupplyOrderUkraineCartItem].Deleted = 0 " +
            "AND [SupplyOrderUkraineCartItem].ID NOT IN @Ids " +
            "AND [SupplyOrderUkraineCartItem].TaxFreePackListID = @PackListId",
            types,
            mapper,
            new { PackListId = packListId, Ids = ids }
        );

        return items;
    }

    public SupplyOrderUkraineCartItem GetByProductIdIfReserved(long productId) {
        return _connection.Query<SupplyOrderUkraineCartItem, Product, MeasureUnit, User, User, User, SupplyOrderUkraineCartItem>(
            "SELECT [SupplyOrderUkraineCartItem].* " +
            ", (" +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].Deleted = 0 " +
            "AND [ProductAvailability].ProductID = [SupplyOrderUkraineCartItem].ProductID " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'pl'" +
            ") AS [AvailableQty] " +
            ",[Product].* " +
            ",[MeasureUnit].* " +
            ",[CreatedBy].* " +
            ",[UpdatedBy].* " +
            ",[Responsible].* " +
            "FROM [SupplyOrderUkraineCartItem] " +
            "LEFT JOIN [Product] " +
            "ON [SupplyOrderUkraineCartItem].ProductID = [Product].ID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [SupplyOrderUkraineCartItem].CreatedByID = [CreatedBy].ID " +
            "LEFT JOIN [User] AS [UpdatedBy] " +
            "ON [SupplyOrderUkraineCartItem].UpdatedByID = [UpdatedBy].ID " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [SupplyOrderUkraineCartItem].ResponsibleID = [Responsible].ID " +
            "LEFT JOIN [SadItem] " +
            "ON [SadItem].SupplyOrderUkraineCartItemID = [SupplyOrderUkraineCartItem].ID " +
            "WHERE [SupplyOrderUkraineCartItem].Deleted = 0 " +
            "AND [SupplyOrderUkraineCartItem].TaxFreePackListID IS NULL " +
            "AND [SadItem].ID IS NULL " +
            "AND [SupplyOrderUkraineCartItem].ReservedQty <> 0 " +
            "AND [SupplyOrderUkraineCartItem].ProductID = @ProductId",
            (item, product, measureUnit, createdBy, updatedBy, responsible) => {
                product.MeasureUnit = measureUnit;

                item.Product = product;
                item.CreatedBy = createdBy;
                item.UpdatedBy = updatedBy;
                item.Responsible = responsible;

                return item;
            },
            new { ProductId = productId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).SingleOrDefault();
    }
}