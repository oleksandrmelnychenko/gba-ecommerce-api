using Akka.Actor;
using Akka.DI.Core;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Pricings;

public sealed class PricingManagementActor : ReceiveActor {
    public PricingManagementActor() {
        ActorReferenceManager.Instance.Add(
            PricingActorNames.PRICINGS_ACTOR,
            Context.ActorOf(Context.DI().Props<PricingsActor>(), PricingActorNames.PRICINGS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            PricingActorNames.PRICINGS_TYPES_ACTOR,
            Context.ActorOf(Context.DI().Props<PriceTypesActor>(), PricingActorNames.PRICINGS_TYPES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            PricingActorNames.PROVIDER_PRICINGS_ACTOR,
            Context.ActorOf(Context.DI().Props<ProviderPricingsActor>(), PricingActorNames.PROVIDER_PRICINGS_ACTOR)
        );
    }
}