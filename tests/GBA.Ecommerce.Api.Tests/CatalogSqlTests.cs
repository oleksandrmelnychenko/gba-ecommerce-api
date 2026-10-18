using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Agreements;
using GBA.Domain.Repositories.Clients;
using GBA.Domain.Repositories.Currencies;
using GBA.Domain.Repositories.ExchangeRates;
using GBA.Domain.Repositories.Organizations;
using GBA.Domain.Repositories.Pricings;
using GBA.Domain.Repositories.Products;
using GBA.Domain.Repositories.Storages;
using GBA.Ecommerce.Controllers;
using GBA.Services.Services.Products;
using Microsoft.Data.SqlClient;

namespace GBA.Ecommerce.Api.Tests;

[Collection(CheckoutSqlCollection.Name)]
public sealed class CatalogSqlTests(CheckoutSqlFixture fixture) {
    [Fact]
    public async Task The_anonymous_brand_page_shows_the_retail_price_and_retail_stock() {
        if (!fixture.IsConfigured || !fixture.HasConcordSchema) return;

        UseUkrainianCulture();
        PriceTokenTestSupport.EnsureKeys();
        ProductService service = NewProductService();
        (Product retail, double retailStock) = FindRetailProduct(service);

        List<FromSearchProduct> rows = await service.GetAllByVendorCodes(new List<string> { retail.VendorCode }, 20, 0);
        List<long> ids = rows.Select(row => row.Id).ToList();
        List<ProtectedSearchProduct> cards = ProductsController.ProtectCatalogCards(
            rows,
            service.GetPricesOnly(ids, Guid.Empty, false),
            service.GetSellableQuantities(ids, Guid.Empty),
            service.GetPricingContext(Guid.Empty, false).CurrencyCode,
            isAuthenticated: false,
            timestamp: 1_700_000_000L);

        ProtectedSearchProduct card = Assert.Single(cards, candidate => candidate.Id == retail.Id);
        decimal[] prices = PriceTokenTestSupport.DecodePrices(card.P);
        Assert.True(prices[1] > 0m);
        Assert.Equal(decimal.Round(retail.CurrentLocalPrice, 2), decimal.Round(prices[1], 2));
        Assert.Equal(retailStock, card.AvailableQtyUk);
        Assert.Equal(retailStock, card.AvailableQtyUkVAT);
    }

    [Fact]
    public void The_price_cache_version_follows_price_inputs_but_not_timestamp_touches() {
        if (!fixture.IsConfigured || !fixture.HasConcordSchema) return;

        using SqlConnection connection = fixture.Open();
        (long ClientAgreementId, Guid ClientNetUid) client = connection.QueryFirst<(long, Guid)>("""
            SELECT TOP (1) ca.ID, c.NetUID
            FROM dbo.ProductGroupDiscount d
            JOIN dbo.ClientAgreement ca ON ca.ID = d.ClientAgreementID AND ca.Deleted = 0
            JOIN dbo.Client c ON c.ID = ca.ClientID AND c.Deleted = 0
            WHERE d.Deleted = 0
            ORDER BY d.ID;
            """);
        PricingRepository repository = new(connection);
        (long RateId, decimal Amount) rate = connection.QueryFirst<(long, decimal)>(
            "SELECT TOP (1) ID, Amount FROM dbo.ExchangeRate WHERE Deleted = 0 ORDER BY ID;");
        (long DiscountId, double Rate) discount = connection.QueryFirst<(long, double)>(
            "SELECT TOP (1) ID, DiscountRate FROM dbo.ProductGroupDiscount WHERE ClientAgreementID = @ClientAgreementId AND Deleted = 0 ORDER BY ID;",
            new { client.ClientAgreementId });

        string before = repository.GetPriceInputsVersion(client.ClientAgreementId);
        try {
            connection.Execute("UPDATE dbo.ExchangeRate SET Updated = GETUTCDATE() WHERE ID = @RateId;", new { rate.RateId });
            connection.Execute("UPDATE dbo.ProductGroupDiscount SET Updated = GETUTCDATE() WHERE ID = @DiscountId;", new { discount.DiscountId });
            Assert.Equal(before, repository.GetPriceInputsVersion(client.ClientAgreementId));

            connection.Execute("UPDATE dbo.ExchangeRate SET Amount = Amount + 0.0100 WHERE ID = @RateId;", new { rate.RateId });
            string afterRate = repository.GetPriceInputsVersion(client.ClientAgreementId);
            Assert.NotEqual(before, afterRate);

            connection.Execute("UPDATE dbo.ProductGroupDiscount SET DiscountRate = DiscountRate + 1 WHERE ID = @DiscountId;", new { discount.DiscountId });
            Assert.NotEqual(afterRate, repository.GetPriceInputsVersion(client.ClientAgreementId));
        } finally {
            connection.Execute("UPDATE dbo.ExchangeRate SET Amount = @Amount WHERE ID = @RateId;", new { rate.Amount, rate.RateId });
            connection.Execute("UPDATE dbo.ProductGroupDiscount SET DiscountRate = @Rate WHERE ID = @DiscountId;", new { discount.Rate, discount.DiscountId });
        }

        Assert.Equal(before, repository.GetPriceInputsVersion(client.ClientAgreementId));
    }

    private (Product Product, double RetailStock) FindRetailProduct(ProductService service) {
        using SqlConnection connection = fixture.Open();
        Storage storage = new StorageRepository(connection).GetWithHighestPriority();
        foreach ((long id, Guid netUid, double amount) in connection.Query<(long, Guid, double)>("""
                     SELECT TOP (25) p.ID, p.NetUID, CAST(pa.Amount AS float)
                     FROM dbo.ProductAvailability pa
                     JOIN dbo.Product p ON p.ID = pa.ProductID AND p.Deleted = 0 AND p.IsForWeb = 1
                     WHERE pa.StorageID = @StorageId AND pa.Deleted = 0 AND pa.Amount >= 5
                       AND (SELECT COUNT(*) FROM dbo.Product same WHERE same.VendorCode = p.VendorCode AND same.Deleted = 0 AND same.IsForWeb = 1) = 1
                     ORDER BY pa.Amount DESC, p.ID;
                     """, new { StorageId = storage.Id })) {
            Product product = service.GetByNetIdForRetail(netUid).GetAwaiter().GetResult();
            if (product?.CurrentLocalPrice > 0m) return (product, amount);
        }

        throw new InvalidOperationException("No priced retail product was found in the test database.");
    }

    private static void UseUkrainianCulture() {
        CultureInfo culture = new("uk-UA");
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private ProductService NewProductService() {
        return new ProductService(
            new ClientRepositoriesFactory(),
            new ProductRepositoriesFactory(),
            new PricingRepositoriesFactory(),
            new ExchangeRateRepositoriesFactory(),
            new CurrencyRepositoriesFactory(),
            new OrganizationRepositoriesFactory(),
            new StorageRepositoryFactory(),
            fixture.ConnectionFactory,
            new AgreementRepositoriesFactory());
    }
}
