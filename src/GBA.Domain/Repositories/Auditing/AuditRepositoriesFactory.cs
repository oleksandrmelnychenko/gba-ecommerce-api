using System.Data;
using GBA.Domain.Repositories.Auditing.Contracts;

namespace GBA.Domain.Repositories.Auditing;

public sealed class AuditRepositoriesFactory : IAuditRepositoriesFactory {
    public IAuditRepository NewAuditRepository(IDbConnection connection) {
        return new AuditRepository(connection);
    }

    public IAuditPropertiesRepository NewAuditPropertiesRepository(IDbConnection connection) {
        return new AuditPropertiesRepository(connection);
    }
}