using System;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Domain.Repositories.Users;

public sealed class UserScreenResolutionRepository : IUserScreenResolutionRepository {
    private readonly IDbConnection _connection;

    public UserScreenResolutionRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(UserScreenResolution userScreenResolution) {
        _connection.Execute(
            "INSERT INTO UserScreenResolution(UserID, Height, Width, Updated) " +
            "VALUES(@UserID, @Height, @Width, getutcdate())",
            userScreenResolution
        );
    }

    public UserScreenResolution GetByUserNetId(Guid userNetId) {
        return _connection.Query<UserScreenResolution, User, UserScreenResolution>(
                "SELECT * FROM UserScreenResolution " +
                "LEFT OUTER JOIN [User] " +
                "ON [User].ID = UserScreenResolution.UserID AND [User].Deleted = 0 " +
                "WHERE [User].NetUID = @UserNetId ",
                (userScreenResolution, user) => {
                    return userScreenResolution;
                },
                new { UserNetId = userNetId }
            )
            .SingleOrDefault();
    }

    public void Update(UserScreenResolution userScreenResolution) {
        _connection.Execute(
            "UPDATE UserScreenResolution SET UserID = @UserID, Height = @Height, Width = @Width, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            userScreenResolution
        );
    }
}