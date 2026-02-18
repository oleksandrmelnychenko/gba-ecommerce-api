using Akka.Actor;
using Akka.DI.Core;
using Akka.Routing;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using GBA.Services.Actors.PaymentOrders.AdvancePaymentGetActors;
using GBA.Services.Actors.PaymentOrders.IncomePaymentOrderGetActors;
using GBA.Services.Actors.PaymentOrders.OutcomePaymentOrderGetActors;
using GBA.Services.Actors.PaymentOrders.PaymentCostMovementGetActors;
using GBA.Services.Actors.PaymentOrders.PaymentMovementGetActors;
using GBA.Services.Actors.PaymentOrders.PaymentRegisterCurrencyExchangeGetActors;
using GBA.Services.Actors.PaymentOrders.PaymentRegisterGetActors;
using GBA.Services.Actors.PaymentOrders.PaymentRegisterTransferGetActors;

namespace GBA.Services.Actors.PaymentOrders;

public sealed class PaymentOrdersManagementActor : ReceiveActor {
    public PaymentOrdersManagementActor() {
        ActorReferenceManager.Instance.Add(
            PaymentOrdersActorNames.PAYMENT_REGISTER_ACTOR,
            Context.ActorOf(Context.DI().Props<PaymentRegisterActor>(), PaymentOrdersActorNames.PAYMENT_REGISTER_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            PaymentOrdersActorNames.BASE_PAYMENT_REGISTER_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BasePaymentRegisterGetActor>().WithRouter(new RoundRobinPool(10)),
                PaymentOrdersActorNames.BASE_PAYMENT_REGISTER_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            PaymentOrdersActorNames.INCOME_PAYMENT_ORDER_ACTOR,
            Context.ActorOf(Context.DI().Props<IncomePaymentOrderActor>(), PaymentOrdersActorNames.INCOME_PAYMENT_ORDER_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            PaymentOrdersActorNames.BASE_INCOME_PAYMENT_ORDER_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseIncomePaymentOrderGetActor>().WithRouter(new RoundRobinPool(10)),
                PaymentOrdersActorNames.BASE_INCOME_PAYMENT_ORDER_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            PaymentOrdersActorNames.PAYMENT_MOVEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<PaymentMovementActor>(), PaymentOrdersActorNames.PAYMENT_MOVEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            PaymentOrdersActorNames.BASE_PAYMENT_MOVEMENT_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BasePaymentMovementGetActor>().WithRouter(new RoundRobinPool(10)),
                PaymentOrdersActorNames.BASE_PAYMENT_MOVEMENT_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            PaymentOrdersActorNames.PAYMENT_COST_MOVEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<PaymentCostMovementActor>(), PaymentOrdersActorNames.PAYMENT_COST_MOVEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            PaymentOrdersActorNames.BASE_PAYMENT_COST_MOVEMENT_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BasePaymentCostMovementGetActor>().WithRouter(new RoundRobinPool(10)),
                PaymentOrdersActorNames.BASE_PAYMENT_COST_MOVEMENT_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            PaymentOrdersActorNames.OUTCOME_PAYMENT_ORDER_ACTOR,
            Context.ActorOf(Context.DI().Props<OutcomePaymentOrderActor>(), PaymentOrdersActorNames.OUTCOME_PAYMENT_ORDER_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            PaymentOrdersActorNames.BASE_OUTCOME_PAYMENT_ORDER_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseOutcomePaymentOrderGetActor>().WithRouter(new RoundRobinPool(10)),
                PaymentOrdersActorNames.BASE_OUTCOME_PAYMENT_ORDER_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            PaymentOrdersActorNames.PAYMENT_REGISTER_TRANSFER_ACTOR,
            Context.ActorOf(Context.DI().Props<PaymentRegisterTransferActor>(), PaymentOrdersActorNames.PAYMENT_REGISTER_TRANSFER_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            PaymentOrdersActorNames.BASE_PAYMENT_REGISTER_TRANSFER_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BasePaymentRegisterTransferGetActor>().WithRouter(new RoundRobinPool(10)),
                PaymentOrdersActorNames.BASE_PAYMENT_REGISTER_TRANSFER_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            PaymentOrdersActorNames.PAYMENT_REGISTER_CURRENCY_EXCHANGE_ACTOR,
            Context.ActorOf(Context.DI().Props<PaymentRegisterCurrencyExchangeActor>(), PaymentOrdersActorNames.PAYMENT_REGISTER_CURRENCY_EXCHANGE_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            PaymentOrdersActorNames.BASE_PAYMENT_REGISTER_CURRENCY_EXCHANGE_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BasePaymentRegisterCurrencyExchangeGetActor>().WithRouter(new RoundRobinPool(10)),
                PaymentOrdersActorNames.BASE_PAYMENT_REGISTER_CURRENCY_EXCHANGE_GET_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            PaymentOrdersActorNames.ADVANCE_PAYMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<AdvancePaymentActor>(), PaymentOrdersActorNames.ADVANCE_PAYMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            PaymentOrdersActorNames.BASE_ADVANCE_PAYMENT_GET_ACTOR,
            Context.ActorOf(Context.DI().Props<BaseAdvancePaymentGetActor>().WithRouter(new RoundRobinPool(10)),
                PaymentOrdersActorNames.BASE_ADVANCE_PAYMENT_GET_ACTOR)
        );
    }
}