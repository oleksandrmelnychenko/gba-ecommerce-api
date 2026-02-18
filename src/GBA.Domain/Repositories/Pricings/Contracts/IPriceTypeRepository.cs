using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Pricings;

namespace GBA.Domain.Repositories.Pricings.Contracts;

public interface IPriceTypeRepository {
    void Add(PriceType priceType);

    void Update(PriceType priceType);

    List<PriceType> GetAll();

    void Remove(Guid netId);
}