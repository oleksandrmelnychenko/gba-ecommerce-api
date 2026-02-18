using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Ecommerce;

namespace GBA.Domain.Repositories.Ecommerce.Contracts;

public interface IEcommerceContactsRepository {
    long Add(EcommerceContacts page);

    void Update(EcommerceContacts page);

    EcommerceContacts GetById(long id);

    EcommerceContacts GetByNetId(Guid netId);

    List<EcommerceContacts> GetAll();

    void Remove(Guid netId);
}