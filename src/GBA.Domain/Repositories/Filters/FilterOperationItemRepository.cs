using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.FilterEntities;
using GBA.Domain.Repositories.Filters.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Filters;

public sealed class FilterOperationItemRepository : IFilterOperationItemRepository {
    private readonly IDbConnection _connection;

    public FilterOperationItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public List<FilterOperationItem> GetAll() {
        return _connection.Query<FilterOperationItem, FilterOperationItemTranslation, FilterOperationItem>(
                "SELECT * FROM FilterOperationItem " +
                "LEFT OUTER JOIN FilterOperationItemTranslation " +
                "ON FilterOperationItem.ID = FilterOperationItemTranslation.FilterOperationItemID " +
                "AND FilterOperationItemTranslation.Deleted = 0 " +
                "AND FilterOperationItemTranslation.CultureCode = @Culture " +
                "WHERE FilterOperationItem.Deleted = 0 ",
                (item, translation) => {
                    if (translation != null) item.Name = translation.Name;

                    return item;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .ToList();
    }
}