using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.FilterEntities;
using GBA.Domain.Repositories.ColumnItems.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.ColumnItems;

public sealed class ColumnItemRepository : IColumnItemRepository {
    private readonly IDbConnection _connection;

    public ColumnItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ColumnItem columnItem) {
        return _connection.Query<long>(
                "INSERT INTO ColumnItem ([Name], [SQL], [CssClass], [Order], [Type], [UserId], [Updated]) " +
                "VALUES (@Name, @SQL, @CssClass, @Order, @Type, @UserId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                columnItem
            )
            .Single();
    }

    public void Update(ColumnItem columnItem) {
        _connection.Execute(
            "UPDATE ColumnItem SET " +
            "[Name] = @Name, [SQL] = @SQL, [CssClass] = @CssClass, [Order] = @Order, [Type] = @Type, [UserId] = @UserId, [Updated] = getutcdate() " +
            "WHERE NetUID = @NetUid ",
            columnItem
        );
    }

    public ColumnItem GetById(long id) {
        return _connection.Query<ColumnItem>(
                "SELECT * FROM ColumnItem " +
                "WHERE ColumnItem.ID = @Id ",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public ColumnItem GetByNetId(Guid netId) {
        return _connection.Query<ColumnItem>(
                "SELECT * FROM ColumnItem " +
                "WHERE ColumnItem.NetUID = @NetId ",
                new { NetId = netId.ToString() }
            )
            .SingleOrDefault();
    }

    public List<ColumnItem> GetAllByTypeAndUserId(FilterEntityType type, long id) {
        return _connection.Query<ColumnItem, ColumnItemTranslation, ColumnItem>(
                "SELECT * FROM ColumnItem " +
                "LEFT JOIN ColumnItemTranslation " +
                "ON ColumnItem.ID = ColumnItemTranslation.ColumnItemID " +
                "AND ColumnItemTranslation.CultureCode = @Culture " +
                "WHERE ColumnItem.Type = @Type AND ColumnItem.UserId = @Id AND ColumnItem.Deleted = 0 " +
                "ORDER BY ColumnItem.[Order]",
                (item, translation) => {
                    if (translation != null) item.Name = translation.Name;

                    return item;
                },
                new { Type = type, Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE ColumnItem SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId ",
            new { NetId = netId.ToString() }
        );
    }
}