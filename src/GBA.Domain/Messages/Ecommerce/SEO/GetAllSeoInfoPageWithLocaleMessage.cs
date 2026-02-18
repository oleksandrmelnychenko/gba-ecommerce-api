namespace GBA.Domain.Messages.Ecommerce.SEO;

public sealed class GetAllSeoInfoPageWithLocaleMessage {
    public GetAllSeoInfoPageWithLocaleMessage(string locale) {
        Locale = locale;
    }

    public string Locale { get; }
}