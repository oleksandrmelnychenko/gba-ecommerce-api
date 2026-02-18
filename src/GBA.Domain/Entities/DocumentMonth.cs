using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities;

public sealed class DocumentMonth : TranslationEntityBase {
    public string Name { get; set; }

    public int Number { get; set; }
}