using System;
using System.Collections.Generic;
using GBA.Domain.Entities.VatRates;

namespace GBA.Domain.Repositories.VatRates.Contracts;

public interface IVatRateRepository {
    long New(VatRate vatRate);

    void Update(VatRate vatRate);

    void Remove(Guid netId);

    IEnumerable<VatRate> GetAll();

    VatRate GetById(long id);
}