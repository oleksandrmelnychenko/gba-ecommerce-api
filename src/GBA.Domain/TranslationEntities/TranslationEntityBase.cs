using GBA.Domain.Entities;

namespace GBA.Domain.TranslationEntities;

public abstract class TranslationEntityBase : EntityBase {
    public string CultureCode { get; set; }
}