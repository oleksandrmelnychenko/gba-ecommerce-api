using Akka.Actor;
using Akka.DI.Core;
using GBA.Domain.Messages.Transporters;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Transporters;

public sealed class TransporterManagementActor : ReceiveActor {
    public TransporterManagementActor() {
        ActorReferenceManager.Instance.Add(
            TransporterActorNames.TRANSPORTERS_ACTOR,
            Context.ActorOf(Context.DI().Props<TransportersActor>(), TransporterActorNames.TRANSPORTERS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            TransporterActorNames.TRANSPORTER_TYPES_ACTOR,
            Context.ActorOf(Context.DI().Props<TransporterTypesActor>(), TransporterActorNames.TRANSPORTER_TYPES_ACTOR)
        );

        Receive<ChangeTransporterPriorityMessage>(message => ActorReferenceManager.Instance.Get(TransporterActorNames.TRANSPORTERS_ACTOR).Tell(message));
    }
}