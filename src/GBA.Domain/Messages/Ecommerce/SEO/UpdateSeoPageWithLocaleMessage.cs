using GBA.Domain.Entities.Ecommerce;

namespace GBA.Domain.Messages.Ecommerce.SEO;

public sealed class UpdateSeoPageWithLocaleMessage {
    public UpdateSeoPageWithLocaleMessage(SeoPage seoPage) {
        SeoPage = seoPage;
    }

    public SeoPage SeoPage { get; }
}