using Akka.Actor;
using Akka.DI.Core;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Consumables;

public sealed class ConsumablesManagementActor : ReceiveActor {
    public ConsumablesManagementActor() {
        ActorReferenceManager.Instance.Add(
            ConsumablesActorNames.CONSUMABLE_PRODUCT_CATEGORY_ACTOR,
            Context.ActorOf(Context.DI().Props<ConsumableProductCategoryActor>(), ConsumablesActorNames.CONSUMABLE_PRODUCT_CATEGORY_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ConsumablesActorNames.CONSUMABLE_PRODUCT_ACTOR,
            Context.ActorOf(Context.DI().Props<ConsumableProductActor>(), ConsumablesActorNames.CONSUMABLE_PRODUCT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ConsumablesActorNames.CONSUMABLE_ORDER_ACTOR,
            Context.ActorOf(Context.DI().Props<ConsumablesOrderActor>(), ConsumablesActorNames.CONSUMABLE_ORDER_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ConsumablesActorNames.DEPRECIATED_CONSUMABLE_ORDER_ACTOR,
            Context.ActorOf(Context.DI().Props<DepreciatedConsumableOrderActor>(), ConsumablesActorNames.DEPRECIATED_CONSUMABLE_ORDER_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ConsumablesActorNames.COSUMABLES_STORAGE_ACTOR,
            Context.ActorOf(Context.DI().Props<ConsumablesStorageActor>(), ConsumablesActorNames.COSUMABLES_STORAGE_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ConsumablesActorNames.COMPANY_CAR_ACTOR,
            Context.ActorOf(Context.DI().Props<CompanyCarActor>(), ConsumablesActorNames.COMPANY_CAR_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ConsumablesActorNames.COMPANY_CAR_ROAD_LIST_ACTOR,
            Context.ActorOf(Context.DI().Props<CompanyCarRoadListActor>(), ConsumablesActorNames.COMPANY_CAR_ROAD_LIST_ACTOR)
        );
    }
}