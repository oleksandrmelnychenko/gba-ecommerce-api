using GBA.Domain.Entities;

namespace GBA.Domain.AuditEntities;

public class AuditEntityProperty : EntityBase {
    public AuditEntityPropertyType Type { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string Value { get; set; }

    public long AuditEntityId { get; set; }

    public virtual AuditEntity AuditEntity { get; set; }
}