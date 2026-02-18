using System.Data;

namespace GBA.Domain.Repositories.Auditing.Contracts;

public interface IAuditRepositoriesFactory {
    IAuditRepository NewAuditRepository(IDbConnection connection);

    IAuditPropertiesRepository NewAuditPropertiesRepository(IDbConnection connection);
}