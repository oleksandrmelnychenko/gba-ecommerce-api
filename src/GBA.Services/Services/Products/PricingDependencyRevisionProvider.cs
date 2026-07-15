using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Dapper;

namespace GBA.Services.Services.Products;

public sealed record PricingDependencyRevisions(
    string ProductPricing,
    string PricingHierarchy,
    string Discounts,
    string ExchangeRates) {
    public static PricingDependencyRevisions Unavailable { get; } = new("", "", "", "");

    public bool IsValid => !string.IsNullOrWhiteSpace(ProductPricing)
                           && !string.IsNullOrWhiteSpace(PricingHierarchy)
                           && !string.IsNullOrWhiteSpace(Discounts)
                           && !string.IsNullOrWhiteSpace(ExchangeRates);

    public bool MatchesExactly(PricingDependencyRevisions other) {
        return IsValid
               && other != null
               && other.IsValid
               && string.Equals(ProductPricing, other.ProductPricing, StringComparison.Ordinal)
               && string.Equals(PricingHierarchy, other.PricingHierarchy, StringComparison.Ordinal)
               && string.Equals(Discounts, other.Discounts, StringComparison.Ordinal)
               && string.Equals(ExchangeRates, other.ExchangeRates, StringComparison.Ordinal);
    }

    internal static PricingDependencyRevisions FromChangeToken(string changeToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(changeToken);
        return new PricingDependencyRevisions(
            $"product-pricing:{changeToken}",
            $"pricing-hierarchy:{changeToken}",
            $"discounts:{changeToken}",
            $"exchange-rates:{changeToken}");
    }
}

public sealed record PricingChangeTrackingStatus(
    PricingDependencyRevisions Revisions,
    int ExpectedTrackedTableCount,
    int ActualTrackedTableCount,
    int MissingTrackedTableCount,
    int ExtraTrackedTableCount,
    int ExpectedPriceFunctionCount,
    int ActualPriceFunctionCount,
    int UnlistedPriceInputCount,
    int NonInputManifestEntryCount,
    int ActualPricingModuleCount = -1,
    int UnreadablePricingModuleCount = -1,
    bool RecoveryIncarnationPresent = false,
    bool RecoveryLineageMatches = false,
    int UnreadableTrackedTableIdentityCount = -1,
    int UnresolvedPriceDependencyCount = -1,
    int CrossDatabasePriceDependencyCount = -1,
    int SynonymBackedPriceDependencyCount = -1,
    long RepairGeneration = 0,
    bool RepairFenceValid = false) {
    public bool IsAvailable => Revisions.IsValid;

    public IReadOnlyDictionary<string, object> ToHealthData() {
        return new Dictionary<string, object> {
            ["expectedTrackedTableCount"] = ExpectedTrackedTableCount,
            ["actualTrackedTableCount"] = ActualTrackedTableCount,
            ["missingTrackedTableCount"] = MissingTrackedTableCount,
            ["extraTrackedTableCount"] = ExtraTrackedTableCount,
            ["expectedPriceFunctionCount"] = ExpectedPriceFunctionCount,
            ["actualPriceFunctionCount"] = ActualPriceFunctionCount,
            ["unlistedPriceInputCount"] = UnlistedPriceInputCount,
            ["nonInputManifestEntryCount"] = NonInputManifestEntryCount,
            ["actualPricingModuleCount"] = ActualPricingModuleCount,
            ["unreadablePricingModuleCount"] = UnreadablePricingModuleCount,
            ["unreadableTrackedTableIdentityCount"] = UnreadableTrackedTableIdentityCount,
            ["unresolvedPriceDependencyCount"] = UnresolvedPriceDependencyCount,
            ["crossDatabasePriceDependencyCount"] = CrossDatabasePriceDependencyCount,
            ["synonymBackedPriceDependencyCount"] = SynonymBackedPriceDependencyCount,
            ["recoveryIncarnationPresent"] = RecoveryIncarnationPresent,
            ["recoveryLineageMatches"] = RecoveryLineageMatches,
            ["recoveryRotationRequired"] = RecoveryIncarnationPresent
                                           && !RecoveryLineageMatches,
            ["repairGeneration"] = RepairGeneration,
            ["repairFenceValid"] = RepairFenceValid
        };
    }

    public static PricingChangeTrackingStatus QueryFailed(int expectedTrackedTableCount) {
        return new PricingChangeTrackingStatus(
            PricingDependencyRevisions.Unavailable,
            expectedTrackedTableCount,
            -1,
            -1,
            -1,
            SqlPricingDependencyRevisionProvider.ExpectedPriceFunctionCount,
            -1,
            -1,
            -1);
    }
}

