using System.Data;

namespace GBA.Domain.Repositories.Clients.OrganizationClients.Contracts;

public interface IOrganizationClientRepositoriesFactory {
    IOrganizationClientRepository NewOrganizationClientRepository(IDbConnection connection);

    IOrganizationClientAgreementRepository OrganizationClientAgreementRepository(IDbConnection connection);
}