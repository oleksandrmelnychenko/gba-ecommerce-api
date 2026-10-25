using System.Data;
using System.Linq;
using Dapper;
using GBA.Common.IdentityConfiguration.Entities;
using GBA.Domain.Repositories.Identities.Contracts;

namespace GBA.Domain.Repositories.Identities;

public sealed class UserTokenRepository : IUserTokenRepository {
    private readonly IDbConnection _connection;

    public UserTokenRepository(IDbConnection connection) {
        _connection = connection;
    }

    public bool IsTokenExistForUser(string userId) {
        return _connection.Query<bool>(
                "SELECT " +
                "CASE " +
                "WHEN (COUNT(*) = 0) " +
                "THEN 0 " +
                "WHEN (COUNT(*) > 0) " +
                "THEN 1 " +
                "END AS Result " +
                "FROM UserToken " +
                "WHERE UserID = @UserId",
                new { UserId = userId }
            )
            .Single();
    }

    public void Add(UserToken userToken) {
        _connection.Execute(
            "INSERT INTO UserToken (Token, UserId) " +
            "VALUES (@Token, @UserId) ",
            userToken
        );
    }

    public void Update(UserToken userToken) {
        _connection.Execute(
            "UPDATE UserToken " +
            "SET Token = @Token, UserId = @UserId " +
            "WHERE ID = @Id ",
            userToken
        );
    }

    public bool TryReplace(long id, string userId, string expectedToken, string replacementToken) {
        int affectedRows = _connection.Execute(
            "UPDATE UserToken " +
            "SET Token = @ReplacementToken " +
            "WHERE ID = @Id " +
            "AND UserID = @UserId " +
            "AND Token = @ExpectedToken",
            new { Id = id, UserId = userId, ExpectedToken = expectedToken, ReplacementToken = replacementToken }
        );

        return affectedRows == 1;
    }

    public UserToken GetByUserId(string userId) {
        return _connection.Query<UserToken>(
                "SELECT TOP(1) * FROM UserToken " +
                "WHERE UserID = @UserId",
                new { UserId = userId }
            )
            .SingleOrDefault();
    }
}
