using System;
using Akka.DI.Core;
using GBA.Domain.Messages.SchedulerTasks;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using ActorRefImplicitSenderExtensions = Akka.Actor.ActorRefImplicitSenderExtensions;
using ReceiveActor = Akka.Actor.ReceiveActor;

namespace GBA.Services.Actors.SchedulerTasks;

public sealed class SchedulerTasksManagementActor : ReceiveActor {
    public SchedulerTasksManagementActor() {
        ActorReferenceManager.Instance.Add(
            SchedulerTasksActorNames.MERGE_SALES_TASK_ACTOR,
            Context.ActorOf(Context.DI().Props<MergeSalesTaskActor>(), SchedulerTasksActorNames.MERGE_SALES_TASK_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SchedulerTasksActorNames.CLEAR_UNAVAILABLE_CLIENT_SHOPPING_CARTS_ACTOR,
            Context.ActorOf(Context.DI().Props<ClearUnavailableClientShoppingCartsActor>(), SchedulerTasksActorNames.CLEAR_UNAVAILABLE_CLIENT_SHOPPING_CARTS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SchedulerTasksActorNames.CLEAR_CLIENT_AGREEMENTS_NUMBER_DAY_DEBT_ACTOR,
            Context.ActorOf(Context.DI().Props<ClearClientAgreementsNumberDayDebtActor>(), SchedulerTasksActorNames.CLEAR_CLIENT_AGREEMENTS_NUMBER_DAY_DEBT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SchedulerTasksActorNames.UPDATE_PRODUCT_PRICES_TASK_ACTOR,
            Context.ActorOf(Context.DI().Props<UpdateProductPricesTaskActor>(), SchedulerTasksActorNames.UPDATE_PRODUCT_PRICES_TASK_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SchedulerTasksActorNames.UPDATE_UKRAINIAN_STORAGE_PRODUCTS_AVAILABILITY_ACTOR,
            Context.ActorOf(Context.DI().Props<UpdateUkrainianStorageProductsAvailabilityActor>(),
                SchedulerTasksActorNames.UPDATE_UKRAINIAN_STORAGE_PRODUCTS_AVAILABILITY_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SchedulerTasksActorNames.UPDATE_POLISH_STORAGE_PRODUCTS_AVAILABILITY_ACTOR,
            Context.ActorOf(Context.DI().Props<UpdatePolishStorageProductsAvailabilityActor>(), SchedulerTasksActorNames.UPDATE_POLISH_STORAGE_PRODUCTS_AVAILABILITY_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SchedulerTasksActorNames.DELETE_OLD_BILL_SALES_ACTOR,
            Context.ActorOf(Context.DI().Props<DeleteOldBillSalesActor>(), SchedulerTasksActorNames.DELETE_OLD_BILL_SALES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SchedulerTasksActorNames.GENERATE_EXPIRED_BILL_USER_NOTIFICATIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<GenerateExpiredBillUserNotificationsActor>(), SchedulerTasksActorNames.GENERATE_EXPIRED_BILL_USER_NOTIFICATIONS_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            SchedulerTasksActorNames.DEFER_EXPIRED_BILL_USER_NOTIFICATIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<DeferExpiredBillUserNotificationsActor>(), SchedulerTasksActorNames.DEFER_EXPIRED_BILL_USER_NOTIFICATIONS_ACTOR)
        );

        Receive<MergeAllSalesMessage>(message =>
            ActorRefImplicitSenderExtensions.Forward(ActorReferenceManager.Instance.Get(SchedulerTasksActorNames.MERGE_SALES_TASK_ACTOR), message));

        Receive<ClearUnavailableClientShoppingCartsMessage>(message =>
            ActorRefImplicitSenderExtensions.Forward(ActorReferenceManager.Instance.Get(SchedulerTasksActorNames.CLEAR_UNAVAILABLE_CLIENT_SHOPPING_CARTS_ACTOR), message));

        Receive<ClearExpiredOrderItemsInClientShoppingCartsMessage>(message =>
            ActorRefImplicitSenderExtensions.Forward(ActorReferenceManager.Instance.Get(SchedulerTasksActorNames.CLEAR_UNAVAILABLE_CLIENT_SHOPPING_CARTS_ACTOR), message));

        Receive<ClearClientAgreementsNumberDayDebtMessage>(message =>
            ActorRefImplicitSenderExtensions.Forward(ActorReferenceManager.Instance.Get(SchedulerTasksActorNames.CLEAR_CLIENT_AGREEMENTS_NUMBER_DAY_DEBT_ACTOR), message));

        Receive<UpdateProductPricesMessage>(message =>
            ActorRefImplicitSenderExtensions.Forward(ActorReferenceManager.Instance.Get(SchedulerTasksActorNames.UPDATE_PRODUCT_PRICES_TASK_ACTOR), message));

        Receive<UpdateUkrainianStorageProductsAvailabilityMessage>(message =>
            ActorRefImplicitSenderExtensions.Forward(ActorReferenceManager.Instance.Get(SchedulerTasksActorNames.UPDATE_UKRAINIAN_STORAGE_PRODUCTS_AVAILABILITY_ACTOR), message));

        Receive<UpdatePolishStorageProductsAvailabilityMessage>(message =>
            ActorRefImplicitSenderExtensions.Forward(ActorReferenceManager.Instance.Get(SchedulerTasksActorNames.UPDATE_POLISH_STORAGE_PRODUCTS_AVAILABILITY_ACTOR), message));

        Receive<InitiateCloseExpiredOrdersMessage>(message =>
            ActorRefImplicitSenderExtensions.Forward(ActorReferenceManager.Instance.Get(SchedulerTasksActorNames.DELETE_OLD_BILL_SALES_ACTOR), message));

        Receive<GenerateExpiredBillUserNotificationsMessage>(message =>
            ActorRefImplicitSenderExtensions.Forward(ActorReferenceManager.Instance.Get(SchedulerTasksActorNames.GENERATE_EXPIRED_BILL_USER_NOTIFICATIONS_ACTOR), message));

        Receive<DeferExpiredBillUserNotificationsMessage>(message =>
            ActorRefImplicitSenderExtensions.Forward(ActorReferenceManager.Instance.Get(SchedulerTasksActorNames.DEFER_EXPIRED_BILL_USER_NOTIFICATIONS_ACTOR), message));
    }

    protected override void PreStart() {
        TimeSpan currentTime = DateTime.Now.TimeOfDay;
        TimeSpan targetTime = TimeSpan.FromHours(0);
        TimeSpan initialDelay = (targetTime - currentTime).TotalMilliseconds >= 0
            ? targetTime - currentTime
            : targetTime - currentTime + TimeSpan.FromDays(1);

        Context.System.Scheduler.ScheduleTellRepeatedly(
            initialDelay,
            TimeSpan.FromHours(24),
            Self,
            new InitiateCloseExpiredOrdersMessage(),
            Self);

        // Context.System.Scheduler.ScheduleTellRepeatedly(
        //     initialDelay: initialDelay,
        //     interval: TimeSpan.FromHours(24),
        //     receiver: Self,
        //     message: new Clear�lientAgreementsNumberDayDebtMessage(),
        //     sender: Self);

        Context.System.Scheduler.ScheduleTellRepeatedly(
            initialDelay,
            TimeSpan.FromHours(24),
            Self,
            new ClearExpiredOrderItemsInClientShoppingCartsMessage(),
            Self);

        base.PreStart();
    }
}