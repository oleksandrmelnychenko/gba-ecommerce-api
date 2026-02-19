using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Dapper;
using GBA.Domain.EntityHelpers;

namespace GBA.Services.Services.Products;

/// <summary>
/// Optimized product search service V2.
/// Same business logic as original but:
/// 1. Ukrainian only (no Polish branches)
/// 2. Cleaner code structure
/// </summary>
public class ProductSearchServiceOptimized {

    public List<SearchResult> GetSearchResults(
        IDbConnection connection,
        string searchValue,
        long limit,
        long offset) {

        if (string.IsNullOrWhiteSpace(searchValue)) {
            return new List<SearchResult>();
        }

        searchValue = searchValue.Trim().ToLowerInvariant();
        string[] terms = searchValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (terms.Length == 1) {
            return ExecuteSingleTermSearch(connection, searchValue, limit, offset);
        }

        return ExecuteMultiTermSearch(connection, terms, searchValue, limit, offset);
    }

    private List<SearchResult> ExecuteSingleTermSearch(
        IDbConnection connection,
        string term,
        long limit,
        long offset) {

        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("@Term", $"%{term}%");
        parameters.Add("@ExactTerm", term);
        parameters.Add("@Offset", offset);
        parameters.Add("@Limit", limit);

        // Match original's logic exactly:
        // 1. SearchStage_Zero - base products with availability
        // 2. Search_FullValue - product field matches
        // 3. OriginalNumbers_FullValue - original number matches
        // 4. United_CTE - union of both
        // 5. Rowed_CTE - ranking with detailed ORDER BY
        string sql = @"
;WITH SearchStage_Zero AS (
    SELECT
        p.ID,
        p.SearchName,
        p.SearchNameUA,
        p.SearchDescription,
        p.SearchDescriptionUA,
        p.SearchVendorCode,
        p.SearchSize,
        p.MainOriginalNumber,
        CASE WHEN ISNULL(pa.Amount, 0) > 0 THEN 1 ELSE 0 END AS Available
    FROM Product p
    LEFT JOIN ProductAvailability pa ON pa.ProductID = p.ID
    WHERE p.Deleted = 0
),
Search_FullValue AS (
    SELECT
        p.ID,
        p.Available,
        p.SearchNameUA AS SearchName,
        CASE WHEN LOWER(RTRIM(LTRIM(p.MainOriginalNumber))) = @ExactTerm THEN 1 ELSE 0 END AS MainOriginalNumberExact,
        CASE WHEN LOWER(RTRIM(LTRIM(p.SearchNameUA))) = @ExactTerm OR LOWER(RTRIM(LTRIM(p.SearchVendorCode))) = @ExactTerm OR LOWER(RTRIM(LTRIM(p.MainOriginalNumber))) = @ExactTerm THEN 1 ELSE 0 END AS HundredPercentMatch,
        CASE WHEN PATINDEX(@Term, p.SearchName) > 0 THEN 1 ELSE 0 END AS SearchName_Match,
        CASE WHEN PATINDEX(@Term, p.SearchDescription) > 0 THEN 1 ELSE 0 END AS SearchDescription_Match,
        CASE WHEN PATINDEX(@Term, p.SearchNameUA) > 0 THEN 1 ELSE 0 END AS SearchNameUA_Match,
        CASE WHEN PATINDEX(@Term, p.SearchDescriptionUA) > 0 THEN 1 ELSE 0 END AS SearchDescriptionUA_Match,
        CASE WHEN PATINDEX(@Term, p.SearchVendorCode) > 0 THEN 1 ELSE 0 END AS SearchVendorCode_Match,
        CASE WHEN PATINDEX(@Term, p.SearchSize) > 0 THEN 1 ELSE 0 END AS SearchSize_Match,
        CASE WHEN PATINDEX(@Term, p.MainOriginalNumber) > 0 THEN 1 ELSE 0 END AS OriginalNumber_Match
    FROM SearchStage_Zero p
    WHERE
        PATINDEX(@Term, p.SearchName) > 0
        OR PATINDEX(@Term, p.SearchDescription) > 0
        OR PATINDEX(@Term, p.SearchNameUA) > 0
        OR PATINDEX(@Term, p.SearchDescriptionUA) > 0
        OR PATINDEX(@Term, p.SearchVendorCode) > 0
        OR PATINDEX(@Term, p.SearchSize) > 0
        OR PATINDEX(@Term, p.MainOriginalNumber) > 0
),
OriginalNumbers_FullValue AS (
    SELECT
        p.ID,
        CASE WHEN ISNULL(pa.Amount, 0) > 0 THEN 1 ELSE 0 END AS Available,
        p.SearchNameUA AS SearchName,
        CASE WHEN LOWER(RTRIM(LTRIM(p.MainOriginalNumber))) = @ExactTerm THEN 1 ELSE 0 END AS MainOriginalNumberExact,
        CASE WHEN LOWER(RTRIM(LTRIM(on_.Number))) = @ExactTerm THEN 1 ELSE 0 END AS HundredPercentMatch,
        0 AS SearchName_Match,
        0 AS SearchDescription_Match,
        0 AS SearchNameUA_Match,
        0 AS SearchDescriptionUA_Match,
        0 AS SearchVendorCode_Match,
        0 AS SearchSize_Match,
        CASE WHEN PATINDEX(@Term, on_.Number) > 0 THEN 1 ELSE 0 END AS OriginalNumber_Match
    FROM OriginalNumber on_
    LEFT JOIN ProductOriginalNumber pon ON pon.OriginalNumberID = on_.ID AND pon.Deleted = 0
    LEFT JOIN Product p ON p.ID = pon.ProductID
    LEFT JOIN ProductAvailability pa ON pa.ProductID = p.ID
    WHERE p.Deleted = 0
    AND PATINDEX(@Term, on_.Number) > 0
),
United_CTE AS (
    SELECT * FROM Search_FullValue
    UNION
    SELECT * FROM OriginalNumbers_FullValue
),
Aggregated_CTE AS (
    SELECT
        p.ID,
        MAX(p.SearchName) AS SearchName,
        MAX(p.MainOriginalNumberExact) AS MainOriginalNumberExact,
        MAX(p.HundredPercentMatch) AS HundredPercentMatch,
        MAX(p.Available) AS Available,
        MAX(p.OriginalNumber_Match) AS OriginalNumber_Match,
        MAX(p.SearchVendorCode_Match) AS SearchVendorCode_Match,
        MAX(p.SearchName_Match) AS SearchName_Match,
        MAX(p.SearchNameUA_Match) AS SearchNameUA_Match,
        MAX(p.SearchDescriptionUA_Match) AS SearchDescriptionUA_Match,
        MAX(p.SearchDescription_Match) AS SearchDescription_Match,
        MAX(p.SearchSize_Match) AS SearchSize_Match
    FROM United_CTE p
    GROUP BY p.ID
),
Rowed_CTE AS (
    SELECT
        a.ID,
        a.HundredPercentMatch,
        a.Available,
        ROW_NUMBER() OVER(ORDER BY
            a.MainOriginalNumberExact DESC,
            a.Available DESC,
            a.HundredPercentMatch DESC,
            (CASE WHEN a.OriginalNumber_Match > 0 THEN 1 ELSE 0 END) DESC,
            (CASE WHEN a.SearchVendorCode_Match > 0 THEN 1 ELSE 0 END) DESC,
            (CASE WHEN a.OriginalNumber_Match > 0 OR a.SearchName_Match > 0 OR a.SearchNameUA_Match > 0 THEN 1 ELSE 0 END) DESC,
            (CASE WHEN a.SearchDescriptionUA_Match > 0 OR a.SearchDescription_Match > 0 THEN 1 ELSE 0 END) DESC,
            (CASE WHEN a.SearchSize_Match > 0 THEN 1 ELSE 0 END) DESC,
            a.SearchName,
            a.ID
        ) AS RowNumber
    FROM Aggregated_CTE a
)
SELECT
    ID AS Id,
    RowNumber,
    CAST(HundredPercentMatch AS BIT) AS HunderdPrecentMatch,
    CAST(Available AS BIT) AS Available
FROM Rowed_CTE
WHERE RowNumber > @Offset AND RowNumber <= @Limit + @Offset
ORDER BY RowNumber;
";

        return connection.Query<SearchResult>(sql, parameters).AsList();
    }

    private List<SearchResult> ExecuteMultiTermSearch(
        IDbConnection connection,
        string[] terms,
        string fullValue,
        long limit,
        long offset) {

        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("@FullValue", fullValue);
        parameters.Add("@Offset", offset);
        parameters.Add("@Limit", limit);

        StringBuilder sql = new();

        // === STAGE ZERO: Base product data ===
        sql.Append(@"
;WITH ProductStage0 AS (
    SELECT
        p.ID,
        p.SearchName,
        p.SearchNameUA,
        p.SearchDescription,
        p.SearchDescriptionUA,
        p.SearchVendorCode,
        p.SearchSize,
        p.MainOriginalNumber,
        CASE WHEN ISNULL(pa.Amount, 0) > 0 THEN 1 ELSE 0 END AS Available
    FROM Product p
    LEFT JOIN ProductAvailability pa ON pa.ProductID = p.ID
    WHERE p.Deleted = 0
)");

        // === PRODUCT FIELDS WATERFALL ===
        for (int i = 0; i < terms.Length; i++) {
            string termParam = $"@Term{i}";
            parameters.Add(termParam, $"%{terms[i]}%");

            string prevStage = i == 0 ? "ProductStage0" : $"ProductStage{i}";
            string currStage = $"ProductStage{i + 1}";

            bool isFirstStage = i == 0;
            bool isLastStage = i == terms.Length - 1;

            sql.Append($@"
, {currStage} AS (
    SELECT
        s.ID,
        s.SearchName,
        s.SearchNameUA,
        s.SearchDescription,
        s.SearchDescriptionUA,
        s.SearchVendorCode,
        s.SearchSize,
        s.MainOriginalNumber,
        s.Available");

            if (isLastStage) {
                sql.Append($@",
        CASE WHEN LOWER(RTRIM(LTRIM(s.MainOriginalNumber))) = @FullValue THEN 1 ELSE 0 END AS MainOriginalNumberExact,
        CASE WHEN LOWER(RTRIM(LTRIM(s.SearchNameUA))) = @FullValue OR LOWER(RTRIM(LTRIM(s.SearchVendorCode))) = @FullValue OR LOWER(RTRIM(LTRIM(s.MainOriginalNumber))) = @FullValue THEN 1 ELSE 0 END AS HundredPercentMatch");
            }

            if (isFirstStage) {
                sql.Append($@",
        CASE WHEN PATINDEX({termParam}, s.SearchName) > 0 THEN 1 ELSE 0 END AS SearchName_Match,
        CASE WHEN PATINDEX({termParam}, s.SearchDescription) > 0 THEN 1 ELSE 0 END AS SearchDescription_Match,
        CASE WHEN PATINDEX({termParam}, s.SearchNameUA) > 0 THEN 1 ELSE 0 END AS SearchNameUA_Match,
        CASE WHEN PATINDEX({termParam}, s.SearchDescriptionUA) > 0 THEN 1 ELSE 0 END AS SearchDescriptionUA_Match,
        CASE WHEN PATINDEX({termParam}, s.SearchVendorCode) > 0 THEN 1 ELSE 0 END AS SearchVendorCode_Match,
        CASE WHEN PATINDEX({termParam}, s.SearchSize) > 0 THEN 1 ELSE 0 END AS SearchSize_Match,
        CASE WHEN PATINDEX({termParam}, s.MainOriginalNumber) > 0 THEN 1 ELSE 0 END AS OriginalNumber_Match");
            } else {
                sql.Append($@",
        CASE WHEN s.SearchName_Match > 0 OR PATINDEX({termParam}, s.SearchName) > 0 THEN 1 ELSE 0 END AS SearchName_Match,
        CASE WHEN s.SearchDescription_Match > 0 OR PATINDEX({termParam}, s.SearchDescription) > 0 THEN 1 ELSE 0 END AS SearchDescription_Match,
        CASE WHEN s.SearchNameUA_Match > 0 OR PATINDEX({termParam}, s.SearchNameUA) > 0 THEN 1 ELSE 0 END AS SearchNameUA_Match,
        CASE WHEN s.SearchDescriptionUA_Match > 0 OR PATINDEX({termParam}, s.SearchDescriptionUA) > 0 THEN 1 ELSE 0 END AS SearchDescriptionUA_Match,
        CASE WHEN s.SearchVendorCode_Match > 0 OR PATINDEX({termParam}, s.SearchVendorCode) > 0 THEN 1 ELSE 0 END AS SearchVendorCode_Match,
        CASE WHEN s.SearchSize_Match > 0 OR PATINDEX({termParam}, s.SearchSize) > 0 THEN 1 ELSE 0 END AS SearchSize_Match,
        CASE WHEN s.OriginalNumber_Match > 0 OR PATINDEX({termParam}, s.MainOriginalNumber) > 0 THEN 1 ELSE 0 END AS OriginalNumber_Match");
            }

            sql.Append($@"
    FROM {prevStage} s
    WHERE
        s.SearchVendorCode LIKE {termParam}
        OR s.MainOriginalNumber LIKE {termParam}
        OR s.SearchNameUA LIKE {termParam}
        OR s.SearchName LIKE {termParam}
        OR s.SearchDescription LIKE {termParam}
        OR s.SearchDescriptionUA LIKE {termParam}
        OR s.SearchSize LIKE {termParam}
)");
        }

        string finalProductStage = $"ProductStage{terms.Length}";

        // === ORIGINAL NUMBER WATERFALL ===
        sql.Append(@"
, OrigNumStage0 AS (
    SELECT
        p.ID,
        MAX(p.SearchNameUA) AS SearchName,
        MAX(CASE WHEN ISNULL(pa.Amount, 0) > 0 THEN 1 ELSE 0 END) AS Available,
        MAX(CASE WHEN LOWER(RTRIM(LTRIM(p.MainOriginalNumber))) = @FullValue THEN 1 ELSE 0 END) AS MainOriginalNumberExact,
        MAX(CASE WHEN LOWER(RTRIM(LTRIM(on_.Number))) = @FullValue THEN 1 ELSE 0 END) AS HundredPercentMatch,
        0 AS SearchName_Match,
        0 AS SearchDescription_Match,
        0 AS SearchNameUA_Match,
        0 AS SearchDescriptionUA_Match,
        0 AS SearchVendorCode_Match,
        0 AS SearchSize_Match,
        1 AS OriginalNumber_Match
    FROM OriginalNumber on_
    INNER JOIN ProductOriginalNumber pon ON pon.OriginalNumberID = on_.ID AND pon.Deleted = 0
    INNER JOIN Product p ON p.ID = pon.ProductID AND p.Deleted = 0
    LEFT JOIN ProductAvailability pa ON pa.ProductID = p.ID
    WHERE PATINDEX(@Term0, on_.Number) > 0
    GROUP BY p.ID
)");

        for (int i = 1; i < terms.Length; i++) {
            string termParam = $"@Term{i}";
            string prevStage = $"OrigNumStage{i - 1}";
            string currStage = $"OrigNumStage{i}";

            sql.Append($@"
, {currStage} AS (
    SELECT s.*
    FROM {prevStage} s
    WHERE EXISTS (
        SELECT 1
        FROM OriginalNumber on_
        INNER JOIN ProductOriginalNumber pon ON pon.OriginalNumberID = on_.ID AND pon.Deleted = 0
        WHERE pon.ProductID = s.ID
          AND PATINDEX({termParam}, on_.Number) > 0
    )
)");
        }

        string finalOrigNumStage = $"OrigNumStage{terms.Length - 1}";

        // === UNION AND RANKING ===
        sql.Append($@"
, United_CTE AS (
    SELECT ID, SearchNameUA AS SearchName, Available, MainOriginalNumberExact, HundredPercentMatch,
           SearchName_Match, SearchDescription_Match, SearchNameUA_Match, SearchDescriptionUA_Match,
           SearchVendorCode_Match, SearchSize_Match, OriginalNumber_Match
    FROM {finalProductStage}
    UNION
    SELECT ID, SearchName, Available, MainOriginalNumberExact, HundredPercentMatch,
           SearchName_Match, SearchDescription_Match, SearchNameUA_Match, SearchDescriptionUA_Match,
           SearchVendorCode_Match, SearchSize_Match, OriginalNumber_Match
    FROM {finalOrigNumStage}
)
, Rowed_CTE AS (
    SELECT
        p.ID,
        MAX(p.HundredPercentMatch) AS HundredPercentMatch,
        MAX(p.Available) AS Available,
        ROW_NUMBER() OVER(ORDER BY
            MAX(p.MainOriginalNumberExact) DESC,
            MAX(p.Available) DESC,
            MAX(p.HundredPercentMatch) DESC,
            (CASE WHEN MAX(p.OriginalNumber_Match) > 0 THEN 1 ELSE 0 END) DESC,
            (CASE WHEN MAX(p.SearchVendorCode_Match) > 0 THEN 1 ELSE 0 END) DESC,
            (CASE WHEN MAX(p.OriginalNumber_Match) > 0 OR MAX(p.SearchName_Match) > 0 OR MAX(p.SearchNameUA_Match) > 0 THEN 1 ELSE 0 END) DESC,
            (CASE WHEN MAX(p.SearchDescriptionUA_Match) > 0 OR MAX(p.SearchDescription_Match) > 0 THEN 1 ELSE 0 END) DESC,
            (CASE WHEN MAX(p.SearchSize_Match) > 0 THEN 1 ELSE 0 END) DESC,
            MAX(p.SearchName),
            p.ID
        ) AS RowNumber
    FROM United_CTE p
    GROUP BY p.ID
)
SELECT
    ID AS Id,
    RowNumber,
    CAST(HundredPercentMatch AS BIT) AS HunderdPrecentMatch,
    CAST(Available AS BIT) AS Available
FROM Rowed_CTE
WHERE RowNumber > @Offset AND RowNumber <= @Limit + @Offset
ORDER BY RowNumber;
");

        return connection.Query<SearchResult>(sql.ToString(), parameters).AsList();
    }
}