public interface IPricingDependencyRevisionProvider {
    PricingDependencyRevisions Get(IDbConnection connection);

    PricingChangeTrackingStatus GetStatus(IDbConnection connection);
}

/// <summary>
/// Uses one database-wide Change Tracking version only when the global tracked-table set exactly
/// matches the pricing allowlist and the allowlist still matches the transitive inputs of both
/// ecommerce price functions. Currency is an explicit input of the price projection. OrderItem stays
/// tracked even though current cache and search calls pass a null order-item ID, so a future function
/// change cannot silently make that branch affect indexed prices. Exchange-rate tables are not
/// currently referenced by either price-function root; dependency drift fails closed.
/// The revision also includes a deterministic hash manifest for every transitive SQL module and an
/// exact tracked-table identity/begin-version manifest. An explicitly rotated recovery incarnation
/// and repair generation are bound to the current SQL Server recovery fork.
/// </summary>
public sealed class SqlPricingDependencyRevisionProvider : IPricingDependencyRevisionProvider {
    public const int ExpectedTrackedTableCount = 15;
    public const int ExpectedPriceFunctionCount = 2;
    private const string ChangeTokenVersion = "v4";
    private const string RuntimeStateProcedureSql =
        "EXEC dbo.GetEcommercePricingChangeTrackingState;";

    private const string RequiredDependencyValuesSql = @"
        (N'dbo', N'Agreement'),
        (N'dbo', N'ClientAgreement'),
        (N'dbo', N'Currency'),
        (N'dbo', N'OrderItem'),
        (N'dbo', N'Organization'),
        (N'dbo', N'Pricing'),
        (N'dbo', N'PricingProductGroupDiscount'),
        (N'dbo', N'PricingSourceCutoverState'),
        (N'dbo', N'PricingSourceDefinition'),
        (N'dbo', N'PricingSourceSyncState'),
        (N'dbo', N'Product'),
        (N'dbo', N'ProductGroupDiscount'),
        (N'dbo', N'ProductPricing'),
        (N'dbo', N'ProductPricingSourceSnapshot'),
        (N'dbo', N'ProductProductGroup')";


    public PricingDependencyRevisions Get(IDbConnection connection) {
        return GetStatus(connection).Revisions;
    }

    public PricingChangeTrackingStatus GetStatus(IDbConnection connection) {
        ArgumentNullException.ThrowIfNull(connection);

        try {
            PricingChangeTrackingState state = connection.QuerySingle<PricingChangeTrackingState>(
                RuntimeStateProcedureSql);
            PricingDependencyRevisions revisions = state.IsUsable
                ? PricingDependencyRevisions.FromChangeToken(
                    CreateChangeToken(state))
                : PricingDependencyRevisions.Unavailable;

            return new PricingChangeTrackingStatus(
                revisions,
                state.ExpectedTrackedTableCount,
                state.ActualTrackedTableCount,
                state.MissingTrackedTableCount,
                state.ExtraTrackedTableCount,
                state.ExpectedPriceFunctionCount,
                state.ActualPriceFunctionCount,
                state.UnlistedPriceInputCount,
                state.NonInputManifestEntryCount,
                state.ActualPricingModuleCount,
                state.UnreadablePricingModuleCount,
                state.RecoveryIncarnationPresent,
                state.RecoveryLineageMatches,
                state.UnreadableTrackedTableIdentityCount,
                state.UnresolvedPriceDependencyCount,
                state.CrossDatabasePriceDependencyCount,
                state.SynonymBackedPriceDependencyCount,
                state.RepairGeneration,
                state.RepairFenceValid);
        } catch (DbException) {
            return PricingChangeTrackingStatus.QueryFailed(ExpectedTrackedTableCount);
        } catch (DataException) {
            return PricingChangeTrackingStatus.QueryFailed(ExpectedTrackedTableCount);
        } catch (InvalidOperationException) {
            return PricingChangeTrackingStatus.QueryFailed(ExpectedTrackedTableCount);
        }
    }

    private static string CreateChangeToken(PricingChangeTrackingState state) {
        byte[] manifest = Encoding.UTF8.GetBytes(
            state.PricingModuleHashManifest + "\n" + state.PricingTrackedTableManifest);
        string moduleHash = Convert.ToHexString(SHA256.HashData(manifest));
        return $"{ChangeTokenVersion}:{state.RecoveryIncarnationId:N}:"
               + $"{state.CurrentRecoveryForkId:N}:"
               + $"{state.RepairGeneration}:{moduleHash}:{state.CurrentVersion!.Value}";
    }

    private sealed class PricingChangeTrackingState {
        public Guid CurrentRecoveryForkId { get; set; }
        public Guid RecordedRecoveryForkId { get; set; }
        public Guid RecoveryIncarnationId { get; set; }
        public long RepairGeneration { get; set; }
        public bool RecoveryIncarnationPresent { get; set; }
        public bool RecoveryLineageMatches { get; set; }
        public bool RepairFenceValid { get; set; }
        public long? CurrentVersion { get; set; }
        public int ExpectedTrackedTableCount { get; set; }
        public int ActualTrackedTableCount { get; set; }
        public int MissingTrackedTableCount { get; set; }
        public int ExtraTrackedTableCount { get; set; }
        public int UnreadableTrackedTableIdentityCount { get; set; }
        public string PricingTrackedTableManifest { get; set; } = string.Empty;
        public int ExpectedPriceFunctionCount { get; set; }
        public int ActualPriceFunctionCount { get; set; }
        public int ActualPricingModuleCount { get; set; }
        public int UnreadablePricingModuleCount { get; set; }
        public string PricingModuleHashManifest { get; set; } = string.Empty;
        public int UnresolvedPriceDependencyCount { get; set; }
        public int CrossDatabasePriceDependencyCount { get; set; }
        public int SynonymBackedPriceDependencyCount { get; set; }
        public int UnlistedPriceInputCount { get; set; }
        public int NonInputManifestEntryCount { get; set; }

        public bool IsUsable => RecoveryIncarnationPresent
                                && RecoveryLineageMatches
                                && RecoveryIncarnationId != Guid.Empty
                                && CurrentRecoveryForkId != Guid.Empty
                                && RecordedRecoveryForkId == CurrentRecoveryForkId
                                && RepairGeneration > 0
                                && RepairFenceValid
                                && CurrentVersion is >= 0
                                && ExpectedTrackedTableCount == SqlPricingDependencyRevisionProvider.ExpectedTrackedTableCount
                                && ActualTrackedTableCount == ExpectedTrackedTableCount
                                && MissingTrackedTableCount == 0
                                && ExtraTrackedTableCount == 0
                                && UnreadableTrackedTableIdentityCount == 0
                                && !string.IsNullOrWhiteSpace(PricingTrackedTableManifest)
                                && ExpectedPriceFunctionCount == SqlPricingDependencyRevisionProvider.ExpectedPriceFunctionCount
                                && ActualPriceFunctionCount == ExpectedPriceFunctionCount
                                && ActualPricingModuleCount >= ExpectedPriceFunctionCount
                                && UnreadablePricingModuleCount == 0
                                && !string.IsNullOrWhiteSpace(PricingModuleHashManifest)
                                && UnresolvedPriceDependencyCount == 0
                                && CrossDatabasePriceDependencyCount == 0
                                && SynonymBackedPriceDependencyCount == 0
                                && UnlistedPriceInputCount == 0
                                && NonInputManifestEntryCount == 0;
    }
}
