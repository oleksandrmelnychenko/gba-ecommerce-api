using System;
using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.AuditEntities;

public class AuditEntity : EntityBase {
    public AuditEntity() {
        OldValues = new HashSet<AuditEntityProperty>();

        NewValues = new HashSet<AuditEntityProperty>();
    }

    public AuditEventType Type { get; set; }

    public Guid BaseEntityNetUid { get; set; }

    public string EntityName { get; set; }

    public string UpdatedBy { get; set; }

    public Guid UpdatedByNetUid { get; set; }

    public virtual ICollection<AuditEntityProperty> OldValues { get; set; }

    public virtual ICollection<AuditEntityProperty> NewValues { get; set; }
}