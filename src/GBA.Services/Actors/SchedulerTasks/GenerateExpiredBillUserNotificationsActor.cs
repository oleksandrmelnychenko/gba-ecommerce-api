using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.UserNotifications;
using GBA.Domain.Messages.SchedulerTasks;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.UserNotifications.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.SchedulerTasks;

public sealed class GenerateExpiredBillUserNotificationsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly IUserNotificationRepositoriesFactory _userNotificationRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public GenerateExpiredBillUserNotificationsActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IUserNotificationRepositoriesFactory userNotificationRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _userNotificationRepositoriesFactory = userNotificationRepositoriesFactory;

        Receive<GenerateExpiredBillUserNotificationsMessage>(ProcessGenerateExpiredBillUserNotificationsMessage);
    }

    private void ProcessGenerateExpiredBillUserNotificationsMessage(GenerateExpiredBillUserNotificationsMessage message) {
        if (DateTime.Now.DayOfWeek.Equals(DayOfWeek.Saturday) || DateTime.Now.DayOfWeek.Equals(DayOfWeek.Sunday)) return;

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISaleRepository saleRepository = _saleRepositoriesFactory.NewSaleRepository(connection);
        IExpiredBillUserNotificationRepository expiredBillUserNotificationRepository =
            _userNotificationRepositoriesFactory.NewExpiredBillUserNotificationRepository(connection);

        long gbaId = _userRepositoriesFactory.NewUserRepository(connection).GetManagerOrGBAIdByClientNetId(Guid.Empty);

        IEnumerable<Sale> sales = saleRepository.GetAllExpiredOrLockedSales();

        foreach (Sale sale in sales) {
            sale.ExpiredDays += 1;

            saleRepository.UpdateSaleExpiredDays(sale);

            if (sale.ExpiredDays > 3)
                expiredBillUserNotificationRepository
                    .Add(new ExpiredBillUserNotification {
                        SaleId = sale.Id,
                        ExpiredDays = sale.ExpiredDays,
                        FromClient =
                            $"{sale.ClientAgreement?.Client?.RegionCode?.Value ?? string.Empty} {sale.ClientAgreement?.Client?.FullName ?? string.Empty}",
                        SaleNumber = sale.SaleNumber.Value,
                        CreatedById = gbaId,
                        ManagerId = sale.ClientAgreement?.Client?.ClientManagers.FirstOrDefault()?.UserProfileId
                    });
        }
    }
}