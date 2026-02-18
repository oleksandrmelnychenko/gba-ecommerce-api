using System;
using System.Collections.Generic;
using GBA.Domain.Entities.UserNotifications;

namespace GBA.Domain.Repositories.UserNotifications.Contracts;

public interface IExpiredBillUserNotificationRepository {
    long Add(ExpiredBillUserNotification notification);

    void Update(ExpiredBillUserNotification notification);

    void UnlockAllNotificationsByUserId(long userId);

    void UpdateDeferredNotification(ExpiredBillUserNotification notification);

    ExpiredBillUserNotification GetByNetId(Guid netId);

    ExpiredBillUserNotification GetLockedNotificationByUserId(long userId);

    IEnumerable<ExpiredBillUserNotification> GetAllFiltered(string value, long userId, bool orderByManager, bool withProcessed);

    IEnumerable<ExpiredBillUserNotification> GetAllDeferredNotifications();
}