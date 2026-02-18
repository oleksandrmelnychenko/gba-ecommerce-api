using System.Data;
using GBA.Domain.Repositories.Organizations.Contracts;

namespace GBA.Domain.Repositories.Organizations;

public sealed class OrganizationRepositoriesFactory : IOrganizationRepositoriesFactory {
    public IOrganizationRepository NewOrganizationRepository(IDbConnection connection) {
        return new OrganizationRepository(connection);
    }

    public IOrganizationTranslationRepository NewOrganizationTranslationRepository(IDbConnection connection) {
        return new OrganizationTranslationRepository(connection);
    }
}