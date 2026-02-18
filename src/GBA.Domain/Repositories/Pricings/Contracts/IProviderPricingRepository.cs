using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Pricings;

namespace GBA.Domain.Repositories.Pricings.Contracts;

public interface IProviderPricingRepository {
    long Add(ProviderPricing providerPricing);

    void Update(ProviderPricing providerPricing);

    ProviderPricing GetById(long id);

    ProviderPricing GetByNetId(Guid netId);

    List<ProviderPricing> GetAll();

    void Remove(Guid netId);
}