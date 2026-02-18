using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Repositories.UserRoles.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.UserRoles;

public sealed class UserRoleTranslationRepository : IUserRoleTranslationRepository {
    private readonly IDbConnection _connection;

    public UserRoleTranslationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(UserRoleTranslation userProfileRoleTranslation) {
        return _connection.Query<long>(
                "INSERT INTO UserRoleTranslation (Name, UserRoleId, CultureCode, Updated) " +
                "VALUES (@Name, @UserRoleId, @CultureCode, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                userProfileRoleTranslation
            )
            .Single();
    }

    public void Update(UserRoleTranslation userProfileRoleTranslation) {
        _connection.Execute(
            "UPDATE UserRoleTranslation SET " +
            "Name = @Name, UserRoleId = @UserRoleId, CultureCode = @CultureCode, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            userProfileRoleTranslation
        );
    }

    public UserRoleTranslation GetById(long id) {
        return _connection.Query<UserRoleTranslation>(
                "SELECT * FROM UserRoleTranslation " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public UserRoleTranslation GetByNetId(Guid netId) {
        return _connection.Query<UserRoleTranslation>(
                "SELECT * FROM UserRoleTranslation " +
                "WHERE NetUID = @NetId",
                new { NetId = netId.ToString() }
            )
            .SingleOrDefault();
    }

    public List<UserRoleTranslation> GetAll() {
        return _connection.Query<UserRoleTranslation>(
                "SELECT * FROM UserRoleTranslation " +
                "WHERE Deleted = 0"
            )
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE UserRoleTranslation SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }
}