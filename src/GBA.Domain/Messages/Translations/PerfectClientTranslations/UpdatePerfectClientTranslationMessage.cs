using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.PerfectClientTranslations;

public sealed class UpdatePerfectClientTranslationMessage {
    public UpdatePerfectClientTranslationMessage(PerfectClientTranslation perfectClientTranslation) {
        PerfectClientTranslation = perfectClientTranslation;
    }

    public PerfectClientTranslation PerfectClientTranslation { get; set; }
}