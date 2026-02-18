using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Entities.UserNotifications;

public sealed class ExpiredBillUserNotification : BaseUserNotification {
    public ExpiredBillUserNotification() {
        UserNotificationType = UserNotificationType.SaleBillExpired;
    }

    public ExpiredBillUserNotificationAppliedAction AppliedAction { get; set; }

    public string SaleNumber { get; set; }

    public string FromClient { get; set; }

    public double ExpiredDays { get; set; }

    public bool Deferred { get; set; }

    public long SaleId { get; set; }

    public long? ManagerId { get; set; }

    public Sale Sale { get; set; }

    public User Manager { get; set; }
}