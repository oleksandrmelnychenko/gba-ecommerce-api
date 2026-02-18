using System.Data;
using GBA.Domain.Repositories.Clients.OrganizationClients.Contracts;

namespace GBA.Domain.Repositories.Clients.OrganizationClients;

public sealed class OrganizationClientRepositoriesFactory : IOrganizationClientRepositoriesFactory {
    public IOrganizationClientRepository NewOrganizationClientRepository(IDbConnection connection) {
        return new OrganizationClientRepository(connection);
    }

    public IOrganizationClientAgreementRepository OrganizationClientAgreementRepository(IDbConnection connection) {
        return new OrganizationClientAgreementRepository(connection);
    }
}