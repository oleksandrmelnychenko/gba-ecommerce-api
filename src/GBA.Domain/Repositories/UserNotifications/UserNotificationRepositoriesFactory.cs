using System.Data;
using GBA.Domain.Repositories.UserNotifications.Contracts;

namespace GBA.Domain.Repositories.UserNotifications;

public sealed class UserNotificationRepositoriesFactory : IUserNotificationRepositoriesFactory {
    public IExpiredBillUserNotificationRepository NewExpiredBillUserNotificationRepository(IDbConnection connection) {
        return new ExpiredBillUserNotificationRepository(connection);
    }
}