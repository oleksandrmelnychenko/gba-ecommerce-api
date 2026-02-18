using Akka.Actor;
using Akka.DI.Core;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.AllegroServices;

public sealed class AllegroServicesManagementActor : ReceiveActor {
    public AllegroServicesManagementActor() {
        ActorReferenceManager.Instance.Add(
            AllegroServicesActorNames.ALLEGRO_WEB_API_ACTOR,
            Context.ActorOf(Context.DI().Props<AllegroWebApiActor>(), AllegroServicesActorNames.ALLEGRO_WEB_API_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            AllegroServicesActorNames.ALLEGRO_WEB_API_SANDBOX_ACTOR,
            Context.ActorOf(Context.DI().Props<AllegroWebApiSandboxActor>(), AllegroServicesActorNames.ALLEGRO_WEB_API_SANDBOX_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            AllegroServicesActorNames.ALLEGRO_CATEGORIES_ACTOR,
            Context.ActorOf(Context.DI().Props<AllegroCategoriesActor>(), AllegroServicesActorNames.ALLEGRO_CATEGORIES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            AllegroServicesActorNames.ALLEGRO_SELLING_ACTOR,
            Context.ActorOf(Context.DI().Props<AllegroSellingActor>(), AllegroServicesActorNames.ALLEGRO_SELLING_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            AllegroServicesActorNames.ALLEGRO_PRODUCT_RESERVATIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<AllegroProductReservationsActor>(), AllegroServicesActorNames.ALLEGRO_PRODUCT_RESERVATIONS_ACTOR)
        );
    }
}