namespace GBA.Domain.Entities;

public sealed class Bank : EntityBase {
    public string Name { get; set; }

    public string MfoCode { get; set; }

    public string EdrpouCode { get; set; }

    public string City { get; set; }

    public string Address { get; set; }

    public string Phones { get; set; }
}