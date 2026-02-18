using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductGroupDiscountRepository : IProductGroupDiscountRepository {
    private readonly IDbConnection _connection;

    public ProductGroupDiscountRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ProductGroupDiscount productGroupDiscount) {
        return _connection.Query<long>(
                "INSERT INTO ProductGroupDiscount (ClientAgreementId, ProductGroupId, IsActive, DiscountRate, Updated) " +
                "VALUES (@ClientAgreementId, @ProductGroupId, @IsActive, @DiscountRate, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                productGroupDiscount
            )
            .Single();
    }

    public void Add(IEnumerable<ProductGroupDiscount> productGroupDiscounts) {
        _connection.Execute(
            "INSERT INTO ProductGroupDiscount (ClientAgreementId, ProductGroupId, IsActive, DiscountRate, Updated) " +
            "VALUES (@ClientAgreementId, @ProductGroupId, @IsActive, @DiscountRate, getutcdate())",
            productGroupDiscounts
        );
    }

    public void Update(ProductGroupDiscount productGroupDiscount) {
        _connection.Execute(
            "UPDATE ProductGroupDiscount SET " +
            "ClientAgreementId = @ClientAgreementId, ProductGroupId = @ProductGroupId, IsActive = @IsActive, DiscountRate = @DiscountRate, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            productGroupDiscount
        );
    }

    public void Update(IEnumerable<ProductGroupDiscount> productGroupDiscount) {
        _connection.Execute(
            "UPDATE ProductGroupDiscount SET " +
            "ClientAgreementId = @ClientAgreementId, ProductGroupId = @ProductGroupId, IsActive = @IsActive, DiscountRate = @DiscountRate, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            productGroupDiscount
        );
    }

    public List<ProductGroupDiscount> GetAllByClientAgreementIds(IEnumerable<long> ids) {
        return _connection.Query<ProductGroupDiscount, ProductGroup, ClientAgreement, Agreement, ProductGroupDiscount>(
                "SELECT * FROM ProductGroupDiscount " +
                "LEFT JOIN ProductGroup " +
                "ON ProductGroupDiscount.ProductGroupID = ProductGroup.ID " +
                "LEFT JOIN ClientAgreement " +
                "ON ProductGroupDiscount.ClientAgreementID = ClientAgreement.ID " +
                "LEFT JOIN Agreement " +
                "ON ClientAgreement.AgreementID = Agreement.ID " +
                "WHERE ProductGroupDiscount.ClientAgreementID IN @Ids",
                (discount, productGroup, clientAgreement, agreement) => {
                    clientAgreement.Agreement = agreement;
                    discount.ClientAgreement = clientAgreement;
                    discount.ProductGroup = productGroup;

                    return discount;
                },
                new { Ids = ids }
            )
            .ToList();
    }

    public List<ProductGroupDiscount> GetAllByClientId(long id) {
        return _connection.Query<ProductGroupDiscount, ClientAgreement, ProductGroupDiscount>(
                "SELECT * FROM ProductGroupDiscount " +
                "LEFT JOIN ClientAgreement " +
                "ON ProductGroupDiscount.ClientAgreementID = ClientAgreement.ID " +
                "WHERE ProductGroupDiscount.Deleted = 0 " +
                "AND ClientAgreement.Deleted = 0 " +
                "AND ClientAgreement.ClientID = @Id",
                (discount, agreement) => {
                    discount.ClientAgreement = agreement;

                    return discount;
                },
                new { Id = id }
            )
            .ToList();
    }

    public ProductGroupDiscount GetByProductGroupAndClientAgreementIdsIfExists(long clientAgreementId, long productGroupId) {
        return _connection.Query<ProductGroupDiscount>(
                "SELECT TOP(1) * " +
                "FROM [ProductGroupDiscount] " +
                "WHERE ClientAgreementID = @ClientAgreementId " +
                "AND ProductGroupID = @ProductGroupId " +
                "AND IsActive = 1 " +
                "AND Deleted = 0 " +
                "ORDER BY ID DESC",
                new {
                    ClientAgreementId = clientAgreementId,
                    ProductGroupId = productGroupId
                }
            )
            .SingleOrDefault();
    }

    public ProductGroupDiscount GetById(long id) {
        return _connection.Query<ProductGroupDiscount, ProductGroup, ClientAgreement, ProductGroupDiscount>(
                "SELECT * FROM ProductGroupDiscount " +
                "LEFT JOIN ProductGroup " +
                "ON ProductGroupDiscount.ProductGroupID = ProductGroup.ID " +
                "LEFT JOIN ClientAgreement " +
                "ON ProductGroupDiscount.ClientAgreementID = ClientAgreement.ID " +
                "WHERE ProductGroupDiscount.ID = @Id",
                (discount, productGroup, agreement) => {
                    discount.ClientAgreement = agreement;
                    discount.ProductGroup = productGroup;

                    return discount;
                },
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public void Remove(IEnumerable<ProductGroupDiscount> productGroupDiscount) {
        _connection.Execute(
            "UPDATE ProductGroupDiscount SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetUid",
            productGroupDiscount
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ProductGroupDiscount] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductGroupDiscount].ID IN @Ids",
            new { Ids = ids }
        );
    }
}