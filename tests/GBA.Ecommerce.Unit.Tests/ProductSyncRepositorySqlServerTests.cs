using System.Data;
using Dapper;
using GBA.Search.Sync;
using Microsoft.Data.SqlClient;

namespace GBA.Ecommerce.Unit.Tests;

[Collection("EcommerceSqlIntegration")]
public sealed class ProductSyncRepositorySqlServerTests {
    private const string ConnectionStringEnvironmentVariable =
        "GBA_ECOMMERCE_SQL_INTEGRATION_CONNECTION_STRING";
    private const string DisposableDatabaseNamePrefix = "GbaEcommerceRevisionTests";

    [Fact]
    public async Task ProductionProjection_UsesSourceWorldAndDeterministicCanonicalProducts() {
        string? connectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        using (SqlConnection setupConnection = new(connectionString)) {
            setupConnection.Open();
            EnsureDisposableDatabase(setupConnection);
            ResetProjectionSchema(setupConnection);
            SeedProjectionData(setupConnection);
        }

        ProductSyncRepository repository = new(
            () => new SqlConnection(connectionString));

        ProductProjectionBatch batch = await repository.GetProductProjectionBatchAsync(
            afterProductId: 0,
            take: 100);

        Assert.True(batch.HasValidRetailConfiguration);
        Assert.Equal(7, batch.Products.Count);
        Assert.DoesNotContain(batch.Products, product => product.Id is 4 or 5);
        Assert.Contains(batch.Products, product => product.Id == 7);
        Assert.Contains(batch.Products, product => product.Id == 8);
        Assert.Contains(batch.Products, product => product.Id == 9);

        ProductSyncData pureFenix = Assert.Single(batch.Products, product => product.Id == 1);
        Assert.True(pureFenix.IsCanonicalFenix);
        Assert.False(pureFenix.IsCanonicalAmg);

        ProductSyncData pureAmg = Assert.Single(batch.Products, product => product.Id == 2);
        Assert.Equal(string.Empty, pureAmg.ProductSourceFenix);
        Assert.Equal("amg:id-CC|code-22", pureAmg.ProductSourceAmg);
        Assert.False(pureAmg.IsCanonicalFenix);
        Assert.True(pureAmg.IsCanonicalAmg);
        ProductCatalogScopeData pureAmgScope = Assert.Single(pureAmg.CatalogScopes);
        Assert.Equal(2, pureAmgScope.OrganizationId);
        Assert.Equal("amg", pureAmgScope.SourceSystem);
        Assert.False(pureAmgScope.WithVat);
        Assert.Equal(7, pureAmgScope.AvailableQty);

        ProductSyncData dualSource = Assert.Single(batch.Products, product => product.Id == 3);
        Assert.Equal("fenix:id-BA|code-31", dualSource.ProductSourceFenix);
        Assert.Equal("amg:id-DA|code-32", dualSource.ProductSourceAmg);
        Assert.True(dualSource.IsCanonicalFenix);
        Assert.True(dualSource.IsCanonicalAmg);
        Assert.Contains(dualSource.CatalogScopes, scope =>
            scope.OrganizationId == 1
            && scope.SourceSystem == "fenix"
            && !scope.WithVat
            && scope.AvailableQty == 3);
        Assert.Contains(dualSource.CatalogScopes, scope =>
            scope.OrganizationId == 2
            && scope.SourceSystem == "amg"
            && !scope.WithVat
            && scope.AvailableQty == 4);
        Assert.Contains(dualSource.CatalogScopes, scope =>
            scope.OrganizationId == 2
            && scope.SourceSystem == "amg"
            && scope.WithVat
            && scope.AvailableQty == 2);

        ProductSyncData crossWorldDuplicate = Assert.Single(
            batch.Products,
            product => product.Id == 6);
        Assert.False(crossWorldDuplicate.IsCanonicalFenix);
        Assert.True(crossWorldDuplicate.IsCanonicalAmg);
        Assert.Equal("fenix:id-AA|code-11", crossWorldDuplicate.ProductSourceFenix);
        Assert.Equal("amg:id-EE|code-33", crossWorldDuplicate.ProductSourceAmg);
    }

