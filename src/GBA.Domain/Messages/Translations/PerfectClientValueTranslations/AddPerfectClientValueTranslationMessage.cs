using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.PerfectClientValueTranslations;

public sealed class AddPerfectClientValueTranslationMessage {
    public AddPerfectClientValueTranslationMessage(PerfectClientValueTranslation perfectClientValueTranslation) {
        PerfectClientValueTranslation = perfectClientValueTranslation;
    }

    public PerfectClientValueTranslation PerfectClientValueTranslation { get; set; }
}