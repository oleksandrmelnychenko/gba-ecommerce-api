using Akka.Actor;
using Akka.DI.Core;
using Akka.Routing;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using GBA.Services.Actors.Supplies.ActProvidingServices;
using GBA.Services.Actors.Supplies.DeliveryProductProtocols;
using GBA.Services.Actors.Supplies.HelperServices;
using GBA.Services.Actors.Supplies.PackingListsGetActors;
using GBA.Services.Actors.Supplies.SupplyInvoicesGetActors;
using GBA.Services.Actors.Supplies.SupplyOrderItemsGetActors;
using GBA.Services.Actors.Supplies.SupplyOrdersGetActors;
using GBA.Services.Actors.Supplies.SupplyOrganizationGetActors;
using GBA.Services.Actors.Supplies.SupplyReturnsGetActors;
using GBA.Services.Actors.Supplies.Ukraine;

namespace GBA.Services.Actors.Supplies;

public sealed class SupplyManagementActor : ReceiveActor {
    public SupplyManagementActor() {
        ActorReferenceManager.Instance.Add(
            SupplyActorNames.SUPPLY_ORDER_ACTOR,
            Context.ActorOf(Context.DI().Props<SupplyOrdersActor>(), SupplyActorNames.SUPPLY_ORDER_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.SUPPLY_PRO_FORM_ACTOR,
            Context.ActorOf(Context.DI().Props<SupplyProFormsActor>(), SupplyActorNames.SUPPLY_PRO_FORM_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.SUPPLY_INVOICE_ACTOR,
            Context.ActorOf(Context.DI().Props<SupplyInvoicesActor>(), SupplyActorNames.SUPPLY_INVOICE_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.SUPPLY_DELIVERY_DOCUMENTS_ACTOR,
            Context.ActorOf(Context.DI().Props<SupplyDeliveryDocumentsActor>(), SupplyActorNames.SUPPLY_DELIVERY_DOCUMENTS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.HELPER_SERVICE_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<HelperServiceManagementActor>(), SupplyActorNames.HELPER_SERVICE_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.SUPPLY_ORDER_ITEMS_ACTOR,
            Context.ActorOf(Context.DI().Props<SupplyOrderItemsActor>(), SupplyActorNames.SUPPLY_ORDER_ITEMS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.PACKING_LIST_ACTOR,
            Context.ActorOf(Context.DI().Props<PackingListsActor>(), SupplyActorNames.PACKING_LIST_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.SUPPLY_SERVICES_SEARCH_ACTOR,
            Context.ActorOf(Context.DI().Props<SupplyServicesSearchActor>().WithRouter(new RoundRobinPool(10)), SupplyActorNames.SUPPLY_SERVICES_SEARCH_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.GROUPED_PAYMENT_TASKS_ACTOR,
            Context.ActorOf(Context.DI().Props<GroupedPaymentTasksActor>().WithRouter(new RoundRobinPool(10)), SupplyActorNames.GROUPED_PAYMENT_TASKS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.SUPPLY_ORGANIZATION_ACTOR,
            Context.ActorOf(Context.DI().Props<SupplyOrganizationActor>(), SupplyActorNames.SUPPLY_ORGANIZATION_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.SUPPLY_PAYMENT_TASKS_ACTOR,
            Context.ActorOf(Context.DI().Props<SupplyPaymentTasksActor>(), SupplyActorNames.SUPPLY_PAYMENT_TASKS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.SUPPLY_UKRAINE_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<SupplyUkraineManagementActor>(), SupplyActorNames.SUPPLY_UKRAINE_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.SUPPLY_RETURNS_ACTOR,
            Context.ActorOf(Context.DI().Props<SupplyReturnsActor>(), SupplyActorNames.SUPPLY_RETURNS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.DELIVERY_PRODUCT_PROTOCOL,
            Context.ActorOf(Context.DI().Props<DeliveryProductProtocolActor>(), SupplyActorNames.DELIVERY_PRODUCT_PROTOCOL)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.ACT_PROVIDING_SERVICE,
            Context.ActorOf(Context.DI().Props<ActProvidingServiceActor>(), SupplyActorNames.ACT_PROVIDING_SERVICE)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.BASE_PACKING_LISTS_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BasePackingListsGetActor>().WithRouter(new RoundRobinPool(10)), SupplyActorNames.BASE_PACKING_LISTS_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.BASE_SUPPLY_INVOICES_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseSupplyInvoicesGetActor>().WithRouter(new RoundRobinPool(10)), SupplyActorNames.BASE_SUPPLY_INVOICES_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.BASE_SUPPLY_ORDERS_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseSupplyOrdersGetActor>().WithRouter(new RoundRobinPool(10)), SupplyActorNames.BASE_SUPPLY_ORDERS_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.BASE_SUPPLY_ORDER_ITEMS_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseSupplyOrderItemsGetActor>().WithRouter(new RoundRobinPool(10)), SupplyActorNames.BASE_SUPPLY_ORDER_ITEMS_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.BASE_SUPPLY_ORGANIZATION_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseSupplyOrganizationGetActor>().WithRouter(new RoundRobinPool(10)), SupplyActorNames.BASE_SUPPLY_ORGANIZATION_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SupplyActorNames.BASE_SUPPLY_RETURNS_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseSupplyReturnsGetActor>().WithRouter(new RoundRobinPool(10)), SupplyActorNames.BASE_SUPPLY_RETURNS_GET_ACTOR)
        );
    }
}