using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductSpecificationRepository : IProductSpecificationRepository {
    private readonly IDbConnection _connection;

    public ProductSpecificationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ProductSpecification productSpecification) {
        return _connection.Query<long>(
            "INSERT INTO [ProductSpecification] ([Name], SpecificationCode, [Locale], DutyPercent, AddedById, ProductId, IsActive, Updated, " +
            "[CustomsValue], [Duty], [VATValue], [VATPercent]) " +
            "VALUES (@Name, @SpecificationCode, @Locale, @DutyPercent, @AddedById, @ProductId, @IsActive, GETUTCDATE(), " +
            "@CustomsValue, @Duty, @VATValue, @VATPercent); " +
            "SELECT SCOPE_IDENTITY();",
            productSpecification
        ).Single();
    }

    public void Add(IEnumerable<ProductSpecification> productSpecifications) {
        _connection.Execute(
            "INSERT INTO [ProductSpecification] ([Name], SpecificationCode, [Locale], DutyPercent, AddedById, ProductId, IsActive, Updated, " +
            "[CustomsValue], [Duty], [VATValue], [VATPercent]) " +
            "VALUES (@Name, @SpecificationCode, @Locale, @DutyPercent, @AddedById, @ProductId, @IsActive, GETUTCDATE(), " +
            "@CustomsValue, @Duty, @VATValue, @VATPercent)",
            productSpecifications
        );
    }

    public void Update(ProductSpecification productSpecification) {
        _connection.Execute(
            "UPDATE [ProductSpecification] " +
            "SET [Name] = @Name" +
            ", SpecificationCode = @SpecificationCode" +
            ", DutyPercent = @DutyPercent" +
            ", AddedById = @AddedById" +
            ", IsActive = @IsActive " +
            ", Updated = GETUTCDATE() " +
            ", CustomsValue = @CustomsValue " +
            ", Duty = @Duty " +
            ", VATValue = @VATValue " +
            ", VATPercent = @VATPercent " +
            "WHERE ID = @Id",
            productSpecification
        );
    }

    public void SetInactiveByProductId(long productId, string locale) {
        _connection.Execute(
            "UPDATE [ProductSpecification] " +
            "SET IsActive = 0, Updated = GETUTCDATE() " +
            "WHERE [ProductSpecification].ProductID = @ProductId " +
            "AND [ProductSpecification].IsActive = 1 " +
            "AND [ProductSpecification].[Locale] = @Locale",
            new { ProductId = productId, Locale = locale }
        );
    }

    public ProductSpecification GetActiveByProductIdAndLocale(long productId, string locale) {
        return _connection.Query<ProductSpecification>(
            "SELECT TOP(1) * " +
            "FROM [ProductSpecification] " +
            "WHERE [ProductSpecification].Deleted = 0 " +
            "AND [ProductSpecification].IsActive = 1 " +
            "AND [ProductSpecification].Locale = @Locale " +
            "AND [ProductSpecification].ProductID = @ProductId " +
            "ORDER BY [ProductSpecification].ID DESC",
            new { ProductId = productId, Locale = locale }
        ).SingleOrDefault();
    }

    public ProductSpecification GetByProductAndSupplyInvoiceIdsIfExists(long productId, long supplyInvoiceId) {
        return _connection.Query<ProductSpecification, OrderProductSpecification, SupplyInvoice, SupplyOrder, Product, ProductSpecification>(
            "SELECT TOP(1) * " +
            "FROM [ProductSpecification] " +
            "LEFT JOIN [OrderProductSpecification] " +
            "ON [OrderProductSpecification].ProductSpecificationID = [ProductSpecification].ID " +
            "AND [OrderProductSpecification].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [OrderProductSpecification].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductSpecification].ProductID " +
            "WHERE [SupplyInvoice].ID = @SupplyInvoiceId " +
            "AND [Product].ID = @ProductId " +
            "ORDER BY [ProductSpecification].ID DESC",
            (specification, orderSpecification, invoice, order, product) => {
                invoice.SupplyOrder = order;

                orderSpecification.SupplyInvoice = invoice;

                specification.OrderProductSpecification = orderSpecification;
                specification.Product = product;

                return specification;
            },
            new { ProductId = productId, SupplyInvoiceId = supplyInvoiceId }
        ).SingleOrDefault();
    }

    public ProductSpecification GetByProductAndSupplyOrderIdsIfExists(long productId, long supplyOrderId) {
        return _connection.Query<ProductSpecification, OrderProductSpecification, SupplyInvoice, SupplyOrder, Product, ProductSpecification>(
            "SELECT TOP(1) * " +
            "FROM [ProductSpecification] " +
            "LEFT JOIN [OrderProductSpecification] " +
            "ON [OrderProductSpecification].ProductSpecificationID = [ProductSpecification].ID " +
            "AND [OrderProductSpecification].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [OrderProductSpecification].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductSpecification].ProductID " +
            "WHERE [SupplyOrder].ID = @SupplyOrderId " +
            "AND [Product].ID = @ProductId " +
            "ORDER BY [ProductSpecification].ID DESC",
            (specification, orderSpecification, invoice, order, product) => {
                invoice.SupplyOrder = order;

                orderSpecification.SupplyInvoice = invoice;

                specification.OrderProductSpecification = orderSpecification;
                specification.Product = product;

                return specification;
            },
            new { ProductId = productId, SupplyOrderId = supplyOrderId }
        ).SingleOrDefault();
    }

    public ProductSpecification GetByProductAndSadIdsIfExists(long productId, long sadId, string culture) {
        return _connection.Query<ProductSpecification, OrderProductSpecification, SupplyInvoice, SupplyOrder, Product, Sad, ProductSpecification>(
            "SELECT TOP(1) * " +
            "FROM [ProductSpecification] " +
            "LEFT JOIN [OrderProductSpecification] " +
            "ON [OrderProductSpecification].ProductSpecificationID = [ProductSpecification].ID " +
            "AND [OrderProductSpecification].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [OrderProductSpecification].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductSpecification].ProductID " +
            "LEFT JOIN [Sad] " +
            "ON [OrderProductSpecification].SadID = [Sad].ID " +
            "WHERE [Sad].ID = @SadId " +
            "AND [Product].ID = @ProductId " +
            "AND [ProductSpecification].Locale = @Culture " +
            "ORDER BY [ProductSpecification].ID DESC",
            (specification, orderSpecification, invoice, order, product, sad) => {
                if (invoice != null) invoice.SupplyOrder = order;

                orderSpecification.Sad = sad;
                orderSpecification.SupplyInvoice = invoice;

                specification.OrderProductSpecification = orderSpecification;
                specification.Product = product;

                return specification;
            },
            new { ProductId = productId, SadId = sadId, Culture = culture }
        ).SingleOrDefault();
    }

    public IEnumerable<ProductSpecification> GetAllFromSearch(string value) {
        return _connection.Query<ProductSpecification>(
            "SELECT [ProductSpecification].SpecificationCode " +
            "FROM [ProductSpecification] " +
            "WHERE [ProductSpecification].SpecificationCode like '%' + @Value + '%' " +
            "AND [ProductSpecification].IsActive = 1 " +
            "GROUP BY [ProductSpecification].SpecificationCode " +
            "ORDER BY [ProductSpecification].SpecificationCode",
            new { Value = value }
        );
    }

    public IEnumerable<ProductSpecification> GetAllProductSpecificationsFiltered(
        string vendorCode,
        string specificationCode,
        string locale,
        long limit,
        long offset) {
        return _connection.Query<ProductSpecification, OrderProductSpecification, SupplyInvoice, Product, User, Sad, ProductSpecification>(
            "; WITH [Search_CTE] " +
            "AS (" +
            "SELECT [ProductSpecification].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [ProductSpecification].[Created] DESC) AS [RowNumber] " +
            "FROM [ProductSpecification] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductSpecification].ProductID " +
            "WHERE [ProductSpecification].Locale = @Locale " +
            "AND [ProductSpecification].Deleted = 0 " +
            "AND [ProductSpecification].IsActive = 1 " +
            (
                string.IsNullOrEmpty(vendorCode)
                    ? string.Empty
                    : "AND PATINDEX(N'%' + @VendorCode + N'%', [Product].VendorCode) <> 0 "
            ) +
            (
                string.IsNullOrEmpty(specificationCode)
                    ? string.Empty
                    : "AND PATINDEX(N'%' + @SpecificationCode + N'%', [ProductSpecification].SpecificationCode) <> 0 "
            ) +
            ") " +
            "SELECT [ProductSpecification].* " +
            ", [OrderProductSpecification].* " +
            ", [SupplyInvoice].* " +
            ", [Product].* " +
            ", [User].* " +
            ", [Sad].* " +
            "FROM [ProductSpecification] " +
            "LEFT JOIN [OrderProductSpecification] " +
            "ON [OrderProductSpecification].ProductSpecificationID = [ProductSpecification].ID " +
            "AND [OrderProductSpecification].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [OrderProductSpecification].SupplyInvoiceID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductSpecification].ProductID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductSpecification].AddedByID " +
            "LEFT JOIN [Sad] " +
            "ON [Sad].ID = [OrderProductSpecification].SadID " +
            "WHERE [ProductSpecification].ID IN (" +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset" +
            ")",
            (specification, orderSpecification, invoice, product, addedBy, sad) => {
                specification.Product = product;
                specification.AddedBy = addedBy;

                if (orderSpecification == null)
                    return specification;

                orderSpecification.Sad = sad;
                orderSpecification.SupplyInvoice = invoice;

                specification.OrderProductSpecification = orderSpecification;

                return specification;
            },
            new {
                VendorCode = vendorCode,
                SpecificationCode = specificationCode,
                Locale = locale,
                Limit = limit,
                Offset = offset
            }
        );
    }

    public ProductSpecification GetById(long id) {
        return _connection.Query<ProductSpecification>(
            "SELECT * FROM [ProductSpecification] " +
            "WHERE [ProductSpecification].[ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }

    public void SetIsActiveById(long id) {
        _connection.Execute(
            "UPDATE [ProductSpecification] " +
            "SET [Updated] = getutcdate() " +
            ", [IsActive] = 1 " +
            "WHERE [ProductSpecification].[ID] = @Id; ",
            new { Id = id });
    }
}