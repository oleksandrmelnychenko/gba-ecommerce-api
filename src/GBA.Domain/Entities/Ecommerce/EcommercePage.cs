namespace GBA.Domain.Entities.Ecommerce;

public sealed class EcommercePage : EntityBase {
    public string PageName { get; set; }

    public string TitleUa { get; set; }

    public string TitleRu { get; set; }

    public string DescriptionUa { get; set; }

    public string DescriptionRu { get; set; }

    public string KeyWords { get; set; }

    public string LdJson { get; set; }

    public string UrlUa { get; set; }

    public string UrlRu { get; set; }
}