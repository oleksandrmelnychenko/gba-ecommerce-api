using Akka.Actor;
using GBA.Domain.Messages.AllegroServices.Selling;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.AllegroServices;

public sealed class AllegroSellingActor : ReceiveActor {
    public AllegroSellingActor() {
        Receive<GetMySellingItemsMessage>(message => {
            ActorReferenceManager.Instance.Get(AllegroServicesActorNames.ALLEGRO_WEB_API_ACTOR).Forward(message);
        });

        Receive<AddNewSellingItemMessage>(message => {
            ActorReferenceManager.Instance.Get(AllegroServicesActorNames.ALLEGRO_WEB_API_ACTOR).Forward(message);
        });

        Receive<CheckNewSellingItemMessage>(message => {
            ActorReferenceManager.Instance.Get(AllegroServicesActorNames.ALLEGRO_WEB_API_ACTOR).Forward(message);
        });

        Receive<GetSellFormFieldsByCategoryIdMessage>(message => {
            ActorReferenceManager.Instance.Get(AllegroServicesActorNames.ALLEGRO_WEB_API_ACTOR).Forward(message);
        });
    }
}