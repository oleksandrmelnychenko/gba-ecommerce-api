namespace GBA.Domain.Repositories.Products;

internal static class EcommerceStorageScope {
    // Synced data can express the same ownership in either direction. Both links
    // are source IDs and must remain valid until the legacy relation is migrated.
    internal const string NamedStorageSql =
        "([Storage].OrganizationID = @OrganizationId " +
        "OR EXISTS (" +
        "SELECT 1 FROM [Organization] AS [AgreementOrganization] " +
        "WHERE [AgreementOrganization].ID = @OrganizationId " +
        "AND [AgreementOrganization].Deleted = 0 " +
        "AND [AgreementOrganization].StorageID = [Storage].ID))";

    internal const string AliasedStorageSql =
        "(s.OrganizationID = @OrganizationId " +
        "OR EXISTS (" +
        "SELECT 1 FROM [Organization] AS [AgreementOrganization] " +
        "WHERE [AgreementOrganization].ID = @OrganizationId " +
        "AND [AgreementOrganization].Deleted = 0 " +
        "AND [AgreementOrganization].StorageID = s.ID))";

    internal static bool MatchesOrganization(
        long? storageOrganizationId,
        long storageId,
        long organizationId,
        long? organizationStorageId) {
        return storageOrganizationId == organizationId || organizationStorageId == storageId;
    }
}
