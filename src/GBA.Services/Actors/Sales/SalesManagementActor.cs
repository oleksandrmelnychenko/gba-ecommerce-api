using Akka.Actor;
using Akka.DI.Core;
using Akka.Routing;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using GBA.Services.Actors.Sales.SalesGetActors;

namespace GBA.Services.Actors.Sales;

public sealed class SalesManagementActor : ReceiveActor {
    public SalesManagementActor() {
        ActorReferenceManager.Instance.Add(
            SalesActorNames.SALES_ACTOR,
            Context.ActorOf(Context.DI().Props<SalesActor>(), SalesActorNames.SALES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SalesActorNames.DEBTS_ACTOR,
            Context.ActorOf(Context.DI().Props<DebtsActor>(), SalesActorNames.DEBTS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SalesActorNames.ORDERS_ACTOR,
            Context.ActorOf(Context.DI().Props<OrdersActor>(), SalesActorNames.ORDERS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SalesActorNames.ORDER_ITEMS_ACTOR,
            Context.ActorOf(Context.DI().Props<OrderItemsActor>(), SalesActorNames.ORDER_ITEMS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SalesActorNames.SALE_OFFERS_ACTOR,
            Context.ActorOf(Context.DI().Props<SaleOffersActor>(), SalesActorNames.SALE_OFFERS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SalesActorNames.CLIENT_SHOPPING_CARTS_ACTOR,
            Context.ActorOf(Context.DI().Props<ClientShoppingCartsActor>(), SalesActorNames.CLIENT_SHOPPING_CARTS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SalesActorNames.PRE_ORDERS_ACTOR,
            Context.ActorOf(Context.DI().Props<PreOrdersActor>(), SalesActorNames.PRE_ORDERS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SalesActorNames.SHIPMENT_LISTS_ACTOR,
            Context.ActorOf(Context.DI().Props<ShipmentListsActor>(), SalesActorNames.SHIPMENT_LISTS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SalesActorNames.SALE_PREDICTION_ACTOR,
            Context.ActorOf(Context.DI().Props<SalePredictionActor>(), SalesActorNames.SALE_PREDICTION_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SalesActorNames.GET_SALE_BY_NET_ID_ACTOR,
            Context.ActorOf(Context.DI().Props<GetSaleByNetIdActor>().WithRouter(new RoundRobinPool(10)), SalesActorNames.GET_SALE_BY_NET_ID_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SalesActorNames.GET_SALES_FILTERED_ACTOR,
            Context.ActorOf(Context.DI().Props<GetSalesFilteredActor>().WithRouter(new RoundRobinPool(10)), SalesActorNames.GET_SALES_FILTERED_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SalesActorNames.GET_ALL_SALE_FUTURE_RESERVATIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<GetAllSaleFutureReservationsActor>().WithRouter(new RoundRobinPool(10)), SalesActorNames.GET_ALL_SALE_FUTURE_RESERVATIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SalesActorNames.GET_ALL_SALES_BY_CLIENT_NET_ID_ACTOR,
            Context.ActorOf(Context.DI().Props<GetAllSalesByClientNetIdActor>().WithRouter(new RoundRobinPool(10)), SalesActorNames.GET_ALL_SALES_BY_CLIENT_NET_ID_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SalesActorNames.BASE_SALES_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseSalesGetActor>().WithRouter(new RoundRobinPool(10)), SalesActorNames.BASE_SALES_GET_ACTOR)
        );
    }
}