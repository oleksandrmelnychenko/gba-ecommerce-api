using Akka.Actor;
using Akka.DI.Core;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.Supplies.HelperServices;

public sealed class HelperServiceManagementActor : ReceiveActor {
    public HelperServiceManagementActor() {
        ActorReferenceManager.Instance.Add(
            HelperServiceActorNames.PORT_WORK_SERVICES_ACTOR,
            Context.ActorOf(Context.DI().Props<PortWorkServicesActor>(), HelperServiceActorNames.PORT_WORK_SERVICES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            HelperServiceActorNames.TRANSPORTATION_SERVICES_ACTOR,
            Context.ActorOf(Context.DI().Props<TransportationServicesActor>(), HelperServiceActorNames.TRANSPORTATION_SERVICES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            HelperServiceActorNames.CONTAINER_SERVICES_ACTOR,
            Context.ActorOf(Context.DI().Props<ContainerServicesActor>(), HelperServiceActorNames.CONTAINER_SERVICES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            HelperServiceActorNames.VEHICLE_SERVICES_ACTOR,
            Context.ActorOf(Context.DI().Props<VehicleServicesActor>(), HelperServiceActorNames.VEHICLE_SERVICES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            HelperServiceActorNames.CUSTOM_SERVICES_ACTOR,
            Context.ActorOf(Context.DI().Props<CustomServicesActor>(), HelperServiceActorNames.CUSTOM_SERVICES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            HelperServiceActorNames.PORT_CUSTOM_AGENCY_SERVICES_ACTOR,
            Context.ActorOf(Context.DI().Props<PortCustomAgencyServicesActor>(), HelperServiceActorNames.PORT_CUSTOM_AGENCY_SERVICES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            HelperServiceActorNames.VEHICLE_DELIVERY_SERVICES_ACTOR,
            Context.ActorOf(Context.DI().Props<VehicleDeliveryServicesActor>(), HelperServiceActorNames.VEHICLE_DELIVERY_SERVICES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            HelperServiceActorNames.CUSTOM_AGENCY_SERVICES_ACTOR,
            Context.ActorOf(Context.DI().Props<CustomAgencyServicesActor>(), HelperServiceActorNames.CUSTOM_AGENCY_SERVICES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            HelperServiceActorNames.PLANE_DELIVERY_SERVICES_ACTOR,
            Context.ActorOf(Context.DI().Props<PlaneDeliveryServicesActor>(), HelperServiceActorNames.PLANE_DELIVERY_SERVICES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            HelperServiceActorNames.SERVICE_DETAIL_ITEMS_ACTOR,
            Context.ActorOf(Context.DI().Props<ServiceDetailItemsActor>(), HelperServiceActorNames.SERVICE_DETAIL_ITEMS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            HelperServiceActorNames.MERGED_SERVICES_ACTOR,
            Context.ActorOf(Context.DI().Props<MergedServicesActor>(), HelperServiceActorNames.MERGED_SERVICES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            HelperServiceActorNames.BILL_OF_LADING_SERVICE,
            Context.ActorOf(Context.DI().Props<BillOfLadingServicesActor>(), HelperServiceActorNames.BILL_OF_LADING_SERVICE)
        );
    }
}