    private static void EnsureDisposableDatabase(SqlConnection connection) {
        if (!connection.Database.StartsWith(
                DisposableDatabaseNamePrefix,
                StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException(
                $"{ConnectionStringEnvironmentVariable} must target a disposable database "
                + $"whose name starts with '{DisposableDatabaseNamePrefix}'.");
        }
    }

    private static void ResetProjectionSchema(IDbConnection connection) {
        connection.Execute(@"
DROP FUNCTION IF EXISTS dbo.GetCalculatedProductPriceForPricingSource;
DROP TABLE IF EXISTS dbo.ProductSlug;
DROP TABLE IF EXISTS dbo.ProductAvailability;
DROP TABLE IF EXISTS dbo.ClientAgreement;
DROP TABLE IF EXISTS dbo.Agreement;
DROP TABLE IF EXISTS dbo.Client;
DROP TABLE IF EXISTS dbo.Product;
DROP TABLE IF EXISTS dbo.Storage;
DROP TABLE IF EXISTS dbo.Organization;
DROP TABLE IF EXISTS dbo.Pricing;
DROP TABLE IF EXISTS dbo.Currency;

CREATE TABLE dbo.Organization (
    ID bigint NOT NULL PRIMARY KEY,
    Deleted bit NOT NULL,
    PriceSourceIsAmg bit NOT NULL
);
CREATE TABLE dbo.Storage (
    ID bigint NOT NULL PRIMARY KEY,
    OrganizationID bigint NOT NULL,
    ForVatProducts bit NOT NULL,
    Updated datetime2 NOT NULL,
    RetailPriority int NOT NULL,
    Deleted bit NOT NULL,
    ForEcommerce bit NOT NULL,
    ForDefective bit NOT NULL,
    Locale nvarchar(8) NULL
);
CREATE TABLE dbo.Client (
    ID bigint NOT NULL PRIMARY KEY,
    IsForRetail bit NOT NULL,
    IsActive bit NOT NULL,
    Deleted bit NOT NULL
);
CREATE TABLE dbo.Pricing (
    ID bigint NOT NULL PRIMARY KEY,
    NetUID uniqueidentifier NOT NULL,
    Deleted bit NOT NULL,
    BasePricingID bigint NULL,
    CalculatedExtraCharge decimal(18, 4) NULL
);
CREATE TABLE dbo.Currency (
    ID bigint NOT NULL PRIMARY KEY,
    Code nvarchar(16) NULL,
    Deleted bit NOT NULL
);
CREATE TABLE dbo.Agreement (
    ID bigint NOT NULL PRIMARY KEY,
    WithVATAccounting bit NOT NULL,
    OrganizationID bigint NOT NULL,
    PricingID bigint NOT NULL,
    CurrencyID bigint NOT NULL,
    SourceFenixID varbinary(64) NULL,
    SourceFenixCode bigint NULL,
    SourceAmgID varbinary(64) NULL,
    SourceAmgCode bigint NULL,
    Updated datetime2 NOT NULL,
    IsSelected bit NOT NULL,
    IsActive bit NOT NULL,
    Deleted bit NOT NULL,
    IsDefault bit NOT NULL
);
CREATE TABLE dbo.ClientAgreement (
    ID bigint NOT NULL PRIMARY KEY,
    ClientID bigint NOT NULL,
    AgreementID bigint NOT NULL,
    Deleted bit NOT NULL,
    Updated datetime2 NOT NULL,
    NetUID uniqueidentifier NOT NULL
);
CREATE TABLE dbo.Product (
    ID bigint NOT NULL PRIMARY KEY,
    NetUID uniqueidentifier NOT NULL,
    VendorCode nvarchar(64) NULL,
    SearchVendorCode nvarchar(64) NULL,
    Name nvarchar(128) NULL,
    NameUA nvarchar(128) NULL,
    Description nvarchar(256) NULL,
    DescriptionUA nvarchar(256) NULL,
    MainOriginalNumber nvarchar(64) NULL,
    Size nvarchar(64) NULL,
    SynonymsUA nvarchar(128) NULL,
    SearchSynonymsUA nvarchar(128) NULL,
    SearchName nvarchar(128) NULL,
    SearchNameUA nvarchar(128) NULL,
    SearchDescription nvarchar(256) NULL,
    SearchDescriptionUA nvarchar(256) NULL,
    SearchSize nvarchar(64) NULL,
    PackingStandard nvarchar(64) NULL,
    OrderStandard nvarchar(64) NULL,
    UCGFEA nvarchar(64) NULL,
    Volume nvarchar(64) NULL,
    [Top] nvarchar(64) NULL,
    Weight float NULL,
    HasAnalogue bit NOT NULL,
    HasComponent bit NOT NULL,
    HasImage bit NOT NULL,
    Image nvarchar(256) NULL,
    MeasureUnitID bigint NOT NULL,
    IsForWeb bit NOT NULL,
    IsForSale bit NOT NULL,
    IsForZeroSale bit NOT NULL,
    SourceFenixID varbinary(64) NULL,
    SourceFenixCode bigint NULL,
    SourceAmgID varbinary(64) NULL,
    SourceAmgCode bigint NULL,
    Updated datetime2 NOT NULL,
    Deleted bit NOT NULL
);
CREATE TABLE dbo.ProductAvailability (
    ID bigint NOT NULL PRIMARY KEY,
    ProductID bigint NOT NULL,
    StorageID bigint NOT NULL,
    Amount float NOT NULL,
    Deleted bit NOT NULL
);
CREATE TABLE dbo.ProductSlug (
    ID bigint NOT NULL PRIMARY KEY,
    ProductID bigint NOT NULL,
    NetUID uniqueidentifier NOT NULL,
    Url nvarchar(256) NULL,
    Locale nvarchar(8) NULL,
    Deleted bit NOT NULL
);

EXEC(N'CREATE FUNCTION dbo.GetCalculatedProductPriceForPricingSource (
    @ProductNetUid uniqueidentifier,
    @PricingNetUid uniqueidentifier,
    @ClientAgreementNetUid uniqueidentifier)
RETURNS decimal(18, 4)
AS
BEGIN
    RETURN 12.34;
END');");
    }

    private static void SeedProjectionData(IDbConnection connection) {
        connection.Execute(@"
DECLARE @Updated datetime2 = '2026-07-15T08:00:00';

INSERT dbo.Organization (ID, Deleted, PriceSourceIsAmg) VALUES
    (1, 0, 0),
    (2, 0, 1);
INSERT dbo.Storage
    (ID, OrganizationID, ForVatProducts, Updated, RetailPriority, Deleted, ForEcommerce, ForDefective, Locale)
VALUES
    (1, 1, 0, @Updated, 1, 0, 1, 0, 'uk'),
    (2, 1, 1, @Updated, 1, 0, 1, 0, 'uk'),
    (3, 2, 0, @Updated, 1, 0, 1, 0, 'uk'),
    (4, 2, 1, @Updated, 1, 0, 1, 0, 'pl');
INSERT dbo.Client (ID, IsForRetail, IsActive, Deleted) VALUES (1, 1, 1, 0);
INSERT dbo.Pricing (ID, NetUID, Deleted, BasePricingID, CalculatedExtraCharge) VALUES
    (1, '00000000-0000-0000-0000-000000000101', 0, NULL, 0),
    (2, '00000000-0000-0000-0000-000000000102', 0, NULL, 0);
INSERT dbo.Currency (ID, Code, Deleted) VALUES (1, 'UAH', 0);
INSERT dbo.Agreement
    (ID, WithVATAccounting, OrganizationID, PricingID, CurrencyID,
     SourceFenixID, SourceFenixCode, SourceAmgID, SourceAmgCode,
     Updated, IsSelected, IsActive, Deleted, IsDefault)
VALUES
    (1, 0, 1, 1, 1, 0xF101, 901, NULL, NULL, @Updated, 1, 1, 0, 1),
    (2, 1, 1, 2, 1, 0xF102, 902, NULL, NULL, @Updated, 1, 1, 0, 1);
INSERT dbo.ClientAgreement (ID, ClientID, AgreementID, Deleted, Updated, NetUID) VALUES
    (1, 1, 1, 0, @Updated, '00000000-0000-0000-0000-000000000201'),
    (2, 1, 2, 0, @Updated, '00000000-0000-0000-0000-000000000202');

INSERT dbo.Product
    (ID, NetUID, VendorCode, SearchVendorCode, Name, NameUA, Description, DescriptionUA,
     MainOriginalNumber, Size, SynonymsUA, SearchSynonymsUA, SearchName, SearchNameUA,
     SearchDescription, SearchDescriptionUA, SearchSize, PackingStandard, OrderStandard,
     UCGFEA, Volume, [Top], Weight, HasAnalogue, HasComponent, HasImage, Image,
     MeasureUnitID, IsForWeb, IsForSale, IsForZeroSale, SourceFenixID, SourceFenixCode,
     SourceAmgID, SourceAmgCode, Updated, Deleted)
VALUES
    (1, NEWID(), 'FENIX', 'FENIX', 'Fenix', 'Fenix', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', 1,
     0, 0, 0, '', 1, 1, 1, 0, 0xAA, 11, NULL, NULL, @Updated, 0),
    (2, NEWID(), 'AMG', 'AMG', 'AMG', 'AMG', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', 1,
     0, 0, 0, '', 1, 1, 1, 0, NULL, NULL, 0xCC, 22, @Updated, 0),
    (3, NEWID(), 'DUAL', 'DUAL', 'Dual', 'Dual', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', '', 1,
     0, 0, 0, '', 1, 1, 1, 0, 0xBA, 31, 0xDA, 32, @Updated, 0);

INSERT dbo.Product
    (ID, NetUID, VendorCode, SearchVendorCode, Name, NameUA, Description, DescriptionUA,
     MainOriginalNumber, Size, SynonymsUA, SearchSynonymsUA, SearchName, SearchNameUA,
     SearchDescription, SearchDescriptionUA, SearchSize, PackingStandard, OrderStandard,
     UCGFEA, Volume, [Top], Weight, HasAnalogue, HasComponent, HasImage, Image,
     MeasureUnitID, IsForWeb, IsForSale, IsForZeroSale, SourceFenixID, SourceFenixCode,
     SourceAmgID, SourceAmgCode, Updated, Deleted)
SELECT 4, NEWID(), 'FENIX-DUP', 'FENIX-DUP', 'Fenix duplicate', 'Fenix duplicate',
       p.Description, p.DescriptionUA, p.MainOriginalNumber, p.Size, p.SynonymsUA,
       p.SearchSynonymsUA, p.SearchName, p.SearchNameUA, p.SearchDescription,
       p.SearchDescriptionUA, p.SearchSize, p.PackingStandard, p.OrderStandard,
       p.UCGFEA, p.Volume, p.[Top], p.Weight, p.HasAnalogue, p.HasComponent,
       p.HasImage, p.Image, p.MeasureUnitID, p.IsForWeb, p.IsForSale, p.IsForZeroSale,
       p.SourceFenixID, p.SourceFenixCode, p.SourceAmgID, p.SourceAmgCode, p.Updated, p.Deleted
FROM dbo.Product p WHERE p.ID = 1
UNION ALL
SELECT 5, NEWID(), 'AMG-DUP', 'AMG-DUP', 'AMG duplicate', 'AMG duplicate',
       p.Description, p.DescriptionUA, p.MainOriginalNumber, p.Size, p.SynonymsUA,
       p.SearchSynonymsUA, p.SearchName, p.SearchNameUA, p.SearchDescription,
       p.SearchDescriptionUA, p.SearchSize, p.PackingStandard, p.OrderStandard,
       p.UCGFEA, p.Volume, p.[Top], p.Weight, p.HasAnalogue, p.HasComponent,
       p.HasImage, p.Image, p.MeasureUnitID, p.IsForWeb, p.IsForSale, p.IsForZeroSale,
       p.SourceFenixID, p.SourceFenixCode, p.SourceAmgID, p.SourceAmgCode, p.Updated, p.Deleted
FROM dbo.Product p WHERE p.ID = 2
UNION ALL
SELECT 6, NEWID(), 'CROSS-WORLD', 'CROSS-WORLD', 'Cross world', 'Cross world',
       p.Description, p.DescriptionUA, p.MainOriginalNumber, p.Size, p.SynonymsUA,
       p.SearchSynonymsUA, p.SearchName, p.SearchNameUA, p.SearchDescription,
       p.SearchDescriptionUA, p.SearchSize, p.PackingStandard, p.OrderStandard,
       p.UCGFEA, p.Volume, p.[Top], p.Weight, p.HasAnalogue, p.HasComponent,
       p.HasImage, p.Image, p.MeasureUnitID, p.IsForWeb, p.IsForSale, p.IsForZeroSale,
       p.SourceFenixID, p.SourceFenixCode, 0xEE, 33, p.Updated, p.Deleted
FROM dbo.Product p WHERE p.ID = 1
UNION ALL
SELECT 7, NEWID(), 'SAME-CODE-DIFFERENT-ID', 'SAME-CODE-DIFFERENT-ID',
       'Same code, different ID', 'Same code, different ID',
       p.Description, p.DescriptionUA, p.MainOriginalNumber, p.Size, p.SynonymsUA,
       p.SearchSynonymsUA, p.SearchName, p.SearchNameUA, p.SearchDescription,
       p.SearchDescriptionUA, p.SearchSize, p.PackingStandard, p.OrderStandard,
       p.UCGFEA, p.Volume, p.[Top], p.Weight, p.HasAnalogue, p.HasComponent,
       p.HasImage, p.Image, p.MeasureUnitID, p.IsForWeb, p.IsForSale, p.IsForZeroSale,
       0xAB, p.SourceFenixCode, NULL, NULL, p.Updated, p.Deleted
FROM dbo.Product p WHERE p.ID = 1
UNION ALL
SELECT 8, NEWID(), 'SAME-ID-DIFFERENT-CODE', 'SAME-ID-DIFFERENT-CODE',
       'Same ID, different code', 'Same ID, different code',
       p.Description, p.DescriptionUA, p.MainOriginalNumber, p.Size, p.SynonymsUA,
       p.SearchSynonymsUA, p.SearchName, p.SearchNameUA, p.SearchDescription,
       p.SearchDescriptionUA, p.SearchSize, p.PackingStandard, p.OrderStandard,
       p.UCGFEA, p.Volume, p.[Top], p.Weight, p.HasAnalogue, p.HasComponent,
       p.HasImage, p.Image, p.MeasureUnitID, p.IsForWeb, p.IsForSale, p.IsForZeroSale,
       p.SourceFenixID, 12, NULL, NULL, p.Updated, p.Deleted
FROM dbo.Product p WHERE p.ID = 1
UNION ALL
SELECT 9, NEWID(), 'CODE-ONLY', 'CODE-ONLY', 'Code only', 'Code only',
       p.Description, p.DescriptionUA, p.MainOriginalNumber, p.Size, p.SynonymsUA,
       p.SearchSynonymsUA, p.SearchName, p.SearchNameUA, p.SearchDescription,
       p.SearchDescriptionUA, p.SearchSize, p.PackingStandard, p.OrderStandard,
       p.UCGFEA, p.Volume, p.[Top], p.Weight, p.HasAnalogue, p.HasComponent,
       p.HasImage, p.Image, p.MeasureUnitID, p.IsForWeb, p.IsForSale, p.IsForZeroSale,
       NULL, p.SourceFenixCode, NULL, NULL, p.Updated, p.Deleted
FROM dbo.Product p WHERE p.ID = 1;

INSERT dbo.ProductAvailability (ID, ProductID, StorageID, Amount, Deleted) VALUES
    (1, 1, 1, 5, 0),
    (2, 2, 3, 7, 0),
    (3, 3, 1, 3, 0),
    (4, 3, 3, 4, 0),
    (5, 3, 4, 2, 0),
    (6, 4, 1, 9, 0),
    (7, 5, 3, 9, 0),
    (8, 6, 3, 6, 0),
    (9, 7, 1, 1, 0),
    (10, 8, 1, 1, 0),
    (11, 9, 1, 1, 0);");
    }
}
