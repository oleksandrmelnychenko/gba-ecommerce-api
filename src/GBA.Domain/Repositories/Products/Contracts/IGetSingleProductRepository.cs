using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers.ProductAvailabilityModels;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IGetSingleProductRepository {
    Product GetById(long id);

    Product GetProductByNetId(
        Guid netId,
        Guid? clientAgreementNetId,
        bool withVat,
        long? currencyId,
        long? organizationId);

    Product GetByNetId(Guid netId, Guid? clientAgreementNetId = null);

    Product GetByNetIdForRetail(Guid netId, long organizationId, bool withVat);

    Product GetByNetId(Guid netId, Guid nonVatAgreementNetId, Guid? vatAgreementNetId);

    Product GetBySlug(string slug, Guid? clientAgreementNetId = null);

    Product GetBySlug(string slug, Guid nonVatAgreementNetId, Guid? vatAgreementNetId);

    Product GetByNetIdWithAvailabilityByCurrentCulture(Guid netId, Guid? clientAgreementNetId = null);

    Product GetByNetIdWithoutIncludes(Guid netId);

    List<Product> GetAll();

    Product GetProductByVendorCode(string vendorCode);

    Product GetProductByVendorCodeWithMeasureUnit(string vendorCode);

    Product GetByVendorCodeAndRuleLocaleWithProductGroupAndWriteOffRules(string vendorCode, string locale);

    Product GetByIdAndRuleLocaleWithProductGroupAndWriteOffRules(long id, string locale);

    Product GetByIdWithCalculatedAvailability(
        long productId,
        long organizationId,
        bool withVat,
        Guid clientAgreementNetId);

    ProductAvailabilityModel GetAllProductAvailabilities(
        Guid productNetId,
        Guid clientAgreementNetId,
        Guid saleNetId);

    Product GetProductByVendorCodeWithWriteOffRule(string vendorCode);

    long GetProductIdByVendorCode(string vendorCode);
}