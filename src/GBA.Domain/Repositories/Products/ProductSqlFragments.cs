namespace GBA.Domain.Repositories.Products;

/// <summary>
/// Cached SQL fragments for product queries to avoid repeated string allocations.
/// All strings use Ukrainian (UA) locale only.
/// </summary>
public static class ProductSqlFragments {
    // Product Name/Description/Notes columns (Ukrainian only)
    public const string ProductCultureColumns = ", [Product].[NameUA] AS [Name] , [Product].[DescriptionUA] AS [Description] , [Product].[NotesUA] AS [Notes] ";

    // Common product base columns (used in most queries)
    public const string ProductBaseColumns =
        "[Product].ID " +
        ", [Product].Created " +
        ", [Product].Deleted ";

    // Common product detail columns
    public const string ProductDetailColumns =
        ", [Product].HasAnalogue " +
        ", [Product].HasComponent " +
        ", [Product].HasImage " +
        ", [Product].[Image] " +
        ", [Product].IsForSale " +
        ", [Product].IsForWeb " +
        ", [Product].IsForZeroSale " +
        ", [Product].MainOriginalNumber " +
        ", [Product].MeasureUnitID " +
        ", [Product].NetUID " +
        ", [Product].OrderStandard " +
        ", [Product].PackingStandard " +
        ", [Product].Size " +
        ", [Product].[Top] " +
        ", [Product].UCGFEA " +
        ", [Product].Updated " +
        ", [Product].VendorCode " +
        ", [Product].Volume " +
        ", [Product].[Weight] ";

    // Extended detail columns (includes Standard field)
    public const string ProductDetailColumnsWithStandard =
        ", [Product].HasAnalogue " +
        ", [Product].HasComponent " +
        ", [Product].HasImage " +
        ", [Product].[Image] " +
        ", [Product].IsForSale " +
        ", [Product].IsForWeb " +
        ", [Product].IsForZeroSale " +
        ", [Product].MainOriginalNumber " +
        ", [Product].MeasureUnitID " +
        ", [Product].NetUID " +
        ", [Product].OrderStandard " +
        ", [Product].PackingStandard " +
        ", [Product].Standard " +
        ", [Product].Size " +
        ", [Product].[Top] " +
        ", [Product].UCGFEA " +
        ", [Product].Updated " +
        ", [Product].VendorCode " +
        ", [Product].Volume " +
        ", [Product].[Weight] ";

    // Common joins for product queries
    public const string ProductAvailabilityJoin =
        "LEFT JOIN [ProductAvailability] " +
        "ON [ProductAvailability].ProductID = [Product].ID " +
        "AND [ProductAvailability].Deleted = 0 ";

    public const string StorageJoin =
        "LEFT JOIN [Storage] " +
        "ON [Storage].ID = [ProductAvailability].StorageID ";

    public const string MeasureUnitViewJoin =
        "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
        "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
        "AND [MeasureUnit].CultureCode = @Culture ";

    public const string ProductImageJoin =
        "LEFT JOIN [ProductImage] " +
        "ON [ProductImage].ProductID = [Product].ID " +
        "AND [ProductImage].Deleted = 0 ";

    public const string OrganizationJoin =
        "LEFT JOIN [Organization] " +
        "ON [Organization].StorageID = [Storage].ID ";

    public const string ProductPricingJoin =
        "LEFT JOIN [ProductPricing] " +
        "ON [ProductPricing].ProductID = [Product].ID " +
        "AND [ProductPricing].Deleted = 0 ";

    // Analogue Name/Description/Notes columns (Ukrainian only)
    public const string AnalogueCultureColumns = ", [Analogue].[NameUA] AS [Name] , [Analogue].[DescriptionUA] AS [Description] , [Analogue].[NotesUA] AS [Notes] ";

    // Analogue Name/Description only (no Notes) - compact format
    public const string AnalogueNameDescriptionColumns = ",[Analogue].[NameUA] AS [Name] ,[Analogue].[DescriptionUA] AS [Description] ";

    // Component Name/Description/Notes columns (Ukrainian only)
    public const string ComponentCultureColumns = ", [Component].[NameUA] AS [Name] , [Component].[DescriptionUA] AS [Description] , [Component].[NotesUA] AS [Notes] ";

    // BaseProduct Name/Description/Notes columns (Ukrainian only)
    public const string BaseProductCultureColumns = ", [BaseProduct].[NameUA] AS [Name] , [BaseProduct].[DescriptionUA] AS [Description] , [BaseProduct].[NotesUA] AS [Notes] ";

    // analogueProduct alias Name/Description only (Ukrainian only)
    public const string AnalogueProductAliasColumns = ",[analogueProduct].[NameUA] AS [Name] ,[analogueProduct].[DescriptionUA] AS [Description] ";

    // ProductComponent alias Name/Description only (Ukrainian only)
    public const string ProductComponentAliasColumns = ",[ProductComponent].[NameUA] AS [Name] ,[ProductComponent].[DescriptionUA] AS [Description] ";
}
