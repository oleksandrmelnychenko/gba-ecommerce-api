using System.Data;

namespace GBA.Domain.Repositories.Organizations.Contracts;

public interface IOrganizationRepositoriesFactory {
    IOrganizationRepository NewOrganizationRepository(IDbConnection connection);

    IOrganizationTranslationRepository NewOrganizationTranslationRepository(IDbConnection connection);
}