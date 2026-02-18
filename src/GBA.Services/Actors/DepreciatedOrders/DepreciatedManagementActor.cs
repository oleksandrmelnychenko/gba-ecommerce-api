using Akka.Actor;
using Akka.DI.Core;
using Akka.Routing;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using GBA.Services.Actors.DepreciatedOrders.DepreciatedOrdersGetActors;

namespace GBA.Services.Actors.DepreciatedOrders;

public class DepreciatedManagementActor : ReceiveActor {
    public DepreciatedManagementActor() {
        ActorReferenceManager.Instance.Add(
            DepreciatedActorNames.DEPRECIATED_ORDERS_ACTOR,
            Context.ActorOf(Context.DI().Props<DepreciatedOrdersActor>(), DepreciatedActorNames.DEPRECIATED_ORDERS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            DepreciatedActorNames.BASE_DEPRECIATED_ORDERS_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseDepreciatedOrdersGetActor>().WithRouter(new RoundRobinPool(5)),
                DepreciatedActorNames.BASE_DEPRECIATED_ORDERS_GET_ACTOR)
        );
    }
}