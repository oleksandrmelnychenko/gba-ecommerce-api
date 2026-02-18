namespace GBA.Domain.Entities.Ecommerce;

public sealed class SeoPage : EntityBase {
    public string PageName { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public string KeyWords { get; set; }

    public string LdJson { get; set; }

    public string Url { get; set; }

    public string Locale { get; set; }
}