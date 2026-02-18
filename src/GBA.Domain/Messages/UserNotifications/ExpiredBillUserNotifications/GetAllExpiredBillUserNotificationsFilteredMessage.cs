using System;

namespace GBA.Domain.Messages.UserNotifications.ExpiredBillUserNotifications;

public sealed class GetAllExpiredBillUserNotificationsFilteredMessage {
    public GetAllExpiredBillUserNotificationsFilteredMessage(Guid userNetId, string value, bool orderByManager, bool withProcessed) {
        UserNetId = userNetId;

        Value = string.IsNullOrEmpty(value) ? string.Empty : value;

        OrderByManager = orderByManager;

        WithProcessed = withProcessed;
    }

    public Guid UserNetId { get; }

    public string Value { get; }

    public bool OrderByManager { get; }

    public bool WithProcessed { get; }
}