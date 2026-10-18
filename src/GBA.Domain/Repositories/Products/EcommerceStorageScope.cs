namespace GBA.Domain.Repositories.Products;

internal static class EcommerceStorageScope {
    internal const string NamedStorageSql =
        "EXISTS (" +
        "SELECT 1 FROM [Organization] AS [AgreementOrganization] " +
        "WHERE [AgreementOrganization].ID = @OrganizationId " +
        "AND [AgreementOrganization].Deleted = 0 " +
        "AND [AgreementOrganization].StorageID = [Storage].ID)";

    internal const string AliasedStorageSql =
        "EXISTS (" +
        "SELECT 1 FROM [Organization] AS [AgreementOrganization] " +
        "WHERE [AgreementOrganization].ID = @OrganizationId " +
        "AND [AgreementOrganization].Deleted = 0 " +
        "AND [AgreementOrganization].StorageID = s.ID)";

    internal const string ProductSellableQuantitySql =
        "ISNULL((" +
        "SELECT SUM(CASE WHEN [ScopedAvailability].Amount > 0 THEN [ScopedAvailability].Amount ELSE 0 END) " +
        "FROM [ProductAvailability] AS [ScopedAvailability] " +
        "INNER JOIN [Storage] AS [ScopedStorage] " +
        "ON [ScopedStorage].ID = [ScopedAvailability].StorageID " +
        "AND [ScopedStorage].Deleted = 0 " +
        "AND [ScopedStorage].ForDefective = 0 " +
        "INNER JOIN [Organization] AS [ScopedOrganization] " +
        "ON [ScopedOrganization].ID = @OrganizationId " +
        "AND [ScopedOrganization].Deleted = 0 " +
        "AND [ScopedOrganization].StorageID = [ScopedStorage].ID " +
        "WHERE [ScopedAvailability].ProductID = [Product].ID " +
        "AND [ScopedAvailability].Deleted = 0), 0)";

    internal static bool MatchesOrganization(long storageId, long? organizationStorageId) {
        return organizationStorageId == storageId;
    }
}
