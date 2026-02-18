namespace GBA.Domain.TranslationEntities;

public sealed class AuditEntityPropertyNameTranslation : TranslationEntityBase {
    public string Name { get; set; }

    public string LocalizedName { get; set; }
}