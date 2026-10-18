using System;
using System.IO;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Services.Infrastructure;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class ShopClientAgreementsTests {
    private readonly Guid _actor = Guid.NewGuid();
    private readonly Mock<IClientAgreementRepository> _agreements = new();

    [Fact]
    public void An_explicitly_selected_agreement_wins() {
        ClientAgreement selected = Agreement();
        _agreements.Setup(repository => repository.GetSelectedByClientNetId(_actor)).Returns(selected);
        _agreements.Setup(repository => repository.GetSelectedByClientNotSelectedNetId(_actor)).Returns(Agreement());

        Assert.Same(selected, ShopClientAgreements.Resolve(_agreements.Object, _actor));
    }

    [Fact]
    public void A_workplace_uses_its_selected_agreement_before_any_fallback() {
        ClientAgreement workplaceAgreement = Agreement();
        _agreements.Setup(repository => repository.GetSelectedByWorkplaceNetId(_actor)).Returns(workplaceAgreement);
        _agreements.Setup(repository => repository.GetSelectedByClientNotSelectedNetId(_actor)).Returns(Agreement());

        Assert.Same(workplaceAgreement, ShopClientAgreements.Resolve(_agreements.Object, _actor));
    }

    [Fact]
    public void Without_a_selection_the_fallback_agreement_is_used() {
        ClientAgreement fallback = Agreement();
        _agreements.Setup(repository => repository.GetSelectedByClientNotSelectedNetId(_actor)).Returns(fallback);

        Assert.Same(fallback, ShopClientAgreements.Resolve(_agreements.Object, _actor));
    }

    [Fact]
    public void A_cart_context_without_any_agreement_is_a_bad_request() {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            ShopClientAgreements.ResolveWithWorkplace(_actor, _agreements.Object, Mock.Of<IWorkplaceRepository>()));

        Assert.Equal(ShopClientAgreements.RequiredMessage, exception.Message);
    }

    [Fact]
    public void The_fallback_mirrors_the_consoles_sale_agreement_choice() {
        string source = ReadSource("src/GBA.Domain/Repositories/Clients/ClientAgreementRepository.cs");
        int start = source.IndexOf("private ClientAgreement GetByClientContextNetId(", StringComparison.Ordinal);
        int end = source.IndexOf("public ClientAgreement GetSelectedByWorkplaceNetId(", start, StringComparison.Ordinal);
        string query = source[start..end];

        Assert.Contains("[Agreement].IsDefaultForSyncConsignment = 0", query, StringComparison.Ordinal);
        Assert.Contains(
            "ORDER BY CASE WHEN [Agreement].PricingID IS NULL THEN 1 ELSE 0 END, [Agreement].IsActive DESC, [ClientAgreement].ID",
            query,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Checkout_and_catalog_resolve_the_same_agreement_as_the_cart() {
        string orderService = ReadSource("src/GBA.Services/Services/Orders/OrderService.cs");
        string offerService = ReadSource("src/GBA.Services/Services/Offers/OfferService.cs");
        string productService = ReadSource("src/GBA.Services/Services/Products/ProductService.cs");

        Assert.Equal(2, Count(orderService, "ShopClientAgreements.Resolve("));
        Assert.Contains("GetByIdWithAgreementAndOrganization(clientShoppingCart.ClientAgreementId)", orderService, StringComparison.Ordinal);
        Assert.DoesNotContain("GetActiveByRootClientNetId", orderService, StringComparison.Ordinal);
        Assert.Contains("ClientAgreement = offer.ClientAgreement,", offerService, StringComparison.Ordinal);
        Assert.DoesNotContain("GetSelectedByClientNotSelectedNetId", productService, StringComparison.Ordinal);
    }

    private static ClientAgreement Agreement() {
        return new ClientAgreement {
            NetUid = Guid.NewGuid(),
            Agreement = new Agreement { Organization = new Organization() }
        };
    }

    private static int Count(string source, string value) {
        int count = 0;
        int index = source.IndexOf(value, StringComparison.Ordinal);
        while (index >= 0) {
            count++;
            index = source.IndexOf(value, index + value.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private static string ReadSource(string relativePath) {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null) {
            string candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate repository file: {relativePath}");
    }
}
