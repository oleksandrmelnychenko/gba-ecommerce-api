using GBA.Domain.Entities;

namespace GBA.Domain.EntityHelpers;

public sealed class PriceTotal {
    public decimal TotalPrice { get; set; }

    public Currency Currency { get; set; }
}