using System.Data;

namespace GBA.Domain.Repositories.Countries.Contracts;

public interface ICountryRepositoryFactory {
    ICountryRepository NewCountryRepository(IDbConnection connection);
}