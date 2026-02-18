using System.Linq;
using System.Text;
using GBA.Domain.FilterEntities;

namespace GBA.Domain.Helpers;

public static class SqlBuilder {
    public static string GenerateAdditionalBoolSQLWhereStatement(BooleanFilterItem filter, string tableName) {
        if (!string.IsNullOrEmpty(filter.SQL)) {
            StringBuilder builder = new();

            if (tableName.ToLower().Equals("user")) tableName = "[User]";

            if (tableName.ToLower().Equals("order")) tableName = "[Order]";

            builder.Append($" AND {tableName}.{filter.SQL} = ");

            if (filter.Value)
                builder.Append("1 ");
            else
                builder.Append("0 ");

            return builder.ToString();
        }

        return string.Empty;
    }

    public static string GenerateSQLWhereStatement(Filter filter, string tableName, bool emptyValue = false) {
        StringBuilder builder = new();
        string operation = string.Empty;

        if (tableName.ToLower().Equals("user")) tableName = "[User]";

        if (tableName.ToLower().Equals("order")) tableName = "[Order]";

        if (filter.FilterItem.FilterOperationItem == null)
            operation = " like '@Value%' ";
        else
            operation = ConvertToSQLOperation(filter);

        if (filter.FilterItem.SQL.Contains("/")) {
            string[] columns = filter.FilterItem.SQL.Split('/');
            bool first = true;
            builder.Append($"WHERE {tableName}.Deleted = 0 AND ( ");

            if (filter.FilterItem.SQL.Contains("."))
                foreach (string column in columns)
                    if (first) {
                        first = false;

                        if (emptyValue)
                            builder.Append($" ({column} {operation} OR {column} IS NULL) ");
                        else
                            builder.Append($" {column} {operation} ");
                    } else {
                        if (emptyValue)
                            builder.Append($" OR ({column} {operation} OR {column} IS NULL) ");
                        else
                            builder.Append($" OR {column} {operation}");
                    }
            else
                foreach (string column in columns)
                    if (first) {
                        first = false;

                        if (emptyValue)
                            builder.Append($" ({tableName}.{column} {operation} OR {tableName}.{column} IS NULL) ");
                        else
                            builder.Append($" {tableName}.{column} {operation}");
                    } else {
                        if (emptyValue)
                            builder.Append($" OR ({tableName}.{column} {operation} OR {tableName}.{column} IS NULL) ");
                        else
                            builder.Append($" OR {tableName}.{column} {operation}");
                    }

            builder.Append(") ");
        } else if (filter.FilterItem.SQL.Contains(".")) {
            if (emptyValue)
                builder.Append($"WHERE {tableName}.Deleted = 0 AND ({filter.FilterItem.SQL} {operation} OR {filter.FilterItem.SQL} IS NULL)");
            else
                builder.Append($"WHERE {tableName}.Deleted = 0 AND {filter.FilterItem.SQL} {operation} ");
        } else {
            if (emptyValue)
                builder.Append($"WHERE {tableName}.Deleted = 0 AND ({tableName}.{filter.FilterItem.SQL} {operation} OR {tableName}.{filter.FilterItem.SQL} IS NULL)");
            else
                builder.Append($"WHERE {tableName}.Deleted = 0 AND {tableName}.{filter.FilterItem.SQL} {operation}");
        }

        if (tableName.ToLower().Equals("client")) builder.Append(" AND Client.IsSubClient = 0 AND Client.IsTradePoint = 0 ");

        return builder.ToString();
    }

    public static string GenerateSQLOrderByStatement(GetQuery query, bool isAggregate = false) {
        StringBuilder builder = new();

        if (query.Table.ToLower().Equals("user")) query.Table = "[User]";

        if (query.Table.ToLower().Equals("order")) query.Table = "[Order]";

        if (query.SortDescriptors.Any()) {
            builder.Append("ORDER BY ");
            bool first = true;

            if (!isAggregate)
                foreach (SortDescriptor descriptor in query.SortDescriptors)
                    if (first) {
                        first = false;

                        if (descriptor.Column.Contains("."))
                            builder.Append($"{descriptor.Column} {descriptor.Dir} ");
                        else
                            builder.Append($"{query.Table}.{descriptor.Column} {descriptor.Dir} ");
                    } else {
                        if (descriptor.Column.Contains("."))
                            builder.Append($", {descriptor.Column} {descriptor.Dir} ");
                        else
                            builder.Append($", {query.Table}.{descriptor.Column} {descriptor.Dir} ");
                    }
            else
                foreach (SortDescriptor descriptor in query.SortDescriptors)
                    if (first) {
                        first = false;

                        if (descriptor.Column.Contains("."))
                            builder.Append($"MIN({descriptor.Column}) {descriptor.Dir} ");
                        else
                            builder.Append($"MIN({query.Table}.{descriptor.Column}) {descriptor.Dir} ");
                    } else {
                        if (descriptor.Column.Contains("."))
                            builder.Append($", MIN({descriptor.Column}) {descriptor.Dir} ");
                        else
                            builder.Append($", MIN({query.Table}.{descriptor.Column}) {descriptor.Dir} ");
                    }
        } else {
            builder.Append($"ORDER BY {query.Table}.ID DESC ");
        }

        return builder.ToString();
    }

    public static string GenerateClientsSQLInStatement(string input) {
        return " AND ClientInRole.ClientTypeRoleID IN (" + input + ") ";
    }

    private static string ConvertToSQLOperation(Filter filter) {
        switch (filter.FilterItem.FilterOperationItem.SQL) {
            case "IN":
                return " IN (" + filter.Value + ") ";
            case "Contains":
                return " like '%' + @Value + '%' ";
            case "StartWidth":
            default:
                return " like @Value + '%' ";
        }
    }
}