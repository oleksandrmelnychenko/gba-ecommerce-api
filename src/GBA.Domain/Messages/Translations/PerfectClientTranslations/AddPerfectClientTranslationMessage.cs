using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Messages.Translations.PerfectClientTranslations;

public sealed class AddPerfectClientTranslationMessage {
    public AddPerfectClientTranslationMessage(PerfectClientTranslation perfectClientTranslation) {
        PerfectClientTranslation = perfectClientTranslation;
    }

    public PerfectClientTranslation PerfectClientTranslation { get; set; }
}