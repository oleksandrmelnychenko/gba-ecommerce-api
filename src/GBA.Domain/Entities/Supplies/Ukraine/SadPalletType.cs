using System.Collections.Generic;

namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class SadPalletType : EntityBase {
    public SadPalletType() {
        SadPallets = new HashSet<SadPallet>();
    }

    public string Name { get; set; }

    public string CssClass { get; set; }

    public double Weight { get; set; }

    public ICollection<SadPallet> SadPallets { get; set; }
}