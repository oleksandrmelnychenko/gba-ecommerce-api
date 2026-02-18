using System.Collections.Generic;
using GBA.Domain.AuditEntities;

namespace GBA.Domain.Repositories.Auditing.Contracts;

public interface IAuditPropertiesRepository {
    void Add(IEnumerable<AuditEntityProperty> properties);
}