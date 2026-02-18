using System.Collections.Generic;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.UserNotifications;
using GBA.Domain.Messages.SchedulerTasks;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.UserNotifications.Contracts;

namespace GBA.Services.Actors.SchedulerTasks;

public sealed class DeferExpiredBillUserNotificationsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly IUserNotificationRepositoriesFactory _userNotificationRepositoriesFactory;

    public DeferExpiredBillUserNotificationsActor(
        IDbConnectionFactory connectionFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IUserNotificationRepositoriesFactory userNotificationRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _userNotificationRepositoriesFactory = userNotificationRepositoriesFactory;

        Receive<DeferExpiredBillUserNotificationsMessage>(ProcessDeferExpiredBillUserNotificationsMessage);
    }

    private void ProcessDeferExpiredBillUserNotificationsMessage(DeferExpiredBillUserNotificationsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
        IExpiredBillUserNotificationRepository expiredBillUserNotificationRepository =
            _userNotificationRepositoriesFactory.NewExpiredBillUserNotificationRepository(connection);

        IEnumerable<ExpiredBillUserNotification> notifications = expiredBillUserNotificationRepository.GetAllDeferredNotifications();

        foreach (ExpiredBillUserNotification notification in notifications) {
            notification.Processed = false;
            notification.Deferred = false;

            notification.ProcessedById = null;

            notification.AppliedAction = ExpiredBillUserNotificationAppliedAction.None;

            notification.ExpiredDays += 1;

            notification.Sale.ExpiredDays += 1;

            expiredBillUserNotificationRepository.UpdateDeferredNotification(notification);

            saleRepository.UpdateSaleExpiredDays(notification.Sale);
        }
    }
}