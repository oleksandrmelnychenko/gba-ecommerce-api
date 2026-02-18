using Akka.Actor;
using Akka.DI.Core;
using GBA.Domain.Messages.Deliveries.RecipientAddresses;
using GBA.Domain.Messages.Deliveries.Recipients;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Deliveries;

public sealed class DeliveryManagementActor : ReceiveActor {
    public DeliveryManagementActor() {
        ActorReferenceManager.Instance.Add(
            DeliveryActorNames.DELIVERY_RECIPIENTS_ACTOR,
            Context.ActorOf(Context.DI().Props<DeliveryRecipientsActor>(), DeliveryActorNames.DELIVERY_RECIPIENTS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            DeliveryActorNames.DELIVERY_RECIPIENT_ADDRESSES_ACTOR,
            Context.ActorOf(Context.DI().Props<DeliveryRecipientAddressesActor>(), DeliveryActorNames.DELIVERY_RECIPIENT_ADDRESSES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            DeliveryActorNames.TERMS_OF_DELIVERIES_ACTOR,
            Context.ActorOf(Context.DI().Props<TermsOfDeliveriesActor>(), DeliveryActorNames.TERMS_OF_DELIVERIES_ACTOR)
        );

        Receive<ChangeDeliveryRecipientPriorityMessage>(message => ActorReferenceManager.Instance.Get(DeliveryActorNames.DELIVERY_RECIPIENTS_ACTOR).Tell(message));

        Receive<ChangeDeliveryRecipientAddressPriorityMessage>(message =>
            ActorReferenceManager.Instance.Get(DeliveryActorNames.DELIVERY_RECIPIENT_ADDRESSES_ACTOR).Tell(message));
    }
}