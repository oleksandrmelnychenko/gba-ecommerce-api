using System;
using System.Collections.Generic;
using GBA.Domain.Entities.ExchangeRates;

namespace GBA.Domain.Repositories.ExchangeRates.Contracts;

public interface IGovExchangeRateHistoryRepository {
    long Add(GovExchangeRateHistory govExchangeRateHistory);

    long AddSpecific(GovExchangeRateHistory govExchangeRateHistory);

    IEnumerable<GovExchangeRateHistory> GetAllByExchangeRateId(long id, long limit, long offset);

    IEnumerable<GovExchangeRateHistory> GetAllByExchangeRateNetId(Guid netId, long limit, long offset);

    IEnumerable<GovExchangeRate> GetAllByExchangeRateNetIds(IEnumerable<Guid> netIds, long limit, long offset, DateTime from, DateTime to);

    GovExchangeRateHistory GetLatestByExchangeRateNetId(Guid netId);

    GovExchangeRateHistory GetLatestNearToDateByExchangeRateNetId(Guid netId, DateTime fromDate);

    GovExchangeRateHistory GetFirstByExchangeRateId(long id);

    GovExchangeRateHistory GetById(long id);
}