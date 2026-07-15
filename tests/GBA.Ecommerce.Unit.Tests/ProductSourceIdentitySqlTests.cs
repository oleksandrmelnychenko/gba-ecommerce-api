using GBA.Domain.EntityHelpers;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class ProductSourceIdentitySqlTests {
    [Fact]
    public void ExternalSourceIdentity_RetainsBothIdAndCodeInTheExactKey() {
        Assert.True(ExternalSourceIdentity.TryCreate(
            [0x01],
            11,
            null,
            null,
            out ExternalSourceIdentity? first));
        Assert.True(ExternalSourceIdentity.TryCreate(
            [0x02],
            11,
            null,
            null,
            out ExternalSourceIdentity? sameCodeDifferentId));
        Assert.True(ExternalSourceIdentity.TryCreate(
            [0x01],
            12,
            null,
            null,
            out ExternalSourceIdentity? sameIdDifferentCode));

        Assert.Equal("fenix:id-01|code-11", first?.Value);
        Assert.False(first?.EqualsExact(sameCodeDifferentId));
        Assert.False(first?.EqualsExact(sameIdDifferentCode));
    }

    [Fact]
    public void ExternalSourceIdentity_RejectsAmbiguousDualSourceRows() {
        Assert.False(ExternalSourceIdentity.TryCreate(
            [0x01],
            11,
            [0x02],
            22,
            out ExternalSourceIdentity? identity));
        Assert.Null(identity);
    }

    [Theory]
    [InlineData("fenix", "fenix")]
    [InlineData(" FENIX ", "fenix")]
    [InlineData("amg", "amg")]
    [InlineData(" AMG ", "amg")]
    public void TryNormalizeSourceWorld_AcceptsOnlySupportedWorlds(
        string input,
        string expected) {
        Assert.True(ProductSourceIdentitySql.TryNormalizeSourceWorld(input, out string actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("agreement:id-42")]
    [InlineData("fenix:id-42")]
    [InlineData("other")]
    public void TryNormalizeSourceWorld_RejectsExactEntityIdentities(string input) {
        Assert.False(ProductSourceIdentitySql.TryNormalizeSourceWorld(input, out string actual));
        Assert.Equal(string.Empty, actual);
    }

    [Fact]
    public void CanonicalSourceWorldPredicate_UsesRequestedParameterThroughout() {
        string sql = ProductSourceIdentitySql.CanonicalSourceWorldPredicate(
            "product",
            "@AgreementSourceWorld");

        Assert.Contains("@AgreementSourceWorld = 'fenix'", sql, StringComparison.Ordinal);
        Assert.Contains("@AgreementSourceWorld = 'amg'", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("@CatalogSource", sql, StringComparison.Ordinal);
        Assert.Contains("canonicalProduct.ID < product.ID", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void SameSourceEntityPredicate_UsesTheWholeExternalSourceKey() {
        string sql = ProductSourceIdentitySql.SameSourceEntityPredicate(
            "leftProduct",
            "rightProduct",
            "fenix");

        Assert.Contains(
            "rightProduct.SourceFenixID = leftProduct.SourceFenixID",
            sql,
            StringComparison.Ordinal);
        Assert.Contains(
            "rightProduct.SourceFenixCode = leftProduct.SourceFenixCode",
            sql,
            StringComparison.Ordinal);
        Assert.Contains(
            "DATALENGTH(leftProduct.SourceFenixID)",
            sql,
            StringComparison.Ordinal);
        Assert.Contains(
            "DATALENGTH(rightProduct.SourceFenixID)",
            sql,
            StringComparison.Ordinal);
    }
}
