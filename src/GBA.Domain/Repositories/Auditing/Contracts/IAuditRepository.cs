using System;
using System.Collections.Generic;
using GBA.Domain.AuditEntities;

namespace GBA.Domain.Repositories.Auditing.Contracts;

public interface IAuditRepository {
    long Add(AuditEntity auditEntity);

    List<AuditEntity> GetAllByBaseEntityNetUid(Guid netId);

    List<AuditEntity> GetProductChangeHistoryByNetUid(Guid netId);

    List<AuditEntity> GetAllByNetIdLimited(Guid netId, long limit, long offset);

    List<AuditEntity> GetAllByNetIdAndSpecificFieldLimited(Guid netId, long limit, long offset, string fieldName);
}