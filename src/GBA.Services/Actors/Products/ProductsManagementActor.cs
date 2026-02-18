using Akka.Actor;
using Akka.DI.Core;
using Akka.Routing;
using GBA.Domain.Messages.Products.ProductReservations;
using GBA.Domain.Messages.Products.ProductSpecifications;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using GBA.Services.Actors.Products.ProductIncomesGetActors;
using GBA.Services.Actors.Products.ProductReservationsGetActors;
using GBA.Services.Actors.Products.ProductsGetActors;
using GBA.Services.Actors.Products.ProductTransfersGetActors;

namespace GBA.Services.Actors.Products;

public sealed class ProductsManagementActor : ReceiveActor {
    public ProductsManagementActor() {
        ActorReferenceManager.Instance.Add(
            ProductsActorNames.PRODUCTS_ACTOR,
            Context.ActorOf(Context.DI().Props<ProductsActor>(), ProductsActorNames.PRODUCTS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.PRODUCT_GROUPS_ACTOR,
            Context.ActorOf(Context.DI().Props<ProductGroupsActor>(), ProductsActorNames.PRODUCT_GROUPS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.PRODUCT_RESERVATIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<ProductReservationsActor>(), ProductsActorNames.PRODUCT_RESERVATIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.PRODUCT_INCOMES_ACTOR,
            Context.ActorOf(Context.DI().Props<ProductIncomesActor>(), ProductsActorNames.PRODUCT_INCOMES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.BASE_PRODUCT_INCOMES_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseProductIncomesGetActor>().WithRouter(new RoundRobinPool(10)), ProductsActorNames.BASE_PRODUCT_INCOMES_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.PRODUCT_WRITE_OFF_RULES_ACTOR,
            Context.ActorOf(Context.DI().Props<ProductWriteOffRulesActor>(), ProductsActorNames.PRODUCT_WRITE_OFF_RULES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.PRODUCT_TRANSFERS_ACTOR,
            Context.ActorOf(Context.DI().Props<ProductTransfersActor>(), ProductsActorNames.PRODUCT_TRANSFERS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.BASE_PRODUCT_TRANSFERS_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseProductTransfersGetActor>().WithRouter(new RoundRobinPool(5)),
                ProductsActorNames.BASE_PRODUCT_TRANSFERS_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.PRODUCT_PLACEMENT_MOVEMENTS_ACTOR,
            Context.ActorOf(Context.DI().Props<ProductPlacementMovementsActor>(), ProductsActorNames.PRODUCT_PLACEMENT_MOVEMENTS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.PRODUCT_PLACEMENT_STORAGE_ACTOR,
            Context.ActorOf(Context.DI().Props<ProductPlacementStorageActor>(), ProductsActorNames.PRODUCT_PLACEMENT_STORAGE_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.PRODUCT_CAPITALIZATION_ACTOR,
            Context.ActorOf(Context.DI().Props<ProductCapitalizationActor>(), ProductsActorNames.PRODUCT_CAPITALIZATION_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.PRODUCT_MOST_PURCHASED_ACTOR,
            Context.ActorOf(Context.DI().Props<ProductMostPurchasedActor>(), ProductsActorNames.PRODUCT_MOST_PURCHASED_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.PRODUCT_CO_PURCHASE_RECOMMENDATIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<ProductCoPurchaseRecommendationsActor>(), ProductsActorNames.PRODUCT_CO_PURCHASE_RECOMMENDATIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.PRODUCT_SPECIFICATIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<ProductSpecificationsActor>(), ProductsActorNames.PRODUCT_SPECIFICATIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.BASE_PRODUCTS_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseProductsGetActor>().WithRouter(new RoundRobinPool(10)), ProductsActorNames.BASE_PRODUCTS_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.GET_PRODUCTS_FROM_ADVANCED_SEARCH_ACTOR,
            Context.ActorOf(Context.DI().Props<GetProductsAdvancedSearchActor>().WithRouter(new RoundRobinPool(10)), ProductsActorNames.GET_PRODUCTS_FROM_ADVANCED_SEARCH_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.GET_ALL_PRODUCT_AVAILABILITIES_ACTOR,
            Context.ActorOf(Context.DI().Props<GetAllProductAvailabilitiesActor>().WithRouter(new RoundRobinPool(10)), ProductsActorNames.GET_ALL_PRODUCT_AVAILABILITIES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.GET_PRODUCT_WITH_CURRENT_PRICING_BY_AGREEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<GetProductWithCurrentPricingByAgreementActor>().WithRouter(new RoundRobinPool(10)),
                ProductsActorNames.GET_PRODUCT_WITH_CURRENT_PRICING_BY_AGREEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.GET_PRODUCT_WITH_PRICES_AND_DISCOUNTS_ACTOR,
            Context.ActorOf(Context.DI().Props<GetProductWithPricesAndDiscountsActor>().WithRouter(new RoundRobinPool(10)),
                ProductsActorNames.GET_PRODUCT_WITH_PRICES_AND_DISCOUNTS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.GET_CURRENT_RESERVATIONS_BY_PRODUCT_NET_ID_ACTOR,
            Context.ActorOf(Context.DI().Props<GetCurrentReservationsByProductNetIdActor>().WithRouter(new RoundRobinPool(10)),
                ProductsActorNames.GET_CURRENT_RESERVATIONS_BY_PRODUCT_NET_ID_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.GET_CURRENT_RESERVATIONS_BY_PRODUCT_NET_ID_AND_VAT_ACTOR,
            Context.ActorOf(Context.DI().Props<GetCurrentReservationsByProductNetIdAndVatActor>().WithRouter(new RoundRobinPool(10)),
                ProductsActorNames.GET_CURRENT_RESERVATIONS_BY_PRODUCT_NET_ID_AND_VAT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.GET_RESERVATION_INFO_BY_PRODUCT_NET_ID_ACTOR,
            Context.ActorOf(Context.DI().Props<GetReservationInfoByProductNetIdActor>().WithRouter(new RoundRobinPool(10)),
                ProductsActorNames.GET_RESERVATION_INFO_BY_PRODUCT_NET_ID_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            ProductsActorNames.GET_CURRENT_RESERVATIONS_BY_PRODUCT_AND_CLIENT_AGREEMENT_NET_IDS_ACTOR,
            Context.ActorOf(Context.DI().Props<GetCurrentReservationsByProductAndClientAgreementNetIdsActor>().WithRouter(new RoundRobinPool(10)),
                ProductsActorNames.GET_CURRENT_RESERVATIONS_BY_PRODUCT_AND_CLIENT_AGREEMENT_NET_IDS_ACTOR)
        );

        Receive<AddProductReservationMessage>(message => {
            ActorReferenceManager.Instance.Get(ProductsActorNames.PRODUCT_RESERVATIONS_ACTOR).Tell(message);
        });

        Receive<UpdateInvoiceProductSpecificationAssignmentsMessage>(message =>
            ActorReferenceManager.Instance.Get(ProductsActorNames.PRODUCT_SPECIFICATIONS_ACTOR).Forward(message));

        Receive<UpdateSadProductSpecificationAssignmentsMessage>(message => ActorReferenceManager.Instance.Get(ProductsActorNames.PRODUCT_SPECIFICATIONS_ACTOR).Forward(message));
    }
}