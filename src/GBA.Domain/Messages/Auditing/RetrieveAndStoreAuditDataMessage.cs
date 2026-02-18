using System;
using System.Collections.Generic;
using GBA.Domain.AuditEntities;

namespace GBA.Domain.Messages.Auditing;

public sealed class RetrieveAndStoreAuditDataMessage {
    public RetrieveAndStoreAuditDataMessage(
        Guid updatedBy,
        Guid entityNetId,
        string entityName,
        object newEntity,
        object oldEntity = null,
        List<AuditEntityProperty> predefinedNewProperties = null,
        List<AuditEntityProperty> predefinedOldProperties = null,
        bool isRemove = false) {
        UpdatedBy = updatedBy;

        EntityNetId = entityNetId;

        EntityName = entityName;

        NewEntity = newEntity;

        OldEntity = oldEntity;

        PerdefinedNewProperties = predefinedNewProperties;

        PerdefinedOldProperties = predefinedOldProperties;

        IsRemove = isRemove;
    }

    public object OldEntity { get; set; }

    public object NewEntity { get; set; }

    public string EntityName { get; set; }

    public Guid UpdatedBy { get; set; }

    public Guid EntityNetId { get; set; }

    public bool IsRemove { get; set; }

    public List<AuditEntityProperty> PerdefinedNewProperties { get; set; }

    public List<AuditEntityProperty> PerdefinedOldProperties { get; set; }
}