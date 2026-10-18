namespace GBA.Domain.Repositories.Products;

internal static class EcommerceStorageScope {
    // Stock follows the selected agreement organization, while VAT remains a pricing and
    // presentation concern. Synced data can express storage ownership in either direction;
    // both source links stay valid until the legacy relation is migrated. Do not broaden this
    // scope with AvailableForReSale or ForVatProducts: that mixes another organization's stock
    // into the quantity the current agreement can reserve.
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
