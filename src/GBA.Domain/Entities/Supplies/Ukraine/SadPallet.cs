using System.Collections.Generic;

namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class SadPallet : EntityBase {
    public SadPallet() {
        SadPalletItems = new HashSet<SadPalletItem>();
    }

    public decimal TotalAmount { get; set; }

    public decimal TotalAmountLocal { get; set; }

    public double TotalNetWeight { get; set; }

    public double TotalGrossWeight { get; set; }

    public string Number { get; set; }

    public string Comment { get; set; }

    public long SadId { get; set; }

    public long SadPalletTypeId { get; set; }

    public Sad Sad { get; set; }

    public SadPalletType SadPalletType { get; set; }

    public ICollection<SadPalletItem> SadPalletItems { get; set; }
}