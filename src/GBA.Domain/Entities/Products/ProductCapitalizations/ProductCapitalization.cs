using System;
using System.Collections.Generic;

namespace GBA.Domain.Entities.Products;

public sealed class ProductCapitalization : EntityBase {
    public ProductCapitalization() {
        ProductCapitalizationItems = new HashSet<ProductCapitalizationItem>();
    }

    public string Number { get; set; }

    public string Comment { get; set; }

    public DateTime FromDate { get; set; }

    public long OrganizationId { get; set; }

    public long ResponsibleId { get; set; }

    public long StorageId { get; set; }

    public decimal TotalAmount { get; set; }

    public Organization Organization { get; set; }

    public User Responsible { get; set; }

    public Storage Storage { get; set; }

    public ICollection<ProductCapitalizationItem> ProductCapitalizationItems { get; set; }

    // Ignored
    public Currency Currency { get; set; }
    public decimal ExchangeRate { get; set; }
}