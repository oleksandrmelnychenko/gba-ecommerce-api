using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.Entities.ExchangeRates;

namespace GBA.Services.Services.ExchangeRates.Contracts;

public interface IExchageRateService {
    Task<List<ExchangeRate>> GetAllByCurrentCulture();
}