using System.Data;
using GBA.Domain.Repositories.Countries.Contracts;

namespace GBA.Domain.Repositories.Countries;

public sealed class CountryRepositoryFactory : ICountryRepositoryFactory {
    public ICountryRepository NewCountryRepository(IDbConnection connection) {
        return new CountryRepository(connection);
    }
}