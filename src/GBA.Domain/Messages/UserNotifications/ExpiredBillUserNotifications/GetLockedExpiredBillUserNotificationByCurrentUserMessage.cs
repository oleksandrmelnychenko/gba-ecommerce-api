using System;

namespace GBA.Domain.Messages.UserNotifications.ExpiredBillUserNotifications;

public sealed class GetLockedExpiredBillUserNotificationByCurrentUserMessage {
    public GetLockedExpiredBillUserNotificationByCurrentUserMessage(Guid userNetId) {
        UserNetId = userNetId;
    }

    public Guid UserNetId { get; }
}