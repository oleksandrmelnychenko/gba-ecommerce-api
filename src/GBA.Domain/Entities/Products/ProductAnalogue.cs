namespace GBA.Domain.Entities.Products;

public sealed class ProductAnalogue : EntityBase {
    public long BaseProductId { get; set; }

    public long AnalogueProductId { get; set; }

    public Product BaseProduct { get; set; }

    public Product AnalogueProduct { get; set; }
}