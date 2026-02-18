using System;
using System.Collections.Generic;
using GBA.Domain.Entities.ExchangeRates;

namespace GBA.Domain.Repositories.ExchangeRates.Contracts;

public interface IGovCrossExchangeRateHistoryRepository {
    long Add(GovCrossExchangeRateHistory crossExchangeRateHistory);

    long AddSpecific(GovCrossExchangeRateHistory crossExchangeRateHistory);

    List<GovCrossExchangeRateHistory> GetAllByGovCrossExchangeRateNetId(Guid netId, long limit, long offset);

    IEnumerable<GovCrossExchangeRate> GetAllByGovCrossExchangeRateNetIds(IEnumerable<Guid> netIds, long limit, long offset, DateTime from, DateTime to);

    GovCrossExchangeRateHistory GetLatestByCrossExchangeRateNetId(Guid netId);

    GovCrossExchangeRateHistory GetLatestNearToDateByCrossExchangeRateNetId(Guid netId, DateTime fromDate);

    GovCrossExchangeRateHistory GetFirstByGovCrossExchangeRateId(long id);

    GovCrossExchangeRateHistory GetById(long id);
}