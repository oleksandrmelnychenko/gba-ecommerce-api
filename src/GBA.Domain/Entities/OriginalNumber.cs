using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Entities;

public sealed class OriginalNumber : EntityBase {
    public OriginalNumber() {
        ProductOriginalNumbers = new HashSet<ProductOriginalNumber>();
    }

    public string MainNumber { get; set; }

    public string Number { get; set; }

    public ICollection<ProductOriginalNumber> ProductOriginalNumbers { get; set; }
}