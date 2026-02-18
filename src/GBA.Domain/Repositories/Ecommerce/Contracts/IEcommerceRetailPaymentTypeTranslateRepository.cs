using System.Collections.Generic;
using GBA.Domain.Entities.Ecommerce;

namespace GBA.Domain.Repositories.Ecommerce.Contracts;

public interface IEcommerceRetailPaymentTypeTranslateRepository {
    void Add(RetailPaymentTypeTranslate retailPaymentTypeTranslate);

    List<RetailPaymentTypeTranslate> GetAllRetailPayments();

    long Update(RetailPaymentTypeTranslate retailPaymentType);

    RetailPaymentTypeTranslate GetByCultureCode(string code);

    RetailPaymentTypeTranslate GetLast();
}