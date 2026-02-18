using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GBA.Domain.Entities;

public abstract class EntityBase {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public Guid NetUid { get; set; }

    public DateTime Created { get; set; }

    public DateTime Updated { get; set; }

    public bool Deleted { get; set; }

    public virtual bool IsNew() {
        return Id.Equals(0);
    }
}