using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.EntityHelpers.Supplies;

public sealed class UkraineOrderValidation {
    public UkraineOrderValidation() {
        UkraineOrderValidationItems = new HashSet<UkraineOrderValidationItem>();
    }

    public bool HasErrors { get; set; }

    public SupplyOrderUkraine SupplyOrderUkraine { get; set; }

    public ICollection<UkraineOrderValidationItem> UkraineOrderValidationItems { get; set; }
}