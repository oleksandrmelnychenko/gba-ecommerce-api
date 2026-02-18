using Akka.Actor;
using Akka.DI.Core;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Supplies.Ukraine;

public sealed class SupplyUkraineManagementActor : ReceiveActor {
    public SupplyUkraineManagementActor() {
        ActorReferenceManager.Instance.Add(
            SupplyUkraineActorNames.CARRIERS_ACTOR,
            Context.ActorOf(Context.DI().Props<CarrierStathamActor>(), SupplyUkraineActorNames.CARRIERS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyUkraineActorNames.SUPPLY_ORDER_UKRAINES_ACTOR,
            Context.ActorOf(Context.DI().Props<SupplyOrderUkrainesActor>(), SupplyUkraineActorNames.SUPPLY_ORDER_UKRAINES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyUkraineActorNames.ACT_RECONCILIATIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<ActReconciliationsActor>(), SupplyUkraineActorNames.ACT_RECONCILIATIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyUkraineActorNames.SUPPLY_ORDER_UKRAINE_CART_ITEMS_ACTOR,
            Context.ActorOf(Context.DI().Props<SupplyOrderUkraineCartItemsActor>(), SupplyUkraineActorNames.SUPPLY_ORDER_UKRAINE_CART_ITEMS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyUkraineActorNames.SUPPLY_ORDER_UKRAINE_CART_ITEMS_RECOMMENDATIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<SupplyOrderUkraineCartItemsRecommendationsActor>(),
                SupplyUkraineActorNames.SUPPLY_ORDER_UKRAINE_CART_ITEMS_RECOMMENDATIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyUkraineActorNames.TAX_FREE_PACK_LISTS_ACTOR,
            Context.ActorOf(Context.DI().Props<TaxFreePackListsActor>(), SupplyUkraineActorNames.TAX_FREE_PACK_LISTS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyUkraineActorNames.TAX_FREES_ACTOR,
            Context.ActorOf(Context.DI().Props<TaxFreesActor>(), SupplyUkraineActorNames.TAX_FREES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyUkraineActorNames.DYNAMIC_PRODUCT_PLACEMENT_ROWS_ACTOR,
            Context.ActorOf(Context.DI().Props<DynamicProductPlacementRowsActor>(), SupplyUkraineActorNames.DYNAMIC_PRODUCT_PLACEMENT_ROWS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyUkraineActorNames.SADS_ACTOR,
            Context.ActorOf(Context.DI().Props<SadsActor>(), SupplyUkraineActorNames.SADS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyUkraineActorNames.SAD_PALLET_TYPES_ACTOR,
            Context.ActorOf(Context.DI().Props<SadPalletTypesActor>(), SupplyUkraineActorNames.SAD_PALLET_TYPES_ACTOR)
        );
    }
}