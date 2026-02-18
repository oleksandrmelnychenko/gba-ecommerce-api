using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductWriteOffRuleRepository : IProductWriteOffRuleRepository {
    private readonly IDbConnection _connection;

    public ProductWriteOffRuleRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ProductWriteOffRule productWriteOffRule) {
        return _connection.Query<long>(
                "INSERT INTO [ProductWriteOffRule] (RuleLocale, RuleType, CreatedByID, UpdatedByID, ProductID, ProductGroupID, Updated) " +
                "VALUES (@RuleLocale, @RuleType, @CreatedById, @UpdatedById, @ProductId, @ProductGroupId, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                productWriteOffRule
            )
            .Single();
    }

    public void Update(ProductWriteOffRule productWriteOffRule) {
        _connection.Execute(
            "UPDATE [ProductWriteOffRule] " +
            "SET RuleType = @RuleType, UpdatedById = @UpdatedById, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            productWriteOffRule
        );
    }

    public ProductWriteOffRule GetById(long id) {
        return _connection.Query<ProductWriteOffRule, Product, ProductGroup, ProductWriteOffRule>(
                "SELECT * " +
                "FROM [ProductWriteOffRule] " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [ProductWriteOffRule].ProductID " +
                "LEFT JOIN [ProductGroup] " +
                "ON [ProductGroup].ID = [ProductWriteOffRule].ProductGroupID " +
                "WHERE [ProductWriteOffRule].ID = @Id",
                (rule, product, productGroup) => {
                    rule.Product = product;
                    rule.ProductGroup = productGroup;

                    return rule;
                },
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public ProductWriteOffRule GetByProductIdAndRuleLocaleIfExists(long productId, string ruleLocale) {
        return _connection.Query<ProductWriteOffRule>(
                "SELECT * " +
                "FROM [ProductWriteOffRule] " +
                "WHERE [ProductWriteOffRule].Deleted = 0 " +
                "AND [ProductWriteOffRule].ProductID = @Id " +
                "AND [ProductWriteOffRule].RuleLocale = @RuleLocale",
                new { Id = productId, RuleLocale = ruleLocale }
            )
            .SingleOrDefault();
    }

    public ProductWriteOffRule GetByProductGroupIdAndRuleLocaleIfExists(long productGroupId, string ruleLocale) {
        return _connection.Query<ProductWriteOffRule>(
                "SELECT * " +
                "FROM [ProductWriteOffRule] " +
                "WHERE [ProductWriteOffRule].Deleted = 0 " +
                "AND [ProductWriteOffRule].ProductGroupID = @Id " +
                "AND [ProductWriteOffRule].RuleLocale = @RuleLocale",
                new { Id = productGroupId, RuleLocale = ruleLocale }
            )
            .SingleOrDefault();
    }

    public ProductWriteOffRule GetByRuleLocale(string ruleLocale) {
        return _connection.Query<ProductWriteOffRule>(
                "SELECT * " +
                "FROM [ProductWriteOffRule] " +
                "WHERE [ProductWriteOffRule].Deleted = 0 " +
                "AND [ProductWriteOffRule].ProductID IS NULL " +
                "AND [ProductWriteOffRule].ProductGroupID IS NULL " +
                "AND [ProductWriteOffRule].RuleLocale = @RuleLocale",
                new { RuleLocale = ruleLocale }
            )
            .SingleOrDefault();
    }

    public IEnumerable<ProductWriteOffRule> GetByProductNetId(Guid netId) {
        return _connection.Query<ProductWriteOffRule, Product, ProductWriteOffRule>(
            "SELECT * " +
            "FROM [ProductWriteOffRule] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductWriteOffRule].ProductID " +
            "WHERE [ProductWriteOffRule].Deleted = 0 " +
            "AND [Product].NetUID = @NetId",
            (rule, product) => {
                rule.Product = product;

                return rule;
            },
            new { NetId = netId }
        );
    }

    public IEnumerable<ProductWriteOffRule> GetByProductGroupNetId(Guid netId) {
        return _connection.Query<ProductWriteOffRule, ProductGroup, ProductWriteOffRule>(
            "SELECT * " +
            "FROM [ProductWriteOffRule] " +
            "LEFT JOIN [ProductGroup] " +
            "ON [ProductGroup].ID = [ProductWriteOffRule].ProductGroupID " +
            "WHERE [ProductWriteOffRule].Deleted = 0 " +
            "AND [ProductGroup].NetUID = @NetId",
            (rule, group) => {
                rule.ProductGroup = group;

                return rule;
            },
            new { NetId = netId }
        );
    }

    public IEnumerable<ProductWriteOffRule> GetByProductId(long productId) {
        return _connection.Query<ProductWriteOffRule>(
            "SELECT * " +
            "FROM [ProductWriteOffRule] " +
            "WHERE [ProductWriteOffRule].Deleted = 0 " +
            "AND [ProductWriteOffRule].ProductID = @Id",
            new { Id = productId }
        );
    }

    public IEnumerable<ProductWriteOffRule> GetByProductGroupId(long productGroupId) {
        return _connection.Query<ProductWriteOffRule>(
            "SELECT * " +
            "FROM [ProductWriteOffRule] " +
            "WHERE [ProductWriteOffRule].Deleted = 0 " +
            "AND [ProductWriteOffRule].ProductGroupID = @Id",
            new { Id = productGroupId }
        );
    }

    public IEnumerable<ProductWriteOffRule> GetDefaultRules() {
        return _connection.Query<ProductWriteOffRule>(
            "SELECT * " +
            "FROM [ProductWriteOffRule] " +
            "WHERE [ProductWriteOffRule].Deleted = 0 " +
            "AND [ProductWriteOffRule].ProductID IS NULL " +
            "AND [ProductWriteOffRule].ProductGroupID IS NULL"
        );
    }

    public void Remove(long ruleId, long updatedById) {
        _connection.Execute(
            "UPDATE [ProductWriteOffRule] " +
            "SET Deleted = 1, Updated = GETUTCDATE(), UpdatedById = @UpdatedById " +
            "WHERE ID = @RuleId",
            new { RuleId = ruleId, UpdatedById = updatedById }
        );
    }

    public void Remove(Guid ruleNetId, long updatedById) {
        _connection.Execute(
            "UPDATE [ProductWriteOffRule] " +
            "SET Deleted = 1, Updated = GETUTCDATE(), UpdatedById = @UpdatedById " +
            "WHERE NetUID = @RuleNetId",
            new { RuleNetId = ruleNetId, UpdatedById = updatedById }
        );
    }
}