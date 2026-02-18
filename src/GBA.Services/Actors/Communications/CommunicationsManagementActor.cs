using Akka.Actor;
using Akka.DI.Core;
using Akka.Routing;
using GBA.Domain.Messages.Communications.Mails;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Communications;

public sealed class CommunicationsManagementActor : ReceiveActor {
    public CommunicationsManagementActor() {
        ActorReferenceManager.Instance.Add(
            CommunicationsActorNames.MAILS_SENDER_ACTOR,
            Context.ActorOf(Context.DI().Props<MailsSenderActor>().WithRouter(new RoundRobinPool(10)), CommunicationsActorNames.MAILS_SENDER_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            CommunicationsActorNames.HUBS_SENDER_ACTOR,
            Context.ActorOf(Context.DI().Props<HubsSenderActor>().WithRouter(new RoundRobinPool(10)), CommunicationsActorNames.HUBS_SENDER_ACTOR)
        );

        Receive<SendNewECommerceOrderNotificationMessage>(message => ActorReferenceManager.Instance.Get(CommunicationsActorNames.MAILS_SENDER_ACTOR).Forward(message));
    }
}