using Akka.Actor;
using Akka.DI.Core;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Regions;

public sealed class RegionsManagementActor : ReceiveActor {
    public RegionsManagementActor() {
        ActorReferenceManager.Instance.Add(
            RegionsActorNames.REGIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<RegionsActor>(), RegionsActorNames.REGIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            RegionsActorNames.REGION_CODES_ACTOR,
            Context.ActorOf(Context.DI().Props<RegionCodesActor>(), RegionsActorNames.REGION_CODES_ACTOR)
        );
    }
}