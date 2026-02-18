using Akka.Actor;
using Akka.DI.Core;
using GBA.Domain.Messages.Auditing;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Auditing;

public sealed class AuditManagementActor : ReceiveActor {
    public AuditManagementActor() {
        ActorReferenceManager.Instance.Add(
            AuditActorNames.AUDITING_ACTOR,
            Context.ActorOf(Context.DI().Props<AuditingActor>(), AuditActorNames.AUDITING_ACTOR)
        );

        Receive<RetrieveAndStoreAuditDataMessage>(message => {
            ActorReferenceManager.Instance.Get(AuditActorNames.AUDITING_ACTOR).Tell(message);
        });
    }
}