using System;
using System.Linq;
using System.Text.RegularExpressions;
using GBA.Domain.EntityHelpers;

namespace GBA.Domain.RepositoryHelpers.Products;

public static class AdvancedSearchQueryBuilder {
    public static string BuildSearchQuery(string value, ProductAdvancedSearchMode mode, string currentCulture, ProductAdvancedSortMode sortMode, bool freetext = true) {
        string searchMethod = freetext ? "FREETEXTTABLE" : "CONTAINSTABLE";

        string sqlExpression = string.Empty;

        string orderByExpression = "ORDER BY [Search_CTE].TotalRank DESC";

        string likeConditions = string.Join(" OR ", value.Split(" ").Select(t => $"Product.SearchVendorCode LIKE '%{t}%'"));

        value = ToFullTextSearch(value);

        switch (sortMode) {
            case ProductAdvancedSortMode.Name:
                orderByExpression +=
                    currentCulture.ToLower().Equals("uk")
                        ? ", [Product].[NameUA]"
                        : ", [Product].[NamePL]";

                break;
            case ProductAdvancedSortMode.VendorCode:
                orderByExpression += ", [Product].[VendorCode]";

                break;
            case ProductAdvancedSortMode.Top:
            default:
                orderByExpression += ", [Product].[Top]";

                break;
        }

        switch (mode) {
            case ProductAdvancedSearchMode.VendorCode:
                sqlExpression +=
                    ";WITH [Search_CTE] " +
                    "AS ( " +
                    "SELECT Product.ID, " +
                    "ISNULL(ProductTextTable.RANK, 0) AS TotalRank " +
                    "FROM dbo.Product " +
                    $"INNER JOIN {searchMethod}(Product, (SearchVendorCode), {value}) AS ProductTextTable " +
                    "ON Product.Id = ProductTextTable.[KEY] " +
                    "GROUP BY " +
                    "Product.ID, " +
                    "ProductTextTable.RANK " +
                    "), ";
                break;
            case ProductAdvancedSearchMode.OriginalNumber:
                sqlExpression +=
                    ";WITH [Search_CTE] " +
                    "AS ( " +
                    "SELECT Product.ID, " +
                    "MAX(ISNULL(OriginalNumberTextTable.RANK, 0)) AS TotalRank " +
                    "FROM dbo.Product " +
                    "LEFT JOIN ProductOriginalNumber " +
                    "ON ProductOriginalNumber.ProductID = Product.ID " +
                    "LEFT JOIN [OriginalNumber] " +
                    "ON [OriginalNumber].ID = [ProductOriginalNumber].OriginalNumberID " +
                    $"INNER JOIN CONTAINSTABLE(OriginalNumber, ([Number]), {value}) AS OriginalNumberTextTable " + // Works better with CONTAINSTABLE
                    "ON ProductOriginalNumber.OriginalNumberID = OriginalNumberTextTable.[Key] " +
                    "GROUP BY " +
                    "Product.ID, " +
                    "OriginalNumberTextTable.RANK " +
                    "), ";
                break;
            case ProductAdvancedSearchMode.Size:
                sqlExpression +=
                    ";WITH [Search_CTE] " +
                    "AS ( " +
                    "SELECT Product.ID, " +
                    "ISNULL(ProductTextTable.RANK, 0) AS TotalRank " +
                    "FROM dbo.Product " +
                    $"INNER JOIN CONTAINSTABLE(Product, (SearchSize), {value}) AS ProductTextTable " +
                    "ON Product.Id = ProductTextTable.[KEY] " +
                    "GROUP BY " +
                    "Product.ID, " +
                    "ProductTextTable.RANK " +
                    "), ";
                break;
            case ProductAdvancedSearchMode.Name:
                sqlExpression +=
                    ";WITH [Search_CTE] " +
                    "AS ( " +
                    "SELECT Product.ID, " +
                    "ISNULL(ProductTextTable.RANK, 0) AS TotalRank " +
                    "FROM dbo.Product ";
                if (currentCulture.Equals("pl"))
                    sqlExpression +=
                        $"INNER JOIN {searchMethod}(Product, (SearchName, NamePL), {value}) AS ProductTextTable " +
                        "ON Product.Id = ProductTextTable.[KEY] ";
                else
                    sqlExpression +=
                        $"INNER JOIN {searchMethod}(Product, (SearchName, NameUA), {value}) AS ProductTextTable " +
                        "ON Product.Id = ProductTextTable.[KEY] ";

                sqlExpression +=
                    "GROUP BY " +
                    "Product.ID, " +
                    "ProductTextTable.RANK " +
                    "), ";
                break;
            case ProductAdvancedSearchMode.Description:
                sqlExpression +=
                    ";WITH [Search_CTE] " +
                    "AS ( " +
                    "SELECT Product.ID, " +
                    "ISNULL(ProductTextTable.RANK, 0) AS TotalRank " +
                    "FROM dbo.Product ";
                if (currentCulture.Equals("pl"))
                    sqlExpression +=
                        $"INNER JOIN {searchMethod}(Product, (SearchDescription, SearchDescriptionPL), {value}) AS ProductTextTable " +
                        "ON Product.Id = ProductTextTable.[KEY] ";
                else
                    sqlExpression +=
                        $"INNER JOIN {searchMethod}(Product, (SearchDescription, SearchDescriptionUA), {value}) AS ProductTextTable " +
                        "ON Product.Id = ProductTextTable.[KEY] ";

                sqlExpression +=
                    "GROUP BY " +
                    "Product.ID, " +
                    "ProductTextTable.RANK " +
                    "), ";
                break;
            case ProductAdvancedSearchMode.All:
            default:
                sqlExpression +=
                    ";WITH [Search_CTE] " +
                    "AS ( " +
                    "SELECT Product.ID, " +
                    $"ISNULL(ProductTextTable.RANK, 0) * CASE WHEN {likeConditions} THEN 1.5 ELSE 1 END + MAX(ISNULL(OriginalNumberTextTable.RANK, 0)) AS TotalRank " +
                    "FROM dbo.Product ";
                if (currentCulture.Equals("pl"))
                    sqlExpression +=
                        $"LEFT JOIN {searchMethod}(Product, (SearchDescription, SearchName, SearchSize, SearchVendorCode, SearchDescriptionPL, NamePL), {value}) AS ProductTextTable " +
                        "ON Product.Id = ProductTextTable.[KEY] ";
                else
                    sqlExpression +=
                        $"LEFT JOIN {searchMethod}(Product, (SearchDescription, SearchName, SearchSize, SearchVendorCode, SearchDescriptionUA, NameUA), {value}) AS ProductTextTable " +
                        "ON Product.Id = ProductTextTable.[KEY] ";

                sqlExpression +=
                    "LEFT JOIN ProductOriginalNumber " +
                    "ON ProductOriginalNumber.ProductID = Product.ID " +
                    "LEFT JOIN [OriginalNumber] " +
                    "ON [OriginalNumber].ID = [ProductOriginalNumber].OriginalNumberID " +
                    $"LEFT JOIN {searchMethod}(OriginalNumber, ([Number]), {value}) AS OriginalNumberTextTable " +
                    "ON ProductOriginalNumber.OriginalNumberID = OriginalNumberTextTable.[Key] " +
                    "WHERE ISNULL(ProductTextTable.[KEY], 0) <> 0 OR ISNULL(OriginalNumberTextTable.[KEY], 0) <> 0 " +
                    "GROUP BY " +
                    "Product.ID, " +
                    "ProductTextTable.RANK, " +
                    "OriginalNumberTextTable.RANK, " +
                    "SearchVendorCode " +
                    "), ";
                break;
        }

        sqlExpression +=
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT [Product].ID " +
            $", ROW_NUMBER() OVER({orderByExpression}) AS [RowNumber] " +
            "FROM [Search_CTE] " +
            "INNER JOIN [Product] " +
            "ON [Product].ID = [Search_CTE].ID " +
            ") " +
            "SELECT ID FROM [Rowed_CTE] " +
            "WHERE [Rowed_CTE].RowNumber > @Offset " +
            "AND [Rowed_CTE].RowNumber <= @Limit + @Offset ";

        return sqlExpression;
    }

    /// <summary>
    /// Converts a raw search string into a SQL full-text compatible expression.
    /// Example:
    /// "втулка AUG51777 50x71x100 0228483"
    /// => "\"втулка*\" OR \"AUG51777*\" OR \"50x71x100*\" OR \"0228483*\""
    /// </summary>
    // public static string ToFullTextSearch(string input) {
    //     if (string.IsNullOrWhiteSpace(input))
    //         return string.Empty;

    // Extract words and numbers (including 'x' or 'X' inside dimensions like 50x71x100)
    //     string[] tokens = Regex.Matches(input, @"[\p{L}\p{N}]+(?:x[\p{N}]+)*")
    //         .Cast<Match>()
    //         .Select(m => m.Value.Trim())
    //         .Where(t => t.Length > 0)
    //         .Distinct(StringComparer.OrdinalIgnoreCase)
    //         .ToArray();
    //
    //     if (tokens.Length == 0)
    //         return string.Empty;
    //
    //     // Build escaped string for SQL: "\"term*\" OR \"term2*\" ..."
    //     return string.Join(" OR ", tokens.Select(t => $"\\\"{t}*\\\""));
    // }
    public static string ToFullTextSearch(string input) {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Extract words and numbers (including 'x' or 'X' inside dimensions like 50x71x100)
        string[] tokens = Regex.Matches(input, @"[\p{L}\p{N}]+(?:x[\p{N}]+)*")
            .Select(m => m.Value.Trim())
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (tokens.Length == 0)
            return string.Empty;

        // Sanitize tokens to prevent SQL injection
        tokens = tokens
            .Select(t => Regex.Replace(t, @"[^\p{L}\p{N}xX]+", ""))
            .Where(t => t.Length > 0)
            .ToArray();

        // Build condition: "term*" OR "term2*"
        string condition = string.Join(" OR ", tokens.Select(t => $"\"{t}*\""));

        // Wrap in single quotes for SQL syntax
        return $"'{condition}'";
    }
}