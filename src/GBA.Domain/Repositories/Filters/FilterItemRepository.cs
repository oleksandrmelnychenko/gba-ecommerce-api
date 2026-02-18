using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.FilterEntities;
using GBA.Domain.Repositories.Filters.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Filters;

public sealed class FilterItemRepository : IFilterItemRepository {
    private readonly IDbConnection _connection;

    public FilterItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public List<FilterItem> GetAllByType(FilterEntityType type) {
        List<FilterItem> filterItems = new();

        _connection.Query<FilterItem, FilterItemTranslation, FilterItem>(
                "SELECT * FROM FilterItem " +
                "LEFT OUTER JOIN FilterItemTranslation " +
                "ON FilterItem.ID = FilterItemTranslation.FilterItemID " +
                "AND FilterItemTranslation.Deleted = 0 " +
                "AND FilterItemTranslation.CultureCode = @Culture " +
                "WHERE FilterItem.Deleted = 0 AND FilterItem.Type = @Type " +
                "ORDER BY FilterItem.[Order]",
                (item, itemTranslation) => {
                    if (itemTranslation != null) {
                        item.Name = itemTranslation.Name;
                        item.Description = itemTranslation.Description;
                    }

                    filterItems.Add(item);

                    return item;
                },
                new { Type = type, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .ToList();

        if (filterItems.Any()) {
            FilterOperationItem filterOperationItem = _connection.Query<FilterOperationItem, FilterOperationItemTranslation, FilterOperationItem>(
                    "SELECT TOP(1) * FROM FilterOperationItem " +
                    "LEFT OUTER JOIN FilterOperationItemTranslation " +
                    "ON FilterOperationItem.ID = FilterOperationItemTranslation.FilterOperationItemID " +
                    "AND FilterOperationItemTranslation.Deleted = 0 " +
                    "AND FilterOperationItemTranslation.CultureCode = @Culture " +
                    "WHERE FilterOperationItem.Deleted = 0 " +
                    "ORDER BY FilterOperationItem.ID",
                    (item, itemTranslation) => {
                        if (itemTranslation != null) item.Name = itemTranslation.Name;

                        return item;
                    },
                    new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
                )
                .SingleOrDefault();

            foreach (FilterItem filterItem in filterItems) filterItem.FilterOperationItem = filterOperationItem;
        }

        return filterItems;
    }

    public FilterItem GetClientTypeRoleFilterItem() {
        FilterItem filterItem = _connection.Query<FilterItem>(
                "SELECT * FROM FilterItem " +
                "WHERE FilterItem.Type = @Type",
                new { Type = FilterEntityType.ClientTypeRole }
            )
            .SingleOrDefault();

        if (filterItem != null)
            filterItem.FilterOperationItem = _connection.Query<FilterOperationItem>(
                    "SELECT * FROM FilterOperationItem " +
                    "WHERE [SQL] = 'IN'"
                )
                .SingleOrDefault();

        return filterItem;
    }
}