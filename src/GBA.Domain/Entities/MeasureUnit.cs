using System.Collections.Generic;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.Products;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities;

public sealed class MeasureUnit : EntityBase {
    public MeasureUnit() {
        Products = new HashSet<Product>();

        MeasureUnitTranslations = new HashSet<MeasureUnitTranslation>();

        ConsumableProducts = new HashSet<ConsumableProduct>();
    }

    public string Name { get; set; }

    public string NameUk { get; set; }

    public string NamePl { get; set; }

    public string Description { get; set; }

    public string CodeOneC { get; set; }

    public ICollection<Product> Products { get; set; }

    public ICollection<MeasureUnitTranslation> MeasureUnitTranslations { get; set; }

    public ICollection<ConsumableProduct> ConsumableProducts { get; set; }
}