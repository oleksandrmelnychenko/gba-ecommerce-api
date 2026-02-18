using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Ecommerce;

namespace GBA.Domain.Repositories.Ecommerce.Contracts;

public interface IEcommercePageRepository {
    long Add(EcommercePage page);

    void Update(EcommercePage page);

    EcommercePage GetById(long id);

    EcommercePage GetByNetId(Guid netId);

    List<EcommercePage> GetAll();

    void Remove(Guid netId);

    void AddList(List<EcommercePage> ecommercePages);

    List<SeoPage> GetAll(string locale);

    void AddList(List<SeoPage> seoPages);

    void Update(SeoPage seoPage);
}