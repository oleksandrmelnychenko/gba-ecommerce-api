using System.Data;
using System.Reflection;
using GBA.Domain.Repositories.Products;
using Moq;

namespace GBA.Ecommerce.Unit.Tests;

public sealed class OptimizedProductRepositorySqlContractTests {
    [Fact]
    public void LivePriceQuery_ValidatesAgreementOrganizationVatAndSourceBeforePricing() {
        string sql = InvokeSqlBuilder("BuildLivePriceSql");

        Assert.Contains("clientAgreement.NetUID = @ClientAgreementNetId", sql, StringComparison.Ordinal);
        Assert.Contains("agreement.IsActive = 1", sql, StringComparison.Ordinal);
        Assert.Contains("agreement.OrganizationID = @OrganizationId", sql, StringComparison.Ordinal);
        Assert.Contains("agreement.WithVATAccounting = @WithVat", sql, StringComparison.Ordinal);
        Assert.Contains("organization.PriceSourceIsAmg = 0", sql, StringComparison.Ordinal);
        Assert.Contains("organization.PriceSourceIsAmg = 1", sql, StringComparison.Ordinal);
        Assert.Contains("agreement.SourceFenixID", sql, StringComparison.Ordinal);
        Assert.Contains("agreement.SourceAmgID", sql, StringComparison.Ordinal);
        Assert.Contains("CROSS JOIN AgreementContext", sql, StringComparison.Ordinal);
        Assert.Contains("GetCalculatedProductPriceWithSharesAndVat", sql, StringComparison.Ordinal);
        Assert.Contains("INNER JOIN #ProductIds", sql, StringComparison.Ordinal);
        Assert.Contains("@CatalogSource", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("NOLOCK", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LiveAvailabilityQuery_IsScopedToCurrentEcommerceBranch() {
        string sql = InvokeSqlBuilder("BuildLiveAvailabilitySql");

        Assert.Contains("p.IsForWeb = 1", sql, StringComparison.Ordinal);
        Assert.Contains("s.ForEcommerce = 1", sql, StringComparison.Ordinal);
        Assert.Contains("s.ForDefective = 0", sql, StringComparison.Ordinal);
        Assert.Contains("s.OrganizationID = @OrganizationId", sql, StringComparison.Ordinal);
        Assert.Contains("s.ForVatProducts = @WithVat", sql, StringComparison.Ordinal);
        Assert.Contains("o.PriceSourceIsAmg = 1", sql, StringComparison.Ordinal);
        Assert.Contains("o.PriceSourceIsAmg = 0", sql, StringComparison.Ordinal);
        Assert.Contains("@CatalogSource", sql, StringComparison.Ordinal);
        Assert.Contains("LEFT JOIN Availability", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("pa.Amount > 0", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("NOLOCK", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LiveHydration_RejectsInvalidOrOversizedRequestsBeforeOpeningSqlCommand() {
        OptimizedProductRepository repository = new(Mock.Of<IDbConnection>());
        List<long> oversized = Enumerable.Range(1, 1001).Select(id => (long)id).ToList();

        Assert.Empty(repository.GetPricesOnly(
            [1],
            Guid.NewGuid(),
            organizationId: null,
            withVat: false,
            catalogSource: "fenix"));
        Assert.Empty(repository.GetPricesOnly(
            oversized,
            Guid.NewGuid(),
            organizationId: 1,
            withVat: false,
            catalogSource: "fenix"));
        Assert.Empty(repository.GetCatalogAvailabilityOnly(
            oversized,
            organizationId: 1,
            withVat: false,
            catalogSource: "fenix"));
        Assert.Empty(repository.GetCatalogAvailabilityOnly(
            [1],
            organizationId: 1,
            withVat: false,
            catalogSource: "unknown"));
    }

    private static string InvokeSqlBuilder(string methodName) {
        MethodInfo method = typeof(OptimizedProductRepository).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{methodName} was not found.");

        return (string)(method.Invoke(null, null)
            ?? throw new InvalidOperationException($"{methodName} returned null."));
    }
}
