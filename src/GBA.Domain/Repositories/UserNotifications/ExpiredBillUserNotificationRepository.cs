using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.UserNotifications;
using GBA.Domain.Repositories.UserNotifications.Contracts;

namespace GBA.Domain.Repositories.UserNotifications;

public sealed class ExpiredBillUserNotificationRepository : IExpiredBillUserNotificationRepository {
    private readonly IDbConnection _connection;

    public ExpiredBillUserNotificationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ExpiredBillUserNotification notification) {
        return _connection.Query<long>(
                "INSERT INTO [ExpiredBillUserNotification] " +
                "(SaleNumber, FromClient, ExpiredDays, [Deferred], SaleId, ManagerId, [Locked], [Processed], CreatedById, LockedById, LastViewedById, ProcessedById, " +
                "AppliedAction, UserNotificationType, Updated) " +
                "VALUES " +
                "(@SaleNumber, @FromClient, @ExpiredDays, @Deferred, @SaleId, @ManagerId, @Locked, @Processed, @CreatedById, @LockedById, @LastViewedById, @ProcessedById, " +
                "@AppliedAction, @UserNotificationType, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                notification
            )
            .Single();
    }

    public void Update(ExpiredBillUserNotification notification) {
        _connection.Execute(
            "UPDATE [ExpiredBillUserNotification] " +
            "SET ExpiredDays = @ExpiredDays, [Deferred] = @Deferred, [Locked] = @Locked, [Processed] = @Processed, LockedById = @LockedById, LastViewedById = @LastViewedById, " +
            "ProcessedById = @ProcessedById, AppliedAction = @AppliedAction, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            notification
        );
    }

    public void UnlockAllNotificationsByUserId(long userId) {
        _connection.Execute(
            "UPDATE [ExpiredBillUserNotification] " +
            "SET LockedByID = NULL, [Locked] = 0, Updated = GETUTCDATE() " +
            "WHERE [ExpiredBillUserNotification].LockedByID = @UserId",
            new { UserId = userId }
        );
    }

    public void UpdateDeferredNotification(ExpiredBillUserNotification notification) {
        _connection.Execute(
            "UPDATE [ExpiredBillUserNotification] " +
            "SET [Processed] = @Processed, [Deferred] = @Deferred, ProcessedById = @ProcessedById, AppliedAction = @AppliedAction, " +
            "ExpiredDays = @ExpiredDays, Updated = GETUTCDATE() " +
            "WHERE [ExpiredBillUserNotification].ID = @Id",
            notification
        );
    }

    public ExpiredBillUserNotification GetByNetId(Guid netId) {
        string sqlExpression =
            "SELECT * " +
            "FROM [ExpiredBillUserNotification] " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [ExpiredBillUserNotification].CreatedByID " +
            "LEFT JOIN [User] AS [LockedBy] " +
            "ON [LockedBy].ID = [ExpiredBillUserNotification].LockedByID " +
            "LEFT JOIN [User] AS [LastViewedBy] " +
            "ON [LastViewedBy].ID = [ExpiredBillUserNotification].LastViewedByID " +
            "LEFT JOIN [User] AS [ProcessedBy] " +
            "ON [ProcessedBy].ID = [ExpiredBillUserNotification].ProcessedByID " +
            "LEFT JOIN [User] AS [Manager] " +
            "ON [Manager].ID = [ExpiredBillUserNotification].ManagerID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [ExpiredBillUserNotification].SaleID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [Sale].SaleNumberID = [SaleNumber].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [Sale].ClientAgreementID = [ClientAgreement].ID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "WHERE [ExpiredBillUserNotification].NetUID = @NetId";

        Type[] types = {
            typeof(ExpiredBillUserNotification),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(Sale),
            typeof(SaleNumber),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(RegionCode)
        };

        Func<object[], ExpiredBillUserNotification> mapper = objects => {
            ExpiredBillUserNotification notification = (ExpiredBillUserNotification)objects[0];
            User createdBy = (User)objects[1];
            User lockedBy = (User)objects[2];
            User lastViewedBy = (User)objects[3];
            User processedBy = (User)objects[4];
            User manager = (User)objects[5];
            Sale sale = (Sale)objects[6];
            SaleNumber saleNumber = (SaleNumber)objects[7];
            ClientAgreement clientAgreement = (ClientAgreement)objects[8];
            Client client = (Client)objects[9];
            RegionCode regionCode = (RegionCode)objects[10];

            client.RegionCode = regionCode;

            clientAgreement.Client = client;

            sale.ClientAgreement = clientAgreement;
            sale.SaleNumber = saleNumber;

            notification.Sale = sale;
            notification.CreatedBy = createdBy;
            notification.LockedBy = lockedBy;
            notification.LastViewedBy = lastViewedBy;
            notification.ProcessedBy = processedBy;
            notification.Manager = manager;

            return notification;
        };

        return _connection.Query(
            sqlExpression,
            types,
            mapper,
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public ExpiredBillUserNotification GetLockedNotificationByUserId(long userId) {
        string sqlExpression =
            "SELECT * " +
            "FROM [ExpiredBillUserNotification] " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [ExpiredBillUserNotification].CreatedByID " +
            "LEFT JOIN [User] AS [LockedBy] " +
            "ON [LockedBy].ID = [ExpiredBillUserNotification].LockedByID " +
            "LEFT JOIN [User] AS [LastViewedBy] " +
            "ON [LastViewedBy].ID = [ExpiredBillUserNotification].LastViewedByID " +
            "LEFT JOIN [User] AS [ProcessedBy] " +
            "ON [ProcessedBy].ID = [ExpiredBillUserNotification].ProcessedByID " +
            "LEFT JOIN [User] AS [Manager] " +
            "ON [Manager].ID = [ExpiredBillUserNotification].ManagerID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [ExpiredBillUserNotification].SaleID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [Sale].SaleNumberID = [SaleNumber].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [Sale].ClientAgreementID = [ClientAgreement].ID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "WHERE [LockedBy].ID = @UserId";

        Type[] types = {
            typeof(ExpiredBillUserNotification),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(Sale),
            typeof(SaleNumber),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(RegionCode)
        };

        Func<object[], ExpiredBillUserNotification> mapper = objects => {
            ExpiredBillUserNotification notification = (ExpiredBillUserNotification)objects[0];
            User createdBy = (User)objects[1];
            User lockedBy = (User)objects[2];
            User lastViewedBy = (User)objects[3];
            User processedBy = (User)objects[4];
            User manager = (User)objects[5];
            Sale sale = (Sale)objects[6];
            SaleNumber saleNumber = (SaleNumber)objects[7];
            ClientAgreement clientAgreement = (ClientAgreement)objects[8];
            Client client = (Client)objects[9];
            RegionCode regionCode = (RegionCode)objects[10];

            client.RegionCode = regionCode;

            clientAgreement.Client = client;

            sale.ClientAgreement = clientAgreement;
            sale.SaleNumber = saleNumber;

            notification.Sale = sale;
            notification.CreatedBy = createdBy;
            notification.LockedBy = lockedBy;
            notification.LastViewedBy = lastViewedBy;
            notification.ProcessedBy = processedBy;
            notification.Manager = manager;

            return notification;
        };

        return _connection.Query(
            sqlExpression,
            types,
            mapper,
            new { UserId = userId }
        ).SingleOrDefault();
    }

    public IEnumerable<ExpiredBillUserNotification> GetAllFiltered(string value, long userId, bool orderByManager, bool withProcessed) {
        string sqlExpression =
            "SELECT * " +
            "FROM [ExpiredBillUserNotification] " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [ExpiredBillUserNotification].CreatedByID " +
            "LEFT JOIN [User] AS [LockedBy] " +
            "ON [LockedBy].ID = [ExpiredBillUserNotification].LockedByID " +
            "LEFT JOIN [User] AS [LastViewedBy] " +
            "ON [LastViewedBy].ID = [ExpiredBillUserNotification].LastViewedByID " +
            "LEFT JOIN [User] AS [ProcessedBy] " +
            "ON [ProcessedBy].ID = [ExpiredBillUserNotification].ProcessedByID " +
            "LEFT JOIN [User] AS [Manager] " +
            "ON [Manager].ID = [ExpiredBillUserNotification].ManagerID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [ExpiredBillUserNotification].SaleID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [Sale].SaleNumberID = [SaleNumber].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [Sale].ClientAgreementID = [ClientAgreement].ID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "WHERE [ExpiredBillUserNotification].Deleted = 0 " +
            "AND [ExpiredBillUserNotification].[Locked] = 0 " +
            "AND [ExpiredBillUserNotification].[Deferred] = 0 " +
            "AND (" +
            "[ExpiredBillUserNotification].SaleNumber LIKE N'%' + @Value + N'%' " +
            "OR " +
            "[ExpiredBillUserNotification].FromClient LIKE N'%' + @Value + N'%'" +
            ") ";

        if (!withProcessed) sqlExpression += "AND [ExpiredBillUserNotification].[Processed] = 0 ";

        if (orderByManager)
            sqlExpression += "ORDER BY CASE WHEN [Manager].ID = @UserId THEN 0 ELSE 1 END, [ExpiredBillUserNotification].ExpiredDays DESC";
        else
            sqlExpression += "ORDER BY [ExpiredBillUserNotification].ExpiredDays DESC";

        Type[] types = {
            typeof(ExpiredBillUserNotification),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(Sale),
            typeof(SaleNumber),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(RegionCode)
        };

        Func<object[], ExpiredBillUserNotification> mapper = objects => {
            ExpiredBillUserNotification notification = (ExpiredBillUserNotification)objects[0];
            User createdBy = (User)objects[1];
            User lockedBy = (User)objects[2];
            User lastViewedBy = (User)objects[3];
            User processedBy = (User)objects[4];
            User manager = (User)objects[5];
            Sale sale = (Sale)objects[6];
            SaleNumber saleNumber = (SaleNumber)objects[7];
            ClientAgreement clientAgreement = (ClientAgreement)objects[8];
            Client client = (Client)objects[9];
            RegionCode regionCode = (RegionCode)objects[10];

            client.RegionCode = regionCode;

            clientAgreement.Client = client;

            sale.ClientAgreement = clientAgreement;
            sale.SaleNumber = saleNumber;

            notification.Sale = sale;
            notification.CreatedBy = createdBy;
            notification.LockedBy = lockedBy;
            notification.LastViewedBy = lastViewedBy;
            notification.ProcessedBy = processedBy;
            notification.Manager = manager;

            return notification;
        };

        return _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                Value = value,
                UserId = userId
            }
        );
    }

    public IEnumerable<ExpiredBillUserNotification> GetAllDeferredNotifications() {
        return _connection.Query<ExpiredBillUserNotification, Sale, ExpiredBillUserNotification>(
            "SELECT * " +
            "FROM [ExpiredBillUserNotification] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [ExpiredBillUserNotification].SaleID " +
            "WHERE [ExpiredBillUserNotification].Deferred = 1",
            (notification, sale) => {
                notification.Sale = sale;

                return notification;
            }
        );
    }
}