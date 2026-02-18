namespace GBA.Domain.Entities.Ecommerce;

public sealed class EcommerceContactInfo : EntityBase {
    public string Address { get; set; }

    public string Phone { get; set; }

    public string Email { get; set; }

    public string SiteUrl { get; set; }

    public string Locale { get; set; }

    public string PixelId { get; set; }
}