using System.Collections.Generic;
using GBA.Domain.Entities.Agreements;

namespace GBA.Domain.Entities.Pricings;

public sealed class ProviderPricing : EntityBase {
    public ProviderPricing() {
        Agreements = new HashSet<Agreement>();
    }

    public string Name { get; set; }

    public long? CurrencyId { get; set; }

    public long? BasePricingId { get; set; }

    public Currency Currency { get; set; }

    public Pricing Pricing { get; set; }

    public ICollection<Agreement> Agreements { get; set; }
}