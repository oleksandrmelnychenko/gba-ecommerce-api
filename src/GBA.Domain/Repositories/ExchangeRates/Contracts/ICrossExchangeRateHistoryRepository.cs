using System;
using System.Collections.Generic;
using GBA.Domain.Entities.ExchangeRates;

namespace GBA.Domain.Repositories.ExchangeRates.Contracts;

public interface ICrossExchangeRateHistoryRepository {
    long Add(CrossExchangeRateHistory crossExchangeRateHistory);

    long AddSpecific(CrossExchangeRateHistory crossExchangeRateHistory);

    List<CrossExchangeRateHistory> GetAllByCrossExchangeRateNetId(Guid netId, long limit, long offset);

    IEnumerable<CrossExchangeRate> GetAllByCrossExchangeRateNetIds(IEnumerable<Guid> netIds, long limit, long offset, DateTime from, DateTime to);

    CrossExchangeRateHistory GetLatestByCrossExchangeRateNetId(Guid netId);

    CrossExchangeRateHistory GetLatestNearToDateByCrossExchangeRateNetId(Guid netId, DateTime fromDate);

    CrossExchangeRateHistory GetFirstByCrossExchangeRateId(long id);
}