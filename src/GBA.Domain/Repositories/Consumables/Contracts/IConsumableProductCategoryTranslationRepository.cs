using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Consumables.Contracts;

public interface IConsumableProductCategoryTranslationRepository {
    void Add(ConsumableProductCategoryTranslation consumableProductCategoryTranslation);

    void Update(ConsumableProductCategoryTranslation consumableProductCategoryTranslation);

    ConsumableProductCategoryTranslation GetByConsumableProductCategoryId(long id);
}