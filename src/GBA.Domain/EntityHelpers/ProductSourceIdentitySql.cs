#nullable enable

using System;

namespace GBA.Domain.EntityHelpers;

/// <summary>Builds product-source keys and source-world SQL predicates.</summary>
public static class ProductSourceIdentitySql {
    public const string Fenix = "fenix";
    public const string Amg = "amg";

    /// <summary>Normalizes a supported product source world.</summary>
    public static bool TryNormalizeSourceWorld(string? sourceWorld, out string normalizedSourceWorld) {
        normalizedSourceWorld = string.Empty;
        if (string.IsNullOrWhiteSpace(sourceWorld)) return false;

        string candidate = sourceWorld.Trim().ToLowerInvariant();
        if (candidate is not (Fenix or Amg)) return false;

        normalizedSourceWorld = candidate;
        return true;
    }

    /// <summary>Returns the source world permanently assigned to an organization.</summary>
    public static string FromOrganization(bool priceSourceIsAmg) => priceSourceIsAmg ? Amg : Fenix;

    public static string CanonicalExpression(string productAlias, string sourceSystem) {
        ArgumentException.ThrowIfNullOrWhiteSpace(productAlias);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceSystem);

        string normalizedSystem = sourceSystem.Trim().ToLowerInvariant();
        string idColumn;
        string codeColumn;
        switch (normalizedSystem) {
            case "fenix":
                idColumn = $"{productAlias}.SourceFenixID";
                codeColumn = $"{productAlias}.SourceFenixCode";
                break;
            case "amg":
                idColumn = $"{productAlias}.SourceAmgID";
                codeColumn = $"{productAlias}.SourceAmgCode";
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(sourceSystem),
                    sourceSystem,
                    "Only Fenix and AMG product sources are supported.");
        }

        return $@"CASE
    WHEN ISNULL(DATALENGTH({idColumn}), 0) > 0
        THEN CONCAT(
            '{normalizedSystem}:id-', CONVERT(varchar(128), {idColumn}, 2),
            CASE WHEN {codeColumn} IS NOT NULL
                THEN CONCAT('|code-', CONVERT(varchar(20), {codeColumn}))
                ELSE '' END)
    WHEN {codeColumn} IS NOT NULL
        THEN CONCAT('{normalizedSystem}:code-', CONVERT(varchar(20), {codeColumn}))
    ELSE ''
END";
    }

    /// <summary>
    /// Selects active products from the requested source world. The agreement and product
    /// are different external entities, so their external IDs must never be compared.
    /// </summary>
    public static string SourceWorldPredicate(
        string productAlias,
        string parameterName = "@CatalogSource") {
        ArgumentException.ThrowIfNullOrWhiteSpace(productAlias);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);

        string fenix = CanonicalExpression(productAlias, "fenix");
        string amg = CanonicalExpression(productAlias, "amg");
        return $@"(
    ({parameterName} = '{Fenix}' AND ({fenix}) <> '')
 OR ({parameterName} = '{Amg}' AND ({amg}) <> '')
)";
    }

    /// <summary>Chooses the lowest active Product.ID for one exact source key.</summary>
    public static string CanonicalProductPredicate(
        string productAlias,
        string parameterName = "@CatalogSource") {
        ArgumentException.ThrowIfNullOrWhiteSpace(productAlias);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);

        return $@"(
    ({parameterName} = '{Fenix}' AND {CanonicalProductForSourcePredicate(productAlias, Fenix)})
 OR ({parameterName} = '{Amg}' AND {CanonicalProductForSourcePredicate(productAlias, Amg)})
)";
    }

    /// <summary>
    /// Keeps a source-neutral row when it is canonical in at least one of its source worlds.
    /// A dual-source row can therefore remain available in one world even when another row
    /// is canonical in the other world.
    /// </summary>
    public static string AnyCanonicalSourcePredicate(string productAlias) {
        string fenixKey = CanonicalExpression(productAlias, Fenix);
        string amgKey = CanonicalExpression(productAlias, Amg);
        return $@"(
    (({fenixKey}) <> '' AND {CanonicalProductForSourcePredicate(productAlias, Fenix)})
 OR (({amgKey}) <> '' AND {CanonicalProductForSourcePredicate(productAlias, Amg)})
)";
    }

    /// <summary>Returns the deterministic canonical predicate for a known source world.</summary>
    public static string CanonicalProductForSourcePredicate(
        string productAlias,
        string sourceSystem) {
        ArgumentException.ThrowIfNullOrWhiteSpace(productAlias);
        if (!TryNormalizeSourceWorld(sourceSystem, out string normalizedSource))
            throw new ArgumentOutOfRangeException(nameof(sourceSystem), sourceSystem, "Unsupported source world.");

        const string candidate = "canonicalProduct";
        return $@"NOT EXISTS (
    SELECT 1
    FROM Product {candidate}
    WHERE {candidate}.Deleted = 0
      AND {candidate}.ID < {productAlias}.ID
      AND {ExactSourceKeyPredicate(candidate, productAlias, normalizedSource)}
)";
    }

    /// <summary>Compares two product rows inside one external source world.</summary>
    public static string SameSourceEntityPredicate(
        string leftAlias,
        string rightAlias,
        string sourceSystem) {
        ArgumentException.ThrowIfNullOrWhiteSpace(leftAlias);
        ArgumentException.ThrowIfNullOrWhiteSpace(rightAlias);
        if (!TryNormalizeSourceWorld(sourceSystem, out string normalizedSource))
            throw new ArgumentOutOfRangeException(nameof(sourceSystem), sourceSystem, "Unsupported source world.");

        return ExactSourceKeyPredicate(leftAlias, rightAlias, normalizedSource);
    }

    private static string ExactSourceKeyPredicate(
        string leftAlias,
        string rightAlias,
        string sourceSystem) {
        (string leftId, string leftCode) = SourceColumns(leftAlias, sourceSystem);
        (string rightId, string rightCode) = SourceColumns(rightAlias, sourceSystem);
        return $@"(
    (ISNULL(DATALENGTH({leftId}), 0) > 0
     AND ISNULL(DATALENGTH({rightId}), 0) > 0
     AND {rightId} = {leftId}
     AND (({leftCode} IS NULL AND {rightCode} IS NULL)
       OR {rightCode} = {leftCode}))
 OR (ISNULL(DATALENGTH({leftId}), 0) = 0
     AND ISNULL(DATALENGTH({rightId}), 0) = 0
     AND {leftCode} IS NOT NULL
     AND {rightCode} = {leftCode})
)";
    }

    private static (string Id, string Code) SourceColumns(
        string productAlias,
        string sourceSystem) {
        return sourceSystem == Fenix
            ? ($"{productAlias}.SourceFenixID", $"{productAlias}.SourceFenixCode")
            : ($"{productAlias}.SourceAmgID", $"{productAlias}.SourceAmgCode");
    }

    /// <summary>Combines source-world membership with deterministic duplicate removal.</summary>
    public static string CanonicalSourceWorldPredicate(
        string productAlias,
        string parameterName = "@CatalogSource") {
        return $"({SourceWorldPredicate(productAlias, parameterName)}) AND ({CanonicalProductPredicate(productAlias, parameterName)})";
    }
}
