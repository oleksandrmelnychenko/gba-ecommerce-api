using System;

namespace GBA.Domain.Messages.UserNotifications.ExpiredBillUserNotifications;

public sealed class DeferExpiredBillUserNotificationByNetIdMessage {
    public DeferExpiredBillUserNotificationByNetIdMessage(Guid notificationNetId, Guid userNetId) {
        NotificationNetId = notificationNetId;

        UserNetId = userNetId;
    }

    public Guid NotificationNetId { get; }

    public Guid UserNetId { get; }
}