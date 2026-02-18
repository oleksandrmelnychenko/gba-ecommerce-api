using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.PerfectClientValueTranslations;

public sealed class UpdatePerfectClientValueTranslationMessage {
    public UpdatePerfectClientValueTranslationMessage(PerfectClientValueTranslation perfectClientValueTranslation) {
        PerfectClientValueTranslation = perfectClientValueTranslation;
    }

    public PerfectClientValueTranslation PerfectClientValueTranslation { get; set; }
}