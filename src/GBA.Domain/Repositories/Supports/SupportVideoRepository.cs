using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Repositories.Supports.Contracts;

namespace GBA.Domain.Repositories.Supports;

public sealed class SupportVideoRepository : ISupportVideoRepository {
    private readonly IDbConnection _connection;

    public SupportVideoRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupportVideo supportVideo) {
        return _connection.Query<long>(
            "INSERT INTO [SupportVideo] ([NameUk], [NamePl], [Url], [DocumentUrl], Updated) " +
            "VALUES (@NameUk, @NamePl, @Url, @DocumentUrl, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            supportVideo
        ).Single();
    }

    public void Update(SupportVideo supportVideo) {
        _connection.Execute(
            "UPDATE [SupportVideo] " +
            "SET [NameUk] = @NameUk, [NamePl] = @NamePl, [Url] = @Url, [DocumentUrl] = @DocumentUrl, Updated = GETUTCDATE(), Deleted = @Deleted " +
            "WHERE ID = @Id",
            supportVideo
        );
    }

    public SupportVideo GetById(long id) {
        return _connection.Query<SupportVideo>(
            "SELECT * " +
            "FROM [SupportVideo] " +
            "WHERE ID = @Id",
            new { Id = id }
        ).SingleOrDefault();
    }

    public IEnumerable<SupportVideo> GetAll() {
        return _connection.Query<SupportVideo>(
            "SELECT * " +
            "FROM [SupportVideo] " +
            "WHERE [SupportVideo].Deleted = 0"
        );
    }
}