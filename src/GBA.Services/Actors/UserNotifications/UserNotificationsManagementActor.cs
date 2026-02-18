using Akka.Actor;
using Akka.DI.Core;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.UserNotifications;

public sealed class UserNotificationsManagementActor : ReceiveActor {
    public UserNotificationsManagementActor() {
        ActorReferenceManager.Instance.Add(
            UserNotificationsActorNames.EXPIRED_BILL_USER_NOTIFICATIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<ExpiredBillUserNotificationsActor>(), UserNotificationsActorNames.EXPIRED_BILL_USER_NOTIFICATIONS_ACTOR)
        );
    }
}