using System;
using GBA.Domain.Entities.Ecommerce;

namespace GBA.Domain.Repositories.Ecommerce.Contracts;

public interface IEcommerceContactInfoRepository {
    long Add(EcommerceContactInfo info);

    void Update(EcommerceContactInfo info);

    EcommerceContactInfo GetById(long id);

    EcommerceContactInfo GetByNetId(Guid netId);

    void Remove(Guid netId);
    EcommerceContactInfo GetLast();

    EcommerceContactInfo GetLast(string locale);

    void UpdateWithLocale(EcommerceContactInfo info);
}