namespace GBA.Domain.Entities.Products;

public sealed class ProductWriteOffRule : EntityBase {
    public string RuleLocale { get; set; }

    public ProductWriteOffRuleType RuleType { get; set; }

    public long CreatedById { get; set; }

    public long? UpdatedById { get; set; }

    public long? ProductId { get; set; }

    public long? ProductGroupId { get; set; }

    public User CreatedBy { get; set; }

    public User UpdatedBy { get; set; }

    public Product Product { get; set; }

    public ProductGroup ProductGroup { get; set; }
}