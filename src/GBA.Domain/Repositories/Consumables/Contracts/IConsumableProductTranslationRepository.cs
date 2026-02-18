using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Consumables.Contracts;

public interface IConsumableProductTranslationRepository {
    void Add(ConsumableProductTranslation consumableProductTranslation);

    void Update(ConsumableProductTranslation consumableProductTranslation);

    ConsumableProductTranslation GetByConsumableProductId(long id);
}