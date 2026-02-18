using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Repositories.UserRoles.Contracts;

namespace GBA.Domain.Repositories.UserRoles;

public sealed class UserRoleDashboardNodeRepository : IUserRoleDashboardNodeRepository {
    private readonly IDbConnection _connection;

    public UserRoleDashboardNodeRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(UserRoleDashboardNode userRoleDashboardNode) {
        return _connection.Query<long>(
            "INSERT INTO [UserRoleDashboardNode] (UserRoleID, DashboardNodeID, Updated) " +
            "VALUES (@UserRoleID, @DashboardNodeID, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY() ",
            userRoleDashboardNode).FirstOrDefault();
    }


    public void Update(UserRoleDashboardNode userRoleDashboardNode) {
        _connection.Execute(
            "UPDATE [UserRoleDashboardNode] SET UserRoleID = @UserRoleId, DashboardNodeID = @DashboardNodeID, " +
            "Updated = GETUTCDATE(), Deleted = @Deleted " +
            "WHERE [UserRoleDashboardNode].ID = @Id ",
            userRoleDashboardNode);
    }

    public void RemoveByUserRoleId(long id) {
        _connection.Execute("DELETE FROM [UserRoleDashboardNode] WHERE UserRoleID = @Id", new { Id = id });
    }
}