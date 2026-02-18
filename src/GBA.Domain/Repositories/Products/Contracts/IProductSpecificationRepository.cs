using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductSpecificationRepository {
    long Add(ProductSpecification productSpecification);

    void Add(IEnumerable<ProductSpecification> productSpecifications);

    void Update(ProductSpecification productSpecification);

    void SetInactiveByProductId(long productId, string locale);

    ProductSpecification GetActiveByProductIdAndLocale(long productId, string locale);

    ProductSpecification GetByProductAndSupplyOrderIdsIfExists(long productId, long supplyOrderId);

    ProductSpecification GetByProductAndSupplyInvoiceIdsIfExists(long productId, long supplyInvoiceId);

    ProductSpecification GetByProductAndSadIdsIfExists(long productId, long sadId, string culture);

    IEnumerable<ProductSpecification> GetAllFromSearch(string value);

    IEnumerable<ProductSpecification> GetAllProductSpecificationsFiltered(
        string vendorCode,
        string specificationCode,
        string locale,
        long limit,
        long offset);

    ProductSpecification GetById(long id);

    void SetIsActiveById(long id);
}