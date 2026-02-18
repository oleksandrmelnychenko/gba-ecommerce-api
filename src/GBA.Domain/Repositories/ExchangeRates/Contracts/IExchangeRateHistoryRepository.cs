using System;
using System.Collections.Generic;
using GBA.Domain.Entities.ExchangeRates;

namespace GBA.Domain.Repositories.ExchangeRates.Contracts;

public interface IExchangeRateHistoryRepository {
    long Add(ExchangeRateHistory exchangeRateHistory);

    long AddSpecific(ExchangeRateHistory exchangeRateHistory);

    IEnumerable<ExchangeRateHistory> GetAllByExchangeRateId(long id, long limit, long offset);

    IEnumerable<ExchangeRateHistory> GetAllByExchangeRateNetId(Guid netId, long limit, long offset);

    IEnumerable<ExchangeRate> GetAllByExchangeRateNetIds(IEnumerable<Guid> netIds, long limit, long offset, DateTime from, DateTime to);

    ExchangeRateHistory GetLatestByExchangeRateNetId(Guid netId);

    ExchangeRateHistory GetLatestNearToDateByExchangeRateNetId(Guid netId, DateTime fromDate);

    ExchangeRateHistory GetFirstByExchangeRateId(long id);

    ExchangeRateHistory GetById(long id);
}