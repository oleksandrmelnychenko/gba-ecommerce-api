using Akka.Actor;
using Akka.DI.Core;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Supports;

public sealed class SupportManagementActor : ReceiveActor {
    public SupportManagementActor() {
        ActorReferenceManager.Instance.Add(
            SupportActorNames.SUPPORT_VIDEO_ACTOR,
            Context.ActorOf(Context.DI().Props<SupportVideoActor>(), SupportActorNames.SUPPORT_VIDEO_ACTOR)
        );
    }
}