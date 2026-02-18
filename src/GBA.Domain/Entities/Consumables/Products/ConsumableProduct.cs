using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities.Consumables;

public sealed class ConsumableProduct : EntityBase {
    public ConsumableProduct() {
        ConsumableProductTranslations = new HashSet<ConsumableProductTranslation>();

        ConsumablesOrderItems = new HashSet<ConsumablesOrderItem>();

        PriceTotals = new List<PriceTotal>();

        MergedServices = new HashSet<MergedService>();

        DeliveryExpenses = new HashSet<DeliveryExpense>();
    }

    public string Name { get; set; }

    public string VendorCode { get; set; }

    public long ConsumableProductCategoryId { get; set; }

    public long? MeasureUnitId { get; set; }

    public double TotalQty { get; set; }

    public ConsumableProductCategory ConsumableProductCategory { get; set; }

    public MeasureUnit MeasureUnit { get; set; }

    public ICollection<ConsumableProductTranslation> ConsumableProductTranslations { get; set; }

    public ICollection<ConsumablesOrderItem> ConsumablesOrderItems { get; set; }

    public List<PriceTotal> PriceTotals { get; set; }

    public ICollection<MergedService> MergedServices { get; set; }

    public ICollection<DeliveryExpense> DeliveryExpenses { get; set; }
}