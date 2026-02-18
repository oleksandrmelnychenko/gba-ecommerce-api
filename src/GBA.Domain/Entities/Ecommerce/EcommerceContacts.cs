namespace GBA.Domain.Entities.Ecommerce;

public sealed class EcommerceContacts : EntityBase {
    public string Name { get; set; }

    public string Phone { get; set; }

    public string Skype { get; set; }

    public string Icq { get; set; }

    public string Email { get; set; }

    public string ImgUrl { get; set; }
}