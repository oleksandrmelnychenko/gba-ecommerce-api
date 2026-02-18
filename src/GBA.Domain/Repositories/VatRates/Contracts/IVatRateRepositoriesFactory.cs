using System.Data;

namespace GBA.Domain.Repositories.VatRates.Contracts;

public interface IVatRateRepositoriesFactory {
    IVatRateRepository NewVatRateRepository(IDbConnection connection);
}