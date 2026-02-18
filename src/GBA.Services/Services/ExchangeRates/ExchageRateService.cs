using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Services.Services.ExchangeRates.Contracts;

namespace GBA.Services.Services.ExchangeRates;

public sealed class ExchageRateService : IExchageRateService {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoryFactory;

    public ExchageRateService(
        IExchangeRateRepositoriesFactory exchangeRateRepositoryFactory,
        IDbConnectionFactory connectionFactory) {
        _exchangeRateRepositoryFactory = exchangeRateRepositoryFactory;

        _connectionFactory = connectionFactory;
    }

    public Task<List<ExchangeRate>> GetAllByCurrentCulture() {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        return Task.FromResult(_exchangeRateRepositoryFactory.NewExchangeRateRepository(connection).GetAllByCulture());
    }
}
