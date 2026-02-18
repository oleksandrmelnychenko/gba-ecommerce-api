using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductWriteOffRuleRepository {
    long Add(ProductWriteOffRule productWriteOffRule);

    void Update(ProductWriteOffRule productWriteOffRule);

    ProductWriteOffRule GetById(long id);

    ProductWriteOffRule GetByProductIdAndRuleLocaleIfExists(long productId, string ruleLocale);

    ProductWriteOffRule GetByProductGroupIdAndRuleLocaleIfExists(long productGroupId, string ruleLocale);

    ProductWriteOffRule GetByRuleLocale(string ruleLocale);

    IEnumerable<ProductWriteOffRule> GetByProductId(long productId);

    IEnumerable<ProductWriteOffRule> GetByProductGroupId(long productGroupId);

    IEnumerable<ProductWriteOffRule> GetByProductNetId(Guid netId);

    IEnumerable<ProductWriteOffRule> GetByProductGroupNetId(Guid netId);

    IEnumerable<ProductWriteOffRule> GetDefaultRules();

    void Remove(long ruleId, long updatedById);

    void Remove(Guid ruleNetId, long updatedById);
}