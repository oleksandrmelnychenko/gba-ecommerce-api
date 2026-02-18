namespace GBA.Domain.Entities.Sales;

public sealed class SaleNumber : EntityBase {
    public string Value { get; set; }

    public long OrganizationId { get; set; }

    public Organization Organization { get; set; }
}