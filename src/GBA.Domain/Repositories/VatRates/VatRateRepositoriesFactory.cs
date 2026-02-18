using System.Data;
using GBA.Domain.Repositories.VatRates.Contracts;

namespace GBA.Domain.Repositories.VatRates;

public sealed class VatRateRepositoriesFactory : IVatRateRepositoriesFactory {
    public IVatRateRepository NewVatRateRepository(IDbConnection connection) {
        return new VatRateRepository(connection);
    }
}