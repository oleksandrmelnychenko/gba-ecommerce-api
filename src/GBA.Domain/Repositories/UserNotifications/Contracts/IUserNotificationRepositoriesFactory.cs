using System.Data;

namespace GBA.Domain.Repositories.UserNotifications.Contracts;

public interface IUserNotificationRepositoriesFactory {
    IExpiredBillUserNotificationRepository NewExpiredBillUserNotificationRepository(IDbConnection connection);
}