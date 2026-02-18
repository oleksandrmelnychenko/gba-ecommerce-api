using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Pricings;

namespace GBA.Domain.Repositories.Pricings.Contracts;

public interface IPricingRepository {
    long Add(Pricing pricing);

    Pricing GetById(long id);

    Pricing GetByNetId(Guid netId);

    long GetPricingIdByName(string name);

    Pricing GetRetailPricingWithCalculatedExtraChargeByCulture();

    Pricing GetByNetIdWithCalculatedExtraCharge(Guid netId);

    Pricing GetByIdWithCalculatedExtraCharge(long id);

    Pricing GetByClientAgreementNetIdWithCalculatedExtraCharge(Guid netId);

    Pricing GetPricingByCurrentCultureWithHighestExtraCharge();

    List<Pricing> GetAll();

    List<Pricing> GetAllBasePricings();

    List<Pricing> GetAllWithBasePricings();

    List<Pricing> GetAllWithCalculatedExtraChargeByCurrentCulture();

    List<Pricing> GetAllWithCalculatedExtraCharge();

    List<Pricing> GetAllWithCalculatedExtraChargeWithDynamicDiscounts(Guid productNetId);

    decimal GetCalculatedExtraChargeForCurrentPricing(long id);

    decimal GetCalculatedExtraChargeForCurrentPricing(Guid netId);

    bool IsAnyAssignedToBasePricing(long basePricingId);

    void Update(Pricing pricing);

    void UpdatePricingPriorityById(long id, bool raise);

    void Remove(Guid netId);
}