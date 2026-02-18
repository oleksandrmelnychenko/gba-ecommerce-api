using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using GBA.Domain.EntityHelpers.SalesModels;
using GBA.Domain.EntityHelpers.SalesModels.ChartOfSalesModels;
using GBA.Domain.EntityHelpers.SalesModels.Models;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class OrderItemRepository : IOrderItemRepository {
    private readonly IDbConnection _connection;

    public OrderItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(OrderItem orderItem) {
        return _connection.Query<long>(
            "INSERT INTO [OrderItem] (ClientShoppingCartId, OrderId, UserId, ProductId, Qty, OverLordQty, Comment, IsValidForCurrentSale, PricePerItem, OrderedQty, " +
            "FromOfferQty, IsFromOffer, ExchangeRateAmount, OneTimeDiscount, DiscountAmount, PricePerItemWithoutVat, ReturnedQty, AssignedSpecificationId, " +
            "IsFromReSale, MisplacedSaleId, Updated, Vat, IsFromShiftedItem) " +
            "VALUES(@ClientShoppingCartId, @OrderId, @UserId, @ProductId, @Qty, @OverLordQty, @Comment, @IsValidForCurrentSale, @PricePerItem, @OrderedQty, " +
            "@FromOfferQty, @IsFromOffer, @ExchangeRateAmount, 0.00, 0.00, 0.00, 0, @AssignedSpecificationId, @IsFromReSale, @MisplacedSaleId, getutcdate(), @Vat, @IsFromShiftedItem); " +
            "SELECT SCOPE_IDENTITY()",
            orderItem
        ).Single();
    }

    public long AddOneTimeDiscount(OrderItem orderItem) {
        return _connection.Query<long>(
            "INSERT INTO [OrderItem] (ClientShoppingCartId, OrderId, UserId, ProductId, Qty, Comment, IsValidForCurrentSale, PricePerItem, OrderedQty, " +
            "FromOfferQty, IsFromOffer, ExchangeRateAmount, OneTimeDiscountComment, OneTimeDiscount, DiscountAmount, PricePerItemWithoutVat, ReturnedQty, AssignedSpecificationId, " +
            "IsFromReSale, MisplacedSaleId, Updated, Vat) " +
            "VALUES(@ClientShoppingCartId, @OrderId, @UserId, @ProductId, @Qty, @Comment, @IsValidForCurrentSale, @PricePerItem, @OrderedQty, " +
            "@FromOfferQty, @IsFromOffer, @ExchangeRateAmount, @OneTimeDiscountComment, @OneTimeDiscount,@DiscountAmount, 0.00, 0, @AssignedSpecificationId, @IsFromReSale, @MisplacedSaleId, getutcdate(), @Vat); " +
            "SELECT SCOPE_IDENTITY()",
            orderItem
        ).Single();
    }

    public void Add(IEnumerable<OrderItem> orderItems) {
        _connection.Execute(
            "INSERT INTO [OrderItem] (ClientShoppingCartId, OrderId, UserId, ProductId, Qty, Comment, IsValidForCurrentSale, PricePerItem, ExchangeRateAmount, OneTimeDiscount, " +
            "DiscountAmount, PricePerItemWithoutVat, ReturnedQty, AssignedSpecificationId, MisplacedSaleId, Updated, Vat) " +
            "VALUES (@ClientShoppingCartId, @OrderId, @UserId, @ProductId, @Qty, @Comment, @IsValidForCurrentSale, @PricePerItem, @ExchangeRateAmount, 0.00, 0.00, 0.00, " +
            "0, @AssignedSpecificationId, @MisplacedSaleId, getutcdate(), @Vat)",
            orderItems
        );
    }

    public void Remove(OrderItem orderItem) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE ID = @Id",
            orderItem
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void Remove(IEnumerable<OrderItem> orderItems) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET Deleted = 1 " +
            "WHERE NetUID = @NetUid",
            orderItems
        );
    }

    public void Update(OrderItem orderItem) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET ClientShoppingCartId = @ClientShoppingCartId, InvoiceDocumentQty = @InvoiceDocumentQty , OrderId = @OrderId, UserId = @UserId, ProductId = @ProductId, Qty = @Qty, Comment = @Comment, " +
            "IsValidForCurrentSale = @IsValidForCurrentSale, PricePerItem = @PricePerItem, ExchangeRateAmount = @ExchangeRateAmount, DiscountAmount = @DiscountAmount, " +
            "PricePerItemWithoutVat = @PricePerItemWithoutVat, Updated = getutcdate(), Vat = @Vat, IsClosed = @IsClosed " +
            "WHERE NetUID = @NetUid",
            orderItem
        );
    }

    public void UpdateItemAssignment(OrderItem orderItem) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET ClientShoppingCartId = @ClientShoppingCartId, OrderId = @OrderId, Updated = getutcdate() " +
            "WHERE ID = @Id",
            orderItem
        );
    }

    public void Update(IEnumerable<OrderItem> orderItems) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET ClientShoppingCartId = @ClientShoppingCartId, OrderId = @OrderId, UserId = @UserId, ProductId = @ProductId, Qty = @Qty, Comment = @Comment, " +
            "IsValidForCurrentSale = @IsValidForCurrentSale, PricePerItem = @PricePerItem, ExchangeRateAmount = @ExchangeRateAmount, DiscountAmount = @DiscountAmount, " +
            "PricePerItemWithoutVat = @PricePerItemWithoutVat, Updated = getutcdate(), Vat = @Vat " +
            "WHERE NetUID = @NetUid",
            orderItems
        );
    }

    public void UpdateQty(OrderItem orderItem) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET Qty = @Qty, Updated = getutcdate() " +
            "WHERE [OrderItem].ID = @Id",
            orderItem
        );
    }

    public void UpdateOverLoadQty(OrderItem orderItem) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET OverLordQty = @OverLordQty, Updated = getutcdate() " +
            "WHERE [OrderItem].ID = @Id",
            orderItem
        );
    }

    public void UpdateInvoiceDocumentQty(OrderItem orderItem) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET InvoiceDocumentQty = @InvoiceDocumentQty, Updated = getutcdate() " +
            "WHERE [OrderItem].ID = @Id",
            orderItem
        );
    }

    public void UpdatePricePerItem(OrderItem orderItem) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET PricePerItem = @PricePerItem, Updated = getutcdate() " +
            "WHERE [OrderItem].ID = @Id",
            orderItem
        );
    }

    public void UpdateReturnedQty(OrderItem orderItem) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET ReturnedQty = @ReturnedQty, Updated = getutcdate() " +
            "WHERE [OrderItem].ID = @Id",
            orderItem
        );
    }

    public void UpdateOneTimeDiscount(OrderItem orderItem) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET OneTimeDiscount = @OneTimeDiscount, DiscountUpdatedByID = @DiscountUpdatedById, OneTimeDiscountComment = @OneTimeDiscountComment,  Updated = getutcdate() " +
            "WHERE [OrderItem].ID = @Id",
            orderItem
        );
    }

    public void UpdateOneTimeDiscountComment(OrderItem orderItem) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET OneTimeDiscountComment = @OneTimeDiscountComment " +
            "WHERE [OrderItem].ID = @Id",
            orderItem
        );
    }

    public void SetItemsZeroOffered(IEnumerable<OrderItem> items) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET OrderedQty = 0, OfferProcessingStatus = 0 " +
            "WHERE ID = @Id",
            items
        );
    }

    public void SetOfferProcessingStatuses(IEnumerable<OrderItem> items) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET OrderedQty = @OrderedQty, OfferProcessingStatus = @OfferProcessingStatus, OfferProcessingStatusChangedById = @OfferProcessingStatusChangedById, " +
            "Comment = @Comment " +
            "WHERE ID = @Id",
            items
        );
    }

    public void AssignSpecification(OrderItem orderItem) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET AssignedSpecificationId = @AssignedSpecificationId, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            orderItem
        );
    }

    public OrderItem GetByNetIdWithoutIncludes(Guid netId) {
        return _connection.Query<OrderItem>(
            "SELECT * " +
            "FROM [OrderItem] " +
            "WHERE [OrderItem].NetUID = @NetId",
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public OrderItem GetOrderItemByOrderProductAndSpecificationIfExits(
        long orderId,
        long productId,
        string specificationName,
        string specificationCode,
        string specificationLocale,
        decimal specificationDutyPercent,
        bool isFromReSale) {
        return _connection.Query<OrderItem>(
            "SELECT [OrderItem].* " +
            "FROM [OrderItem] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [OrderItem].AssignedSpecificationID = [ProductSpecification].ID " +
            "WHERE [OrderItem].ProductID = @ProductId " +
            "AND [OrderItem].OrderID = @OrderId " +
            "AND ([ProductSpecification].[Name] = @SpecificationName OR (@SpecificationName IS NULL AND [ProductSpecification].[Name] IS NULL)) " +
            "AND [ProductSpecification].[SpecificationCode] = @SpecificationCode " +
            "AND [ProductSpecification].[Locale] = @SpecificationLocale " +
            "AND [ProductSpecification].[DutyPercent] = @SpecificationDutyPercent " +
            "AND [OrderItem].Deleted = 0 " +
            "AND [OrderItem].IsFromReSale = @IsFromReSale",
            new {
                OrderId = orderId,
                ProductId = productId,
                SpecificationName = specificationName,
                SpecificationCode = specificationCode,
                SpecificationLocale = specificationLocale,
                SpecificationDutyPercent = specificationDutyPercent,
                IsFromReSale = isFromReSale
            }
        ).SingleOrDefault();
    }

    public OrderItem GetOrderItemByOrderProductAndSpecification(
        long orderId,
        long productId,
        ProductSpecification productSpecification,
        bool isFromReSale) {
        if (productSpecification != null)
            return _connection.Query<OrderItem>(
                "SELECT [OrderItem].* " +
                "FROM [OrderItem] " +
                "LEFT JOIN [ProductSpecification] " +
                "ON [OrderItem].AssignedSpecificationID = [ProductSpecification].ID " +
                "WHERE [OrderItem].ProductID = @ProductId " +
                "AND [OrderItem].OrderID = @OrderId " +
                "AND [ProductSpecification].[SpecificationCode] = @SpecificationCode " +
                "AND [ProductSpecification].[Locale] = @SpecificationLocale " +
                "AND [ProductSpecification].[DutyPercent] = @DutyPercent " +
                "AND [ProductSpecification].[VATPercent] = @VATPercent " +
                "AND [OrderItem].Deleted = 0 " +
                "AND [OrderItem].IsFromReSale = @IsFromReSale",
                new {
                    OrderId = orderId,
                    ProductId = productId,
                    productSpecification.SpecificationCode,
                    SpecificationLocale = productSpecification.Locale,
                    productSpecification.DutyPercent,
                    productSpecification.VATPercent,
                    IsFromReSale = isFromReSale
                }
            ).SingleOrDefault();

        return _connection.Query<OrderItem>(
            "SELECT [OrderItem].* " +
            "FROM [OrderItem] " +
            "WHERE [OrderItem].ProductID = @ProductId " +
            "AND [OrderItem].OrderID = @OrderId " +
            "AND [OrderItem].Deleted = 0 " +
            "AND [OrderItem].IsFromReSale = @IsFromReSale",
            new {
                OrderId = orderId,
                ProductId = productId,
                IsFromReSale = isFromReSale
            }
        ).SingleOrDefault();
    }

    public OrderItem GetByNetIdWithProduct(Guid netId) {
        return _connection.Query<OrderItem, Product, OrderItem>(
            "SELECT * " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Product] " +
            "ON [OrderItem].ProductID = [Product].ID " +
            "WHERE [OrderItem].NetUID = @NetId",
            (orderItem, product) => {
                orderItem.Product = product;

                return orderItem;
            },
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public OrderItem GetByNetId(Guid netId) {
        OrderItem orderItemToReturn = null;

        _connection.Query<OrderItem, OrderItemBaseShiftStatus, OrderItem>(
            "SELECT * FROM [OrderItem] " +
            "LEFT JOIN OrderItemBaseShiftStatus " +
            "ON OrderItem.ID = OrderItemBaseShiftStatus.OrderItemID " +
            "WHERE OrderItem.NetUID = @NetId",
            (item, status) => {
                if (orderItemToReturn != null) {
                    if (status != null && !orderItemToReturn.ShiftStatuses.Any(s => s.Id.Equals(status.Id))) orderItemToReturn.ShiftStatuses.Add(status);
                } else {
                    if (status != null) item.ShiftStatuses.Add(status);

                    orderItemToReturn = item;
                }

                return item;
            },
            new { NetId = netId.ToString() }
        );

        return orderItemToReturn;
    }

    public OrderItem GetFilteredByIds(long orderId, long productId, bool reSaleAvailability) {
        return _connection.Query<OrderItem>(
            "SELECT TOP(1) * " +
            "FROM [OrderItem] " +
            "WHERE OrderID = @OrderId " +
            "AND ProductID = @ProductId " +
            "AND Deleted = 0 " +
            "AND IsFromReSale = @ReSaleAvailability",
            new { OrderId = orderId, ProductId = productId, ReSaleAvailability = reSaleAvailability }
        ).SingleOrDefault();
    }

    public OrderItem GetById(long id) {
        return _connection.Query<OrderItem, Product, OrderItem>(
            "SELECT * " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "WHERE [OrderItem].ID = @Id",
            (orderItem, product) => {
                orderItem.Product = product;

                return orderItem;
            },
            new { Id = id }
        ).SingleOrDefault();
    }

    public OrderItem GetByIdWithClientAgreement(long id) {
        return _connection.Query<OrderItem, Product, Order, Sale, ClientAgreement, Agreement, Organization, OrderItem>(
            "SELECT * " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [Sale].ClientAgreementID = [ClientAgreement].ID " +
            "LEFT JOIN [Agreement] " +
            "ON [ClientAgreement].AgreementID = [Agreement].ID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "WHERE [OrderItem].ID = @Id",
            (orderItem, product, order, sale, clientAgreement, agreement, organization) => {
                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                sale.ClientAgreement = clientAgreement;

                order.Sale = sale;

                orderItem.Product = product;
                orderItem.Order = order;

                return orderItem;
            },
            new { Id = id }
        ).SingleOrDefault();
    }

    public IEnumerable<OrderItem> GetByIdsWithClientAgreement(IEnumerable<long> ids) {
        return _connection.Query<OrderItem, Product, Order, Sale, ClientAgreement, Agreement, Organization, OrderItem>(
            "SELECT * " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [Sale].ClientAgreementID = [ClientAgreement].ID " +
            "LEFT JOIN [Agreement] " +
            "ON [ClientAgreement].AgreementID = [Agreement].ID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "WHERE [OrderItem].ID IN @Ids",
            (orderItem, product, order, sale, clientAgreement, agreement, organization) => {
                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                sale.ClientAgreement = clientAgreement;

                order.Sale = sale;

                orderItem.Product = product;
                orderItem.Order = order;

                return orderItem;
            },
            new { Ids = ids }
        ).ToList();
    }

    public OrderItem GetByIdWithClientInfo(long id) {
        return _connection.Query<OrderItem, Order, Sale, ClientAgreement, Agreement, OrderItem>(
            "SELECT * " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "WHERE [OrderItem].ID = @Id",
            (orderItem, order, sale, clientAgreement, agreement) => {
                clientAgreement.Agreement = agreement;

                sale.ClientAgreement = clientAgreement;

                order.Sale = sale;

                orderItem.Order = order;

                return orderItem;
            },
            new { Id = id }
        ).SingleOrDefault();
    }

    public void RestoreUnpackedQtyByTaxFreeItemsIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET [OrderItem].UnpackedQty = [OrderItem].UnpackedQty + [TaxFreeItem].Qty " +
            "FROM [OrderItem] " +
            "LEFT JOIN [TaxFreeItem] " +
            "ON [TaxFreeItem].OrderItemID = [OrderItem].ID " +
            "AND [TaxFreeItem].Deleted = 0 " +
            "WHERE [TaxFreeItem].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RestoreUnpackedQtyByTaxFreeIdExceptProvidedIds(long taxFreeId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET UnpackedQty = UnpackedQty + [TaxFreeItem].Qty, Updated = GETUTCDATE() " +
            "FROM [OrderItem] " +
            "LEFT JOIN [TaxFreeItem] " +
            "ON [TaxFreeItem].OrderItemID = [OrderItem].ID " +
            "WHERE [TaxFreeItem].TaxFreeID = @TaxFreeId " +
            "AND [TaxFreeItem].ID NOT IN @Ids " +
            "AND [TaxFreeItem].Deleted = 0",
            new { TaxFreeId = taxFreeId, Ids = ids }
        );
    }

    public void DecreaseUnpackedQtyById(long id, double qty) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET UnpackedQty = UnpackedQty - @Qty, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            new { Id = id, Qty = qty }
        );
    }

    public void SetUnpackedQtyToAllItemsByOrderId(long orderId) {
        _connection.Execute(
            "UPDATE [OrderItem] " +
            "SET UnpackedQty = Qty " +
            "WHERE [OrderItem].OrderID = @OrderId",
            new { OrderId = orderId }
        );
    }

    public OrderItem GetByIdWithIncludes(long id, Guid clientAgreementNetId) {
        bool withVat = _connection.Query<bool>(
            "SELECT [Agreement].[WithVATAccounting] FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "WHERE [ClientAgreement].[NetUID] = @NetId; ",
            new { NetId = clientAgreementNetId }).FirstOrDefault();

        string sqlExpression =
            "SELECT [OrderItem].* " +
            ", [Product].ID " +
            ", [Product].Created " +
            ", [Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ", [Product].HasAnalogue " +
            ", [Product].HasComponent " +
            ", [Product].HasImage " +
            ", [Product].[Image] " +
            ", [Product].IsForSale " +
            ", [Product].IsForWeb " +
            ", [Product].IsForZeroSale " +
            ", [Product].MainOriginalNumber " +
            ", [Product].MeasureUnitID " +
            ", [Product].NetUID " +
            ", [Product].OrderStandard " +
            ", [Product].PackingStandard " +
            ", [Product].Size " +
            ", [Product].[Top] " +
            ", [Product].UCGFEA " +
            ", [Product].Updated " +
            ", [Product].VendorCode " +
            ", [Product].Volume " +
            ", [Product].[Weight] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'uk' " +
            "AND [Storage].ForVatProducts = 0 " +
            "), 0) AS [AvailableQtyUk] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'uk' " +
            "AND [Storage].ForVatProducts = 1 " +
            "), 0) AS [AvailableQtyUkVAT] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'pl' " +
            "AND [Storage].ForVatProducts = 0 " +
            "), 0) AS [AvailableQtyPl] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'pl' " +
            "AND [Storage].ForVatProducts = 1 " +
            "), 0) AS [AvailableQtyPlVAT] " +
            ", [MeasureUnit].ID " +
            ", [MeasureUnit].Created " +
            ", (CASE WHEN [MeasureUnitTranslation].Name IS NOT NULL THEN [MeasureUnitTranslation].Name ELSE [MeasureUnit].Name END) AS [Name] " +
            ", (CASE WHEN [MeasureUnitTranslation].Description IS NOT NULL THEN [MeasureUnitTranslation].Description ELSE [MeasureUnit].Description END) AS [Description] " +
            ", [MeasureUnit].Created " +
            ", [MeasureUnit].Deleted " +
            ", [MeasureUnit].NetUID " +
            ", [MeasureUnit].Updated " +
            ", [ProductSlug].* " +
            ", (CASE " +
            "WHEN [OrderItem].[IsFromReSale] = 0 " +
            "THEN dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) " +
            "ELSE dbo.GetCalculatedProductPriceWithShares_ReSale([Product].NetUID, @ClientAgreementNetId, @Culture, [OrderItem].[ID]) " +
            "END) AS [ProductCurrentPrice] " +
            ", (CASE " +
            "WHEN [OrderItem].[IsFromReSale] = 0 " +
            "THEN dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) " +
            "ELSE dbo.GetCalculatedProductLocalPriceWithShares_ReSale([Product].NetUID, @ClientAgreementNetId, @Culture, [OrderItem].[ID]) " +
            "END) AS [ProductCurrentLocalPrice] " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Product] " +
            "ON [OrderItem].ProductID = [Product].ID " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "LEFT JOIN [MeasureUnitTranslation] " +
            "ON [MeasureUnitTranslation].MeasureUnitID = [MeasureUnit].ID " +
            "AND [MeasureUnitTranslation].CultureCode = @Culture " +
            "AND [MeasureUnitTranslation].Deleted = 0 " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Product].ID" +
            ") " +
            "WHERE [OrderItem].ID = @Id";

        return _connection.Query<OrderItem, Product, MeasureUnit, ProductSlug, decimal, decimal, OrderItem>(
                sqlExpression,
                (orderItem, product, measureUnit, productSlug, currentPrice, currentLocalPrice) => {
                    product.MeasureUnit = measureUnit;
                    product.ProductSlug = productSlug;
                    product.CurrentPrice = currentPrice;
                    product.CurrentLocalPrice = currentLocalPrice;

                    orderItem.Product = product;

                    return orderItem;
                },
                new { Id = id, ClientAgreementNetId = clientAgreementNetId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, WithVat = withVat },
                splitOn: "ID,ProductCurrentPrice,ProductCurrentLocalPrice")
            .Single();
    }

    public OrderItem GetByIdAndClientAgreementNetIdWithIncludes(long id, Guid clientAgreementNetId, long currencyId) {
        OrderItem orderItemToReturn = null;

        Agreement currentAgreement = _connection.Query<Agreement>(
            "SELECT Agreement.* FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "WHERE [ClientAgreement].[NetUID] = @NetId; ",
            new { NetId = clientAgreementNetId }).FirstOrDefault();

        string sqlExpression =
            "SELECT [OrderItem].* " +
            ", [Product].ID " +
            ", [Product].Created " +
            ", [Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ", [Product].HasAnalogue " +
            ", [Product].HasComponent " +
            ", [Product].HasImage " +
            ", [Product].[Image] " +
            ", [Product].IsForSale " +
            ", [Product].IsForWeb " +
            ", [Product].IsForZeroSale " +
            ", [Product].MainOriginalNumber " +
            ", [Product].MeasureUnitID " +
            ", [Product].NetUID " +
            ", [Product].OrderStandard " +
            ", [Product].PackingStandard " +
            ", [Product].Size " +
            ", [Product].[Top] " +
            ", [Product].UCGFEA " +
            ", [Product].Updated " +
            ", [Product].VendorCode " +
            ", [Product].Volume " +
            ", [Product].[Weight] " +
            ", ( ";
        if (currentAgreement?.WithVATAccounting ?? false)
            sqlExpression +=
                "SELECT SUM([ProductAvailability].Amount) " +
                "FROM [ProductAvailability] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductAvailability].StorageID " +
                "WHERE [ProductAvailability].ProductID = [Product].ID " +
                "AND [ProductAvailability].Deleted = 0 " +
                "AND [Storage].ForDefective = 0 " +
                "AND [Storage].Locale = N'uk' " +
                "AND [Storage].OrganizationID = @OrganizationId " +
                ") AS [AvailableQtyUk] ";
        else
            sqlExpression +=
                "SELECT SUM(AmountQty) AS [AvailableQtyUk] " +
                "FROM ( " +
                "SELECT SUM([ProductAvailability].Amount) AS AmountQty FROM [ProductAvailability] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductAvailability].StorageID " +
                "WHERE [ProductAvailability].ProductID = [Product].ID  " +
                "AND [ProductAvailability].Deleted = 0 " +
                "AND [Storage].ForDefective = 0 " +
                "AND [Storage].Locale = N'uk' " +
                "AND [Storage].AvailableForReSale = 1 " +
                "UNION " +
                "SELECT SUM([ProductAvailability].Amount) AS AmountQty FROM [ProductAvailability] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductAvailability].StorageID " +
                "WHERE [ProductAvailability].ProductID = [Product].ID " +
                "AND [ProductAvailability].Deleted = 0 " +
                "AND [Storage].ForDefective = 0 " +
                "AND [Storage].Locale = N'uk' " +
                "AND [Storage].OrganizationID = @OrganizationId " +
                ") AS AmountQty) AS [AvailableQtyUk]";

        sqlExpression +=
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'uk' " +
            "AND [Storage].ForVatProducts = 1 " +
            "), 0) AS [AvailableQtyUkVAT] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'pl' " +
            "AND [Storage].ForVatProducts = 0 " +
            "), 0) AS [AvailableQtyPl] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'pl' " +
            "AND [Storage].ForVatProducts = 1 " +
            "), 0) AS [AvailableQtyPlVAT] " +
            ", [MeasureUnit].ID " +
            ", [MeasureUnit].Created " +
            ", (CASE WHEN [MeasureUnitTranslation].Name IS NOT NULL THEN [MeasureUnitTranslation].Name ELSE [MeasureUnit].Name END) AS [Name] " +
            ", (CASE WHEN [MeasureUnitTranslation].Description IS NOT NULL THEN [MeasureUnitTranslation].Description ELSE [MeasureUnit].Description END) AS [Description] " +
            ", [MeasureUnit].Created " +
            ", [MeasureUnit].Deleted " +
            ", [MeasureUnit].NetUID " +
            ", [MeasureUnit].Updated " +
            ", [ProductSlug].* " +
            ", (CASE " +
            "WHEN [OrderItem].[IsFromReSale] = 0 " +
            "THEN dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, [OrderItem].ID) " +
            "ELSE dbo.GetCalculatedProductPriceWithShares_ReSale([Product].NetUID, @ClientAgreementNetId, @Culture, [OrderItem].[ID]) " +
            "END) AS [CurrentPrice] " +
            ",dbo.[GetCurrentEuroExchangeRateFiltered]([Product].NetUID, @CurrencyId, @WithVat, 0) AS LocalExchangeRate " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Product] " +
            "ON [OrderItem].ProductID = [Product].ID " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "LEFT JOIN [MeasureUnitTranslation] " +
            "ON [MeasureUnitTranslation].MeasureUnitID = [MeasureUnit].ID " +
            "AND [MeasureUnitTranslation].CultureCode = @Culture " +
            "AND [MeasureUnitTranslation].Deleted = 0 " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = ( " +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Product].ID " +
            ") " +
            "WHERE [OrderItem].ID = @Id ";


        _connection.Query<OrderItem, Product, MeasureUnit, ProductSlug, decimal, decimal, OrderItem>(
            sqlExpression,
            (orderItem, product, measureUnit, productSlug, currentPrice, localExchangeRate) => {
                product.MeasureUnit = measureUnit;
                product.ProductSlug = productSlug;
                product.CurrentPrice = currentPrice;
                product.CurrentLocalPrice = decimal.Round(product.CurrentPrice * localExchangeRate, 2, MidpointRounding.AwayFromZero);

                orderItem.Product = product;

                orderItemToReturn = orderItem;

                return orderItem;
            },
            new {
                Id = id,
                ClientAgreementNetId = clientAgreementNetId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                WithVat = currentAgreement?.WithVATAccounting,
                currentAgreement?.OrganizationId,
                CurrencyId = currencyId
            },
            splitOn: "ID,CurrentPrice,LocalExchangeRate"
        );

        return orderItemToReturn;
    }

    public OrderItem GetByIdWithIncludes(long id, Guid? clientAgreementNetId, Guid? vatAgreementNetId) {
        bool withVat = clientAgreementNetId.HasValue && _connection.Query<bool>(
            "SELECT [Agreement].[WithVATAccounting] FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "WHERE [ClientAgreement].[NetUID] = @NetId; ",
            new { NetId = clientAgreementNetId.Value }).FirstOrDefault();

        string sqlExpression =
            "SELECT [OrderItem].* " +
            ", [Product].ID " +
            ", [Product].Created " +
            ", [Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ", [Product].HasAnalogue " +
            ", [Product].HasComponent " +
            ", [Product].HasImage " +
            ", [Product].[Image] " +
            ", [Product].IsForSale " +
            ", [Product].IsForWeb " +
            ", [Product].IsForZeroSale " +
            ", [Product].MainOriginalNumber " +
            ", [Product].MeasureUnitID " +
            ", [Product].NetUID " +
            ", [Product].OrderStandard " +
            ", [Product].PackingStandard " +
            ", [Product].Size " +
            ", [Product].[Top] " +
            ", [Product].UCGFEA " +
            ", [Product].Updated " +
            ", [Product].VendorCode " +
            ", [Product].Volume " +
            ", [Product].[Weight] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'uk' " +
            "AND [Storage].ForVatProducts = 0 " +
            "), 0) AS [AvailableQtyUk] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'uk' " +
            "AND [Storage].ForVatProducts = 1 " +
            "), 0) AS [AvailableQtyUkVAT] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'pl' " +
            "AND [Storage].ForVatProducts = 0 " +
            "), 0) AS [AvailableQtyPl] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'pl' " +
            "AND [Storage].ForVatProducts = 1 " +
            "), 0) AS [AvailableQtyPlVAT] " +
            (
                clientAgreementNetId.HasValue
                    ? ", (CASE " +
                      "WHEN [OrderItem].[IsFromReSale] = 0 " +
                      "THEN dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) " +
                      "ELSE dbo.GetCalculatedProductPriceWithShares_ReSale([Product].NetUID, @ClientAgreementNetId, @Culture, [OrderItem].[ID]) " +
                      "END) AS [CurrentPrice] " +
                      ", (CASE " +
                      "WHEN [OrderItem].[IsFromReSale] = 0 " +
                      "THEN dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) " +
                      "ELSE dbo.GetCalculatedProductLocalPriceWithShares_ReSale([Product].NetUID, @ClientAgreementNetId, @Culture, [OrderItem].[ID]) " +
                      "END) AS [CurrentLocalPrice] "
                    : string.Empty
            ) +
            (
                vatAgreementNetId.HasValue
                    ? ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) AS CurrentWithVatPrice " +
                      ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) AS CurrentLocalWithVatPrice "
                    : string.Empty
            ) +
            ", [MeasureUnit].ID " +
            ", [MeasureUnit].Created " +
            ", (CASE WHEN [MeasureUnitTranslation].Name IS NOT NULL THEN [MeasureUnitTranslation].Name ELSE [MeasureUnit].Name END) AS [Name] " +
            ", (CASE WHEN [MeasureUnitTranslation].Description IS NOT NULL THEN [MeasureUnitTranslation].Description ELSE [MeasureUnit].Description END) AS [Description] " +
            ", [MeasureUnit].Created " +
            ", [MeasureUnit].Deleted " +
            ", [MeasureUnit].NetUID " +
            ", [MeasureUnit].Updated " +
            ", [ProductSlug].* " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Product] " +
            "ON [OrderItem].ProductID = [Product].ID " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "LEFT JOIN [MeasureUnitTranslation] " +
            "ON [MeasureUnitTranslation].MeasureUnitID = [MeasureUnit].ID " +
            "AND [MeasureUnitTranslation].CultureCode = @Culture " +
            "AND [MeasureUnitTranslation].Deleted = 0 " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Product].ID" +
            ") " +
            "WHERE [OrderItem].ID = @Id";

        return _connection.Query<OrderItem, Product, MeasureUnit, ProductSlug, OrderItem>(
            sqlExpression,
            (orderItem, product, measureUnit, productSlug) => {
                product.MeasureUnit = measureUnit;
                product.ProductSlug = productSlug;

                orderItem.Product = product;

                return orderItem;
            },
            new {
                Id = id,
                ClientAgreementNetId = clientAgreementNetId,
                VatAgreementNetId = vatAgreementNetId ?? Guid.Empty,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                WithVat = withVat
            }
        ).Single();
    }

    public IEnumerable<OrderItem> GetAllFromCurrentShoppingByClientNetId(long? workplaceId, Guid clientAgreementNetId, long? currencyId, long? organizationId, bool withVat) {
        List<OrderItem> items = new();

        string sqlExpression =
            "SELECT [OrderItem].* " +
            ", [Product].ID " +
            ", [Product].Created " +
            ", [Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ", [Product].HasAnalogue " +
            ", [Product].HasComponent " +
            ", [Product].HasImage " +
            ", [Product].[Image] " +
            ", [Product].IsForSale " +
            ", [Product].IsForWeb " +
            ", [Product].IsForZeroSale " +
            ", [Product].MainOriginalNumber " +
            ", [Product].MeasureUnitID " +
            ", [Product].NetUID " +
            ", [Product].OrderStandard " +
            ", [Product].PackingStandard " +
            ", [Product].Size " +
            ", [Product].[Top] " +
            ", [Product].UCGFEA " +
            ", [Product].Updated " +
            ", [Product].VendorCode " +
            ", [Product].Volume " +
            ", [Product].[Weight] " +
            ", (CASE " +
            "WHEN [OrderItem].[IsFromReSale] = 0 " +
            "THEN dbo.[GetDefaultCalculatedProductPriceWithSharesAndVat]([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) " +
            "ELSE dbo.GetCalculatedProductPriceWithShares_ReSale([Product].NetUID, @ClientAgreementNetId, @Culture, [OrderItem].[ID]) " +
            "END) AS [CurrentPrice] " +
            ", (CASE " +
            "WHEN [OrderItem].[IsFromReSale] = 0 " +
            "THEN dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) " +
            "ELSE dbo.GetCalculatedProductLocalPriceWithShares_ReSale([Product].NetUID, @ClientAgreementNetId, @Culture, [OrderItem].[ID]) " +
            "END) AS [CurrentLocalPrice] " +
            ", (SELECT Code FROM Currency " +
            "WHERE ID = @CurrencyId " +
            "AND Deleted = 0) AS CurrencyCode " +
            ", ( ";
        if (withVat)
            sqlExpression +=
                "SELECT SUM([ProductAvailability].Amount) " +
                "FROM [ProductAvailability] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductAvailability].StorageID " +
                "WHERE [ProductAvailability].ProductID = [Product].ID " +
                "AND [ProductAvailability].Deleted = 0 " +
                "AND [Storage].ForDefective = 0 " +
                "AND [Storage].Locale = N'uk' " +
                "AND [Storage].OrganizationID = @OrganizationId " +
                ") AS [AvailableQtyUk] ";
        else
            sqlExpression +=
                "SELECT SUM(AmountQty) AS [AvailableQtyUk] " +
                "FROM ( " +
                "SELECT SUM([ProductAvailability].Amount) AS AmountQty FROM [ProductAvailability] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductAvailability].StorageID " +
                "WHERE [ProductAvailability].ProductID = [Product].ID  " +
                "AND [ProductAvailability].Deleted = 0 " +
                "AND [Storage].ForDefective = 0 " +
                "AND [Storage].Locale = N'uk' " +
                "AND [Storage].AvailableForReSale = 1 " +
                "UNION " +
                "SELECT SUM([ProductAvailability].Amount) AS AmountQty FROM [ProductAvailability] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductAvailability].StorageID " +
                "WHERE [ProductAvailability].ProductID = [Product].ID " +
                "AND [ProductAvailability].Deleted = 0 " +
                "AND [Storage].ForDefective = 0 " +
                "AND [Storage].Locale = N'uk' " +
                "AND [Storage].OrganizationID = @OrganizationId " +
                ") AS AmountQty) AS [AvailableQtyUk] ";

        sqlExpression +=
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'uk' " +
            "AND [Storage].ForVatProducts = 1 " +
            "), 0) AS [AvailableQtyUkVAT] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'pl' " +
            "AND [Storage].ForVatProducts = 0 " +
            "), 0) AS [AvailableQtyPl] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'pl' " +
            "AND [Storage].ForVatProducts = 1 " +
            "), 0) AS [AvailableQtyPlVAT] " +
            ", [MeasureUnit].ID " +
            ", [MeasureUnit].Created " +
            ", (CASE WHEN [MeasureUnitTranslation].Name IS NOT NULL THEN [MeasureUnitTranslation].Name ELSE [MeasureUnit].Name END) AS [Name] " +
            ", (CASE WHEN [MeasureUnitTranslation].Description IS NOT NULL THEN [MeasureUnitTranslation].Description ELSE [MeasureUnit].Description END) AS [Description] " +
            ", [MeasureUnit].Created " +
            ", [MeasureUnit].Deleted " +
            ", [MeasureUnit].NetUID " +
            ", [MeasureUnit].Updated " +
            ", [ProductSlug].* " +
            ", [ProductImage].* " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Product] " +
            "ON [OrderItem].ProductID = [Product].ID " +
            "LEFT JOIN [ProductImage] " +
            "ON [ProductImage].ProductID = [Product].ID " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "LEFT JOIN [MeasureUnitTranslation] " +
            "ON [MeasureUnitTranslation].MeasureUnitID = [MeasureUnit].ID " +
            "AND [MeasureUnitTranslation].CultureCode = @Culture " +
            "AND [MeasureUnitTranslation].Deleted = 0 " +
            "LEFT JOIN [ClientShoppingCart] " +
            "ON [ClientShoppingCart].ID = [OrderItem].ClientShoppingCartID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [ClientShoppingCart].ClientAgreementID " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Product].ID" +
            ") " +
            "WHERE [OrderItem].Deleted = 0 " +
            "AND [ClientShoppingCart].Deleted = 0 " +
            "AND [ClientShoppingCart].IsOffer = 0 " +
            "AND [ClientShoppingCart].IsVatCart = @WithVat ";
        sqlExpression += workplaceId == null
            ? "AND [ClientShoppingCart].WorkplaceID IS NULL "
            : "AND [ClientShoppingCart].WorkplaceID = @WorkplaceId ";
        sqlExpression +=
            "AND [ClientAgreement].NetUID = @ClientAgreementNetId";

        Type[] types = {
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductSlug),
            typeof(ProductImage)
        };

        Func<object[], OrderItem> orderItemMapper = objects => {
            OrderItem orderItem = (OrderItem)objects[0];
            Product product = (Product)objects[1];
            MeasureUnit measureUnit = (MeasureUnit)objects[2];
            ProductSlug productSlug = (ProductSlug)objects[3];
            ProductImage productImage = (ProductImage)objects[4];

            if (items.Any(i => i.Id.Equals(orderItem.Id))) return orderItem;

            product.MeasureUnit = measureUnit;
            product.ProductSlug = productSlug;

            if (productImage != null && !productImage.Deleted) product.Image = productImage.ImageUrl;

            orderItem.Product = product;

            items.Add(orderItem);

            return orderItem;
        };

        _connection.Query(
            sqlExpression,
            types,
            orderItemMapper,
            new {
                WorkplaceId = workplaceId,
                ClientAgreementNetId = clientAgreementNetId,
                WithVat = withVat,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                CurrencyId = currencyId,
                OrganizationId = organizationId
            });

        return items;
    }

    public IEnumerable<OrderItem> GetAllFromCurrentShoppingByClientNetId(Guid clientNetId, Guid? clientAgreementNetId, Guid? vatAgreementNetId, bool withVat) {
        string sqlExpression =
            "SELECT [OrderItem].* " +
            ", [Product].ID " +
            ", [Product].Created " +
            ", [Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ", [Product].HasAnalogue " +
            ", [Product].HasComponent " +
            ", [Product].HasImage " +
            ", [Product].[Image] " +
            ", [Product].IsForSale " +
            ", [Product].IsForWeb " +
            ", [Product].IsForZeroSale " +
            ", [Product].MainOriginalNumber " +
            ", [Product].MeasureUnitID " +
            ", [Product].NetUID " +
            ", [Product].OrderStandard " +
            ", [Product].PackingStandard " +
            ", [Product].Size " +
            ", [Product].[Top] " +
            ", [Product].UCGFEA " +
            ", [Product].Updated " +
            ", [Product].VendorCode " +
            ", [Product].Volume " +
            ", [Product].[Weight] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'uk' " +
            "AND [Storage].ForVatProducts = 0 " +
            "), 0) AS [AvailableQtyUk] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'uk' " +
            "AND [Storage].ForVatProducts = 1 " +
            "), 0) AS [AvailableQtyUkVAT] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'pl' " +
            "AND [Storage].ForVatProducts = 0 " +
            "), 0) AS [AvailableQtyPl] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'pl' " +
            "AND [Storage].ForVatProducts = 1 " +
            "), 0) AS [AvailableQtyPlVAT] " +
            (
                clientAgreementNetId.HasValue
                    ? ", (CASE " +
                      "WHEN [OrderItem].[IsFromReSale] = 0 " +
                      "THEN dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) " +
                      "ELSE dbo.GetCalculatedProductPriceWithShares_ReSale([Product].NetUID, @ClientAgreementNetId, @Culture, [OrderItem].[ID]) " +
                      "END) AS [CurrentPrice] " +
                      ", (CASE " +
                      "WHEN [OrderItem].[IsFromReSale] = 0 " +
                      "THEN dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) " +
                      "ELSE dbo.GetCalculatedProductLocalPriceWithShares_ReSale([Product].NetUID, @ClientAgreementNetId, @Culture, [OrderItem].[ID]) " +
                      "END) AS [CurrentLocalPrice] "
                    : string.Empty
            ) +
            (
                vatAgreementNetId.HasValue
                    ? ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) AS CurrentWithVatPrice " +
                      ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) AS CurrentLocalWithVatPrice "
                    : string.Empty
            ) +
            ", [MeasureUnit].ID " +
            ", [MeasureUnit].Created " +
            ", (CASE WHEN [MeasureUnitTranslation].Name IS NOT NULL THEN [MeasureUnitTranslation].Name ELSE [MeasureUnit].Name END) AS [Name] " +
            ", (CASE WHEN [MeasureUnitTranslation].Description IS NOT NULL THEN [MeasureUnitTranslation].Description ELSE [MeasureUnit].Description END) AS [Description] " +
            ", [MeasureUnit].Created " +
            ", [MeasureUnit].Deleted " +
            ", [MeasureUnit].NetUID " +
            ", [MeasureUnit].Updated " +
            ", [ProductSlug].* " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Product] " +
            "ON [OrderItem].ProductID = [Product].ID " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "LEFT JOIN [MeasureUnitTranslation] " +
            "ON [MeasureUnitTranslation].MeasureUnitID = [MeasureUnit].ID " +
            "AND [MeasureUnitTranslation].CultureCode = @Culture " +
            "AND [MeasureUnitTranslation].Deleted = 0 " +
            "LEFT JOIN [ClientShoppingCart] " +
            "ON [ClientShoppingCart].ID = [OrderItem].ClientShoppingCartID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [ClientShoppingCart].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Product].ID" +
            ") " +
            "WHERE [OrderItem].Deleted = 0 " +
            "AND [ClientShoppingCart].Deleted = 0 " +
            "AND [ClientShoppingCart].IsOffer = 0 " +
            "AND [ClientShoppingCart].IsVatCart = @WithVat " +
            "AND [Client].NetUID = @NetId";

        return _connection.Query<OrderItem, Product, MeasureUnit, ProductSlug, OrderItem>(
            sqlExpression,
            (orderItem, product, measureUnit, productSlug) => {
                product.MeasureUnit = measureUnit;
                product.ProductSlug = productSlug;

                orderItem.Product = product;

                return orderItem;
            },
            new {
                NetId = clientNetId,
                ClientAgreementNetId = clientAgreementNetId ?? Guid.Empty,
                VatAgreementNetId = vatAgreementNetId ?? Guid.Empty,
                WithVat = withVat,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );
    }

    public List<OrderItem> GetAllBySaleNetIdWithProductLocation(Guid netId) {
        List<OrderItem> orderItems = new();

        _connection.Query<OrderItem, Product, ProductLocation, Storage, ProductPlacement, OrderItem>(
            "SELECT * " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [ProductLocation] " +
            "ON [ProductLocation].OrderItemID = [OrderItem].ID " +
            "AND [ProductLocation].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductLocation].StorageID " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ID = [ProductLocation].ProductPlacementID " +
            "WHERE [OrderItem].ID IN (" +
            "SELECT [OrderItem].ID " +
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "WHERE [Sale].NetUID = @NetId " +
            "AND [OrderItem].Deleted = 0 " +
            "AND [OrderItem].Qty <> 0" +
            ")",
            (orderItem, product, productLocation, storage, productPlacement) => {
                if (orderItems.Any(i => i.Id.Equals(orderItem.Id))) {
                    if (productLocation == null) return orderItem;

                    productLocation.Storage = storage;
                    productLocation.ProductPlacement = productPlacement;

                    orderItems.First(i => i.Id.Equals(orderItem.Id)).ProductLocations.Add(productLocation);
                } else {
                    if (productLocation != null) {
                        productLocation.Storage = storage;
                        productLocation.ProductPlacement = productPlacement;

                        orderItem.ProductLocations.Add(productLocation);
                    }

                    orderItem.Product = product;

                    orderItems.Add(orderItem);
                }

                return orderItem;
            },
            new { NetId = netId }
        );

        return orderItems;
    }

    public List<OrderItem> GetAllWithConsignmentMovementBySaleId(long saleId) {
        List<OrderItem> items = new();

        _connection.Query<OrderItem, ConsignmentItemMovement, ConsignmentItem, OrderItem>(
            "SELECT * " +
            "FROM [OrderItem] " +
            "LEFT JOIN [ConsignmentItemMovement] " +
            "ON [ConsignmentItemMovement].OrderItemID = [OrderItem].ID " +
            "AND [ConsignmentItemMovement].Deleted = 0 " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [ConsignmentItemMovement].ConsignmentItemID " +
            "WHERE [OrderItem].ID IN (" +
            "SELECT [OrderItem].ID " +
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "WHERE [Sale].ID = @Id " +
            "AND [OrderItem].Deleted = 0 " +
            "AND [OrderItem].Qty <> 0" +
            ")",
            (orderItem, consignmentItemMovement, consignmentItem) => {
                if (items.Any(i => i.Id.Equals(orderItem.Id)))
                    orderItem = items.First(i => i.Id.Equals(orderItem.Id));
                else
                    items.Add(orderItem);

                if (consignmentItemMovement == null) return orderItem;

                consignmentItemMovement.ConsignmentItem = consignmentItem;

                orderItem.ConsignmentItemMovements.Add(consignmentItemMovement);

                return orderItem;
            },
            new { SaleId = saleId }
        );

        return items;
    }

    public List<OrderItem> GetAllWithProductMovementsBySaleId(long id) {
        List<OrderItem> items = new();

        _connection.Query<OrderItem, Product, OrderItem>(
            "SELECT [OrderItem].* " +
            ", [Product].* " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "WHERE [OrderItem].ID IN (" +
            "SELECT [OrderItem].ID " +
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "WHERE [Sale].ID = @Id " +
            "AND [OrderItem].Deleted = 0 " +
            //"AND [OrderItem].Qty <> 0" +
            ")",
            (orderItem, product) => {
                if (items.Any(i => i.Id.Equals(orderItem.Id))) return orderItem;

                orderItem.Product = product;

                items.Add(orderItem);

                return orderItem;
            },
            new { Id = id }
        );

        return items;
    }

    public OrderItem GetBySaleNetIdAndOrderItemId(Guid saleNetId, long orderItemId) {
        OrderItem orderItemToReturn = null;

        string sqlExpression =
            "WITH ProductPricing_CTE (ID,PricingID, ProductID, Price) " +
            "AS " +
            "( " +
            "SELECT " +
            "ROW_NUMBER() OVER (ORDER BY Pricing.ID) AS ID, " +
            "Pricing.ID, " +
            "Product.ID, " +
            "ROUND((ProductPricing.Price + (ProductPricing.Price * ISNULL(dbo.GetPricingExtraCharge(Pricing.NetUID), 0)/100)) - ((ProductPricing.Price + (ProductPricing.Price * ISNULL(dbo.GetPricingExtraCharge(Pricing.NetUID), 0)/100)) * ISNULL(ProductGroupDiscount.DiscountRate, 0))/100, 2) " +
            "AS Price " +
            "FROM Sale " +
            "LEFT OUTER JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT OUTER JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT OUTER JOIN [Order] " +
            "ON Sale.OrderID = [Order].ID AND [Order].Deleted = 0 " +
            "LEFT JOIN [OrderItem] " +
            "ON OrderItem.ID = ( " +
            "SELECT ID FROM [OrderItem] " +
            "WHERE OrderItem.ID = @OrderItemId " +
            ") " +
            "LEFT OUTER JOIN Product " +
            "ON Product.ID = OrderItem.ProductID AND Product.Deleted = 0 " +
            "LEFT OUTER JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
            "LEFT OUTER JOIN ProductGroupDiscount " +
            "ON ProductGroupDiscount.ID = ( " +
            "SELECT TOP(1) ID FROM ProductGroupDiscount " +
            "WHERE ClientAgreementID = ClientAgreement.ID " +
            "AND ProductGroupId = ProductProductGroup.ProductGroupId " +
            "AND IsActive = 1 " +
            ") " +
            "LEFT OUTER JOIN Pricing " +
            "ON Pricing.ID = Agreement.PricingID " +
            "LEFT OUTER JOIN ProductPricing " +
            "ON ProductPricing.ID = (SELECT TOP(1) ID FROM ProductPricing WHERE ProductID = Product.ID) " +
            "WHERE Sale.NetUID = @SaleNetId " +
            ") " +
            "SELECT * FROM [OrderItem] " +
            "LEFT OUTER JOIN Product " +
            "ON Product.ID = OrderItem.ProductID AND Product.Deleted = 0 " +
            "LEFT OUTER JOIN ProductPricing_CTE " +
            "ON ProductPricing_CTE.ProductID = Product.ID " +
            "LEFT OUTER JOIN Pricing " +
            "ON ProductPricing_CTE.PricingID = Pricing.ID AND Pricing.Deleted = 0 " +
            "LEFT OUTER JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
            "LEFT OUTER JOIN ProductGroup " +
            "ON ProductProductGroup.ProductGroupID = ProductGroup.ID AND ProductGroup.Deleted = 0 " +
            "LEFT OUTER JOIN [User] " +
            "ON [User].ID = OrderItem.UserID AND [User].Deleted = 0 " +
            "LEFT OUTER JOIN ProductAvailability " +
            "ON ProductAvailability.ProductId = Product.Id AND ProductAvailability.Deleted = 0 " +
            "LEFT OUTER JOIN Storage AS ProductAvailabilityStorage " +
            "ON ProductAvailabilityStorage.Id = ProductAvailability.StorageId AND ProductAvailabilityStorage.Deleted = 0 " +
            "WHERE OrderItem.ID = @OrderItemId";

        Type[] types = {
            typeof(OrderItem),
            typeof(Product),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(User),
            typeof(ProductAvailability),
            typeof(Storage)
        };

        Func<object[], OrderItem> mapper = objects => {
            OrderItem orderItem = (OrderItem)objects[0];
            Product product = (Product)objects[1];
            ProductPricing productPricing = (ProductPricing)objects[2];
            Pricing pricing = (Pricing)objects[3];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[4];
            User user = (User)objects[6];
            ProductAvailability productAvailability = (ProductAvailability)objects[7];
            Storage productAvailabilityStorage = (Storage)objects[8];

            if (productAvailability != null && productAvailabilityStorage != null) {
                productAvailability.Storage = productAvailabilityStorage;
                product.ProductAvailabilities.Add(productAvailability);
            }

            if (product != null) {
                if (productPricing != null) {
                    if (pricing != null) productPricing.Pricing = pricing;

                    product.CurrentPrice = productPricing.Price;

                    if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                    orderItem.TotalAmount = productPricing.Price * Convert.ToDecimal(orderItem.Qty);
                    orderItem.User = user;
                    product.ProductPricings.Add(productPricing);
                }

                if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                orderItem.Product = product;
            }

            if (orderItemToReturn != null) {
                if (productPricing != null && !orderItemToReturn.Product.ProductPricings.Any(p => p.Id.Equals(productPricing.Id)))
                    orderItemToReturn.Product.ProductPricings.Add(productPricing);

                if (productProductGroup != null && !orderItemToReturn.Product.ProductProductGroups.Any(p => p.Id.Equals(productProductGroup.Id)))
                    orderItemToReturn.Product.ProductProductGroups.Add(productProductGroup);

                if (productAvailability != null && productAvailabilityStorage != null &&
                    !orderItemToReturn.Product.ProductAvailabilities.Any(p => p.Id.Equals(productAvailability.Id)))
                    orderItemToReturn.Product.ProductAvailabilities.Add(productAvailability);
            } else {
                orderItemToReturn = orderItem;
            }

            return orderItem;
        };

        var props = new { SaleNetId = saleNetId, OrderItemId = orderItemId };

        _connection.Query(sqlExpression, types, mapper, props);

        return orderItemToReturn;
    }

    public OrderItem GetWithCalculatedProductPrices(Guid netId, Guid clientAgreementNetId, long organizationId, bool vatSale, bool reSale) {
        OrderItem toReturn = null;

        string sqlExpression =
            "SELECT [OrderItem].* " +
            ",[Product].ID " +
            ",[Product].Deleted " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].Created " +
            ",[Product].Image " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ",[Product].NetUID " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].Weight " +
            (
                reSale
                    ? ",dbo.[GetCalculatedProductPriceWithShares_ReSale]([Product].NetUID, @ClientAgreementNetId, @Culture, [OrderItem].[ID]) AS [CurrentPrice] " +
                      ",dbo.[GetCalculatedProductLocalPriceWithShares_ReSale]([Product].NetUID, @ClientAgreementNetId, @Culture, [OrderItem].[ID]) AS [CurrentLocalPrice] "
                    : ",dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) AS [CurrentPrice] " +
                      ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) AS [CurrentLocalPrice] "
            ) +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[User].* " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Product] " +
            "ON [OrderItem].ProductID = [Product].ID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OrderItem].UserID " +
            "WHERE [OrderItem].NetUID = @NetId";

        _connection.Query<OrderItem, Product, ProductAvailability, Storage, User, OrderItem>(
            sqlExpression,
            (item, product, availability, storage, user) => {
                if (toReturn != null) {
                    if (availability == null || toReturn.Product.ProductAvailabilities.Any(a => a.Id.Equals(availability.Id))) return item;

                    availability.Storage = storage;

                    toReturn.Product.ProductAvailabilities.Add(availability);
                } else {
                    if (availability != null) {
                        availability.Storage = storage;

                        product.ProductAvailabilities.Add(availability);
                    }

                    item.Product = product;
                    item.User = user;

                    toReturn = item;
                }

                return item;
            },
            new {
                NetId = netId,
                OrganizationId = organizationId,
                ClientAgreementNetId = clientAgreementNetId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                WithVat = vatSale
            }
        );

        return toReturn;
    }

    public OrderItem GetWithCalculatedProductPrices(long id, Guid clientAgreementNetId, long organizationId, bool vatSale, bool reSale) {
        OrderItem toReturn = null;

        string sqlExpression =
            "SELECT [OrderItem].* " +
            ",[Product].ID " +
            ",[Product].Deleted " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].Created " +
            ",[Product].Image " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ",[Product].NetUID " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].Weight " +
            (
                reSale
                    ? ",dbo.GetCalculatedProductPriceWithShares_ReSale([Product].NetUID, @ClientAgreementNetId, @Culture, [OrderItem].[ID]) AS [CurrentPrice] " +
                      ",dbo.GetCalculatedProductLocalPriceWithShares_ReSale([Product].NetUID, @ClientAgreementNetId, @Culture, [OrderItem].[ID]) AS [CurrentLocalPrice] "
                    : ",dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) AS [CurrentPrice] " +
                      ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) AS [CurrentLocalPrice] "
            ) +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[User].* " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Product] " +
            "ON [OrderItem].ProductID = [Product].ID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [OrderItem].UserID " +
            "WHERE [OrderItem].ID = @Id";

        _connection.Query<OrderItem, Product, ProductAvailability, Storage, User, OrderItem>(
            sqlExpression,
            (item, product, availability, storage, user) => {
                if (toReturn != null) {
                    if (availability == null || toReturn.Product.ProductAvailabilities.Any(a => a.Id.Equals(availability.Id))) return item;

                    availability.Storage = storage;

                    toReturn.Product.ProductAvailabilities.Add(availability);
                } else {
                    if (availability != null) {
                        availability.Storage = storage;

                        product.ProductAvailabilities.Add(availability);
                    }

                    item.Product = product;
                    item.User = user;

                    toReturn = item;
                }

                return item;
            },
            new {
                Id = id,
                OrganizationId = organizationId,
                ClientAgreementNetId = clientAgreementNetId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                WithVat = vatSale
            }
        );

        return toReturn;
    }

    public OrderItem GetBySaleNetIdAndOrderItemId(Guid saleNetId, long orderItemId, long organizationId) {
        OrderItem orderItemToReturn = null;

        string sqlExpression =
            "WITH ProductPricing_CTE (ID,PricingID, ProductID, Price) " +
            "AS " +
            "( " +
            "SELECT " +
            "ROW_NUMBER() OVER (ORDER BY Pricing.ID) AS ID, " +
            "Pricing.ID, " +
            "Product.ID, " +
            "ROUND((ProductPricing.Price + (ProductPricing.Price * ISNULL(dbo.GetPricingExtraCharge(Pricing.NetUID), 0)/100)) - ((ProductPricing.Price + (ProductPricing.Price * ISNULL(dbo.GetPricingExtraCharge(Pricing.NetUID), 0)/100)) * ISNULL(ProductGroupDiscount.DiscountRate, 0))/100, 14) " +
            "AS Price " +
            "FROM Sale " +
            "LEFT OUTER JOIN ClientAgreement " +
            "ON ClientAgreement.ID = Sale.ClientAgreementID " +
            "LEFT OUTER JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT OUTER JOIN [Order] " +
            "ON Sale.OrderID = [Order].ID AND [Order].Deleted = 0 " +
            "LEFT JOIN [OrderItem] " +
            "ON OrderItem.ID = ( " +
            "SELECT ID FROM [OrderItem] " +
            "WHERE OrderItem.ID = @OrderItemId " +
            ") " +
            "LEFT OUTER JOIN Product " +
            "ON Product.ID = OrderItem.ProductID AND Product.Deleted = 0 " +
            "LEFT OUTER JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
            "LEFT OUTER JOIN ProductGroupDiscount " +
            "ON ProductGroupDiscount.ID = ( " +
            "SELECT TOP(1) ID FROM ProductGroupDiscount " +
            "WHERE ClientAgreementID = ClientAgreement.ID " +
            "AND ProductGroupId = ProductProductGroup.ProductGroupId " +
            "AND IsActive = 1 " +
            ") " +
            "LEFT OUTER JOIN Pricing " +
            "ON Pricing.ID = Agreement.PricingID " +
            "LEFT OUTER JOIN ProductPricing " +
            "ON ProductPricing.ID = (SELECT TOP(1) ID FROM ProductPricing WHERE ProductID = Product.ID) " +
            "WHERE Sale.NetUID = @SaleNetId " +
            ") " +
            "SELECT * FROM [OrderItem] " +
            "LEFT OUTER JOIN Product " +
            "ON Product.ID = OrderItem.ProductID AND Product.Deleted = 0 " +
            "LEFT OUTER JOIN ProductPricing_CTE " +
            "ON ProductPricing_CTE.ProductID = Product.ID " +
            "LEFT OUTER JOIN Pricing " +
            "ON ProductPricing_CTE.PricingID = Pricing.ID AND Pricing.Deleted = 0 " +
            "LEFT OUTER JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
            "LEFT OUTER JOIN ProductGroup " +
            "ON ProductProductGroup.ProductGroupID = ProductGroup.ID AND ProductGroup.Deleted = 0 " +
            "LEFT OUTER JOIN [User] " +
            "ON [User].ID = OrderItem.UserID AND [User].Deleted = 0 " +
            "LEFT OUTER JOIN ProductAvailability " +
            "ON ProductAvailability.ProductId = Product.Id AND ProductAvailability.Deleted = 0 " +
            "LEFT OUTER JOIN Storage AS ProductAvailabilityStorage " +
            "ON ProductAvailabilityStorage.Id = ProductAvailability.StorageId " +
            "AND ProductAvailabilityStorage.Deleted = 0 " +
            "AND ProductAvailabilityStorage.OrganizationID = @OrganizationId " +
            "WHERE OrderItem.ID = @OrderItemId";

        Type[] types = {
            typeof(OrderItem),
            typeof(Product),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(User),
            typeof(ProductAvailability),
            typeof(Storage)
        };

        Func<object[], OrderItem> mapper = objects => {
            OrderItem orderItem = (OrderItem)objects[0];
            Product product = (Product)objects[1];
            ProductPricing productPricing = (ProductPricing)objects[2];
            Pricing pricing = (Pricing)objects[3];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[4];
            User user = (User)objects[6];
            ProductAvailability productAvailability = (ProductAvailability)objects[7];
            Storage productAvailabilityStorage = (Storage)objects[8];

            if (productAvailability != null && productAvailabilityStorage != null) {
                productAvailability.Storage = productAvailabilityStorage;
                product.ProductAvailabilities.Add(productAvailability);
            }

            if (product != null) {
                if (productPricing != null) {
                    if (pricing != null) productPricing.Pricing = pricing;

                    product.CurrentPrice = productPricing.Price;

                    if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                    orderItem.TotalAmount = productPricing.Price * Convert.ToDecimal(orderItem.Qty);
                    orderItem.User = user;
                    product.ProductPricings.Add(productPricing);
                }

                if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                orderItem.Product = product;
            }

            if (orderItemToReturn != null) {
                if (productPricing != null && !orderItemToReturn.Product.ProductPricings.Any(p => p.Id.Equals(productPricing.Id)))
                    orderItemToReturn.Product.ProductPricings.Add(productPricing);

                if (productProductGroup != null && !orderItemToReturn.Product.ProductProductGroups.Any(p => p.Id.Equals(productProductGroup.Id)))
                    orderItemToReturn.Product.ProductProductGroups.Add(productProductGroup);

                if (productAvailability != null && productAvailabilityStorage != null &&
                    !orderItemToReturn.Product.ProductAvailabilities.Any(p => p.Id.Equals(productAvailability.Id)))
                    orderItemToReturn.Product.ProductAvailabilities.Add(productAvailability);
            } else {
                orderItemToReturn = orderItem;
            }

            return orderItem;
        };

        var props = new { SaleNetId = saleNetId, OrderItemId = orderItemId, OrganizationId = organizationId };

        _connection.Query(sqlExpression, types, mapper, props);

        return orderItemToReturn;
    }

    public Dictionary<DateTime, decimal?> GetChartInfoSalesByClient(
        DateTime from,
        DateTime to,
        long clientId,
        TypePeriodGrouping typePeriod) {
        Dictionary<DateTime, decimal?> toReturn = new();

        string selectByPeriod = string.Format(
            "DATEADD({0}, DATEDIFF({0}, 0, [OrderItem].[Created]), 0) AS Date ",
            typePeriod == TypePeriodGrouping.Day ? "DAY" :
            typePeriod == TypePeriodGrouping.Week ? "WEEK" :
            typePeriod == TypePeriodGrouping.Month ? "MONTH" :
            typePeriod == TypePeriodGrouping.Year ? "YEAR" : "MONTH"
        );

        string groupedByPeriod = string.Format(
            "GROUP BY DATEADD({0}, DATEDIFF({0},0, [OrderItem].[Created]), 0) ",
            typePeriod == TypePeriodGrouping.Day ? "DAY" :
            typePeriod == TypePeriodGrouping.Week ? "WEEK" :
            typePeriod == TypePeriodGrouping.Month ? "MONTH" :
            typePeriod == TypePeriodGrouping.Year ? "YEAR" : "MONTH"
        );

        string sqlQuery =
            ";WITH VALUES_SALES AS (" +
            "SELECT " +
            selectByPeriod +
            ",CONVERT(money, SUM(([OrderItem].[Qty] - [OrderItem].[ReturnedQty]) * [OrderItem].[PricePerItem])) AS Value " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Order] " +
            "ON [Order].[ID] = [OrderItem].[OrderID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [OrderItem].[ProductID] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].[OrderID] = [Order].[ID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [Order].[ClientAgreementID] " +
            "WHERE [OrderItem].[Deleted] = 0  " +
            "AND [Sale].[ChangedToInvoice] IS NOT NULL " +
            "AND [OrderItem].[Created] >= @From " +
            "AND [OrderItem].[Created] <= @To " +
            "AND [ClientAgreement].[ClientID] = @Id " +
            groupedByPeriod;

        string endQuery =
            string.Format(
                ") " +
                "SELECT DATEADD({0}, DATEDIFF({0}, 0 , DATEADD({0}, x.number, @From)), 0) AS [DateValue] " +
                ",[VALUES_SALES].[Value] AS Value " +
                "FROM    master.dbo.spt_values x " +
                "LEFT JOIN [VALUES_SALES] " +
                "ON [VALUES_SALES].[Date] = DATEADD({0}, DATEDIFF({0}, 0 , DATEADD({0}, x.number, @From)), 0) " +
                "WHERE x.type = 'P' " +
                "AND  x.number <= DATEDIFF({0}, @From, @To)",
                typePeriod == TypePeriodGrouping.Day ? "DAY" :
                typePeriod == TypePeriodGrouping.Week ? "WEEK" :
                typePeriod == TypePeriodGrouping.Month ? "MONTH" :
                typePeriod == TypePeriodGrouping.Year ? "YEAR" : "MONTH"
            );

        sqlQuery += endQuery;

        Type[] types = {
            typeof(DateTime),
            typeof(decimal)
        };

        Func<object[], DateTime> mapper = objects => {
            DateTime date = (DateTime)objects[0];
            decimal? value = (decimal?)objects[1];

            toReturn.Add(date, value);

            return date;
        };

        _connection.Query(
            sqlQuery, types, mapper,
            new { From = from, To = to, Id = clientId },
            splitOn: "DateValue,Value"
        );

        return toReturn;
    }

    public InfoAboutSalesModel GetInfoAboutSales(
        DateTime from,
        DateTime to,
        long? managerId,
        long? organizationId) {
        InfoAboutSalesModel toReturn = new() {
            SalesByManagerAndProductTop = new List<SaleByManagerAndProductTopModel>()
        };

        string partQuerySelection = string.Empty;

        object parameters = new { From = from, To = to, ManagerId = managerId, OrganizationId = organizationId };

        if (managerId.HasValue)
            partQuerySelection = "AND [User].[ID] = @ManagerId ";

        if (organizationId.HasValue)
            partQuerySelection += "AND [Organization].[ID] = @OrganizationId ";

        string sqlQuery =
            "SELECT " +
            "[User].[NetUID] AS [UserNetId] " +
            ",[User].[LastName] + ' ' + [User].[FirstName] + ' ' + [User].[MiddleName] AS [UserFullName] " +
            ",CASE WHEN [Product].[Top] IS NULL THEN '' ELSE [Product].[Top] END [Top] " +
            ",CONVERT(money, SUM(([OrderItem].[Qty] - [OrderItem].[ReturnedQty]) * [OrderItem].[PricePerItem])) AS [Amount] " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Order] " +
            "ON [Order].[ID] = [OrderItem].[OrderID] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].[OrderID] = [Order].[ID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [OrderItem].[ProductID] " +
            "LEFT JOIN [User] " +
            "ON [User].[ID] = [Order].[UserID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [Sale].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [Agreement].[OrganizationID] " +
            "WHERE [OrderItem].[Deleted] = 0 " +
            "AND [Order].[Deleted] = 0 " +
            "AND [Sale].[Deleted] = 0 " +
            "AND [Sale].[ChangedToInvoice] IS NOT NULL " +
            "AND [Order].[Created] >= @From " +
            "AND [Order].[Created] <= @To " +
            partQuerySelection +
            "GROUP BY [User].[NetUID] " +
            ",[User].[LastName] + ' ' + [User].[FirstName] + ' ' + [User].[MiddleName] " +
            ",[Product].[Top]";

        Type[] types = {
            typeof(Guid),
            typeof(string),
            typeof(string),
            typeof(decimal)
        };

        Func<object[], Guid> mapper = objects => {
            Guid managerNetId = (Guid)objects[0];
            string managerName = (string)objects[1];
            string top = (string)objects[2];
            decimal value = (decimal)objects[3];

            if (toReturn.SalesByManagerAndProductTop.Any(x => x.ManagerNetId == managerNetId)) {
                SaleByManagerAndProductTopModel existManagerAndProductTop = toReturn.SalesByManagerAndProductTop.FirstOrDefault(x => x.ManagerNetId == managerNetId);

                if (existManagerAndProductTop == null) return managerNetId;

                if (existManagerAndProductTop.SalesValueByProductTop.Any(x => x.Key == top))
                    existManagerAndProductTop.SalesValueByProductTop[top] = value;
                else
                    existManagerAndProductTop.SalesValueByProductTop.Add(top, value);
            } else {
                SaleByManagerAndProductTopModel newManagerAndProductTopModel = new() {
                    ManagerNetId = managerNetId,
                    ManagerName = managerName
                };

                newManagerAndProductTopModel.SalesValueByProductTop.Add(top, value);

                toReturn.SalesByManagerAndProductTop.Add(newManagerAndProductTopModel);
            }

            if (toReturn.TotalByColumn.ContainsKey(top))
                toReturn.TotalByColumn[top] += value;
            else
                toReturn.TotalByColumn.Add(top, value);

            return managerNetId;
        };


        _connection.Query(
            sqlQuery, types, mapper,
            parameters,
            splitOn: "UserNetId,UserFullName,Top,Amount"
        );

        return toReturn;
    }

    public AllProductsSaleManagersModel GetManagersProductSalesByTop(DateTime from, DateTime to, TypeOfProductTop typeProductTop) {
        AllProductsSaleManagersModel toReturn = new();

        Type[] typesUser = {
            typeof(Guid),
            typeof(string)
        };

        Func<object[], Guid> mapperUser = objects => {
            Guid managerNetId = (Guid)objects[0];
            string managerName = (string)objects[1];

            toReturn.Managers.Add(new ManagerWithTotalValueSoldModel {
                NetId = managerNetId,
                ManagerName = managerName,
                TypeOrder = OrderSource.Local
            });

            return managerNetId;
        };

        _connection.Query(
            "SELECT " +
            "[User].[NetUID] AS [ManagerNetID] " +
            ",[User].[LastName] + ' ' + [User].[FirstName] + ' ' + [User].[MiddleName] AS [UserName] " +
            "FROM [User] " +
            "LEFT JOIN [UserRole] " +
            "ON [UserRole].[Deleted] = 0 " +
            "AND [UserRole].[ID] = [User].[UserRoleID] " +
            "WHERE [User].[Deleted] = 0 " +
            "AND ([UserRole].[UserRoleType] = 0 OR [UserRole].[UserRoleType] = 8)",
            typesUser, mapperUser,
            splitOn: "ManagerNetID,UserName"
        );

        string sqlQuerySelection =
            typeProductTop == TypeOfProductTop.TopN
                ? "AND [Product].[Top] = 'N' "
                : typeProductTop == TypeOfProductTop.TopX
                    ? "AND ([Product].[Top] = 'X' OR [Product].[Top] = N'Х') "
                    : "";

        string sqlQuery =
            "SELECT " +
            "[Product].[NetUID] AS [ProductNetId] " +
            ",[Product].[VendorCode] [ProductVendorCode] " +
            ",[User].[NetUID] AS [ManagerNetId] " +
            ",CONVERT(money, SUM((([OrderItem].[Qty] - [OrderItem].[ReturnedQty]) * [OrderItem].[PricePerItem]))) AS [Value] " +
            ",[Order].[OrderSource] AS [TypeOrder] " +
            "FROM [OrderItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [OrderItem].[ProductID] " +
            "LEFT JOIN [Order] " +
            "ON [Order].[ID] = [OrderItem].[OrderID] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].[OrderID] = [Order].[ID] " +
            "LEFT JOIN [User] " +
            "ON [User].[ID] = [Order].[UserID] " +
            "WHERE " +
            "[OrderItem].[Deleted] = 0 " +
            "AND [OrderItem].[Created] >= @From " +
            "AND [OrderItem].[Created] <= @To " +
            "AND [User].[NetUID] IS NOT NULL " +
            "AND [Sale].[ChangedToInvoice] IS NOT NULL " +
            sqlQuerySelection +
            "GROUP BY [Product].[NetUID] " +
            ",[Product].[VendorCode] " +
            ",[User].[NetUID] " +
            ",[Order].[OrderSource];";

        Type[] types = {
            typeof(Guid),
            typeof(string),
            typeof(Guid),
            typeof(decimal),
            typeof(OrderSource)
        };

        Func<object[], Guid> mapper = objects => {
            Guid productNetId = (Guid)objects[0];
            string vendorCode = (string)objects[1];
            Guid managerNetId = (Guid)objects[2];
            decimal valueSaleProduct = (decimal)objects[3];
            OrderSource typeOrder = (OrderSource)objects[4];

            ProductsSalesByManagersModel tempModel;

            if (toReturn.Products.Any(x => x.ProductNetId == productNetId)) {
                tempModel = toReturn.Products.FirstOrDefault(x => x.ProductNetId == productNetId);
            } else {
                tempModel = new ProductsSalesByManagersModel {
                    VendorCode = vendorCode,
                    ProductNetId = productNetId
                };

                toReturn.Products.Add(tempModel);
            }

            if (tempModel == null) return productNetId;

            if (!toReturn.Managers.Any(x => x.NetId.Equals(managerNetId))) return productNetId;

            ManagerWithTotalValueSoldModel managerModel;

            switch (typeOrder) {
                case OrderSource.Local:
                    managerModel = toReturn.Managers.FirstOrDefault(x => x.NetId == managerNetId);
                    break;
                case OrderSource.Shop:
                    managerModel = toReturn.Managers.FirstOrDefault(x => x.TypeOrder == OrderSource.Shop);
                    break;
                case OrderSource.Offer:
                default:
                    managerModel = toReturn.Managers.FirstOrDefault(x => x.TypeOrder == OrderSource.Offer);
                    break;
            }

            if (managerModel == null) return productNetId;

            managerModel.TotalManagerSold += valueSaleProduct;

            if (tempModel.ManagersSoldProduct.ContainsKey(managerModel.NetId))
                tempModel.ManagersSoldProduct[managerModel.NetId] += valueSaleProduct;
            else
                tempModel.ManagersSoldProduct.Add(managerModel.NetId, valueSaleProduct);

            return productNetId;
        };

        _connection.Query(
            sqlQuery, types, mapper,
            new { From = from, To = to },
            splitOn: "ProductNetId,ProductVendorCode,ManagerNetId,Value,TypeOrder"
        );

        return toReturn;
    }

    public decimal GetReSalePricePerItem(Guid productNetId, Guid clientAgreementNetId, long orderItemId) {
        return _connection.Query<decimal>(
            "SELECT dbo.[GetCalculatedProductPriceWithShares_ReSale](@ProductNetId, @ClientAgreementNetId, @Culture, @OrderItemId)",
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                ProductNetId = productNetId,
                ClientAgreementNetId = clientAgreementNetId,
                OrderItemId = orderItemId
            }
        ).Single();
    }
}