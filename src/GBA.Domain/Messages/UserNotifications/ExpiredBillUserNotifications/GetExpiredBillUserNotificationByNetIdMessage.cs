using System;

namespace GBA.Domain.Messages.UserNotifications.ExpiredBillUserNotifications;

public sealed class GetExpiredBillUserNotificationByNetIdMessage {
    public GetExpiredBillUserNotificationByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}