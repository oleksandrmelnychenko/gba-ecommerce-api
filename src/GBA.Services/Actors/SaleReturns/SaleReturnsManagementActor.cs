using Akka.Actor;
using Akka.DI.Core;
using Akka.Routing;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using GBA.Services.Actors.SaleReturns.SaleReturnsGetActors;

namespace GBA.Services.Actors.SaleReturns;

public sealed class SaleReturnsManagementActor : ReceiveActor {
    public SaleReturnsManagementActor() {
        ActorReferenceManager.Instance.Add(
            SaleReturnsActorNames.SALE_RETURNS_ACTOR,
            Context.ActorOf(Context.DI().Props<SaleReturnsActor>(), SaleReturnsActorNames.SALE_RETURNS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SaleReturnsActorNames.BASE_SALE_RETURNS_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseSaleReturnsGetActor>().WithRouter(new RoundRobinPool(10)), SaleReturnsActorNames.BASE_SALE_RETURNS_GET_ACTOR)
        );
    }
}