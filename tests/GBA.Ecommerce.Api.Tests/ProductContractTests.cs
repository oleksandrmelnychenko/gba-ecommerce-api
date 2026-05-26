using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace GBA.Ecommerce.Api.Tests;

public sealed class ProductContractTests(EcommerceApiFixture fixture) : IClassFixture<EcommerceApiFixture> {
    private readonly HttpClient _client = fixture.Client;
    private readonly ApiTestConfig _config = fixture.Config;

    [Fact]
    public async Task Product_search_caps_limit_and_applies_offset() {
        JsonElement[] cappedProducts = await GetSearchProductsAsync(_config.SearchQuery, 150, 0);
        Assert.True(cappedProducts.Length <= 100);

        JsonElement[] firstPage = await GetSearchProductsAsync(_config.SearchQuery, 2, 0);
        JsonElement[] offsetPage = await GetSearchProductsAsync(_config.SearchQuery, 2, 1);

        Assert.NotEmpty(firstPage);
        if (firstPage.Length < 2 || offsetPage.Length == 0) return;

        Assert.Equal(
            ApiAssertions.RequiredInt32(firstPage[1], "Id"),
            ApiAssertions.RequiredInt32(offsetPage[0], "Id"));
    }

    [Fact]
    public async Task Product_search_empty_value_returns_empty_body_array() {
        JsonElement[] products = await GetSearchProductsAsync(string.Empty, 10, 0);

        Assert.Empty(products);
    }

    [Fact]
    public async Task Product_search_negative_offset_clamps_safely_when_supported() {
        string url = SearchUrl(_config.SearchQuery, 1, -10);
        using HttpResponseMessage response = await _client.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.BadRequest) return;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);
        Assert.Equal(JsonValueKind.Array, envelope.Body.ValueKind);

        JsonElement[] negativeOffsetProducts = envelope.Body.EnumerateArray().ToArray();
        JsonElement[] firstPage = await GetSearchProductsAsync(_config.SearchQuery, 1, 0);

        Assert.NotEmpty(negativeOffsetProducts);
        Assert.NotEmpty(firstPage);
        Assert.Equal(
            ApiAssertions.RequiredInt32(firstPage[0], "Id"),
            ApiAssertions.RequiredInt32(negativeOffsetProducts[0], "Id"));
    }

    [Fact]
    public async Task Product_get_by_net_id_returns_product_contract_from_search_result() {
        JsonElement searchProduct = await GetFirstSearchProductAsync();
        Guid netUid = ApiAssertions.RequiredGuid(searchProduct, "NetUid");

        using HttpResponseMessage response = await _client.GetAsync(
            _config.ApiPath($"products/get?netId={netUid}&withVat=0"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);
        AssertProductContract(envelope.Body, netUid);
    }

    [Fact]
    public async Task Product_get_by_slug_returns_product_contract_when_search_result_has_slug() {
        JsonElement[] products = await GetSearchProductsAsync(_config.SearchQuery, 10, 0);
        JsonElement? productWithSlug = products
            .Cast<JsonElement?>()
            .FirstOrDefault(product => TryGetProductSlugUrl(product!.Value, out _));

        if (productWithSlug is null) return;

        JsonElement searchProduct = productWithSlug.Value;
        Guid netUid = ApiAssertions.RequiredGuid(searchProduct, "NetUid");
        Assert.True(TryGetProductSlugUrl(searchProduct, out string? slug));

        using HttpResponseMessage response = await _client.GetAsync(
            _config.ApiPath($"products/get/slug?slug={Uri.EscapeDataString(slug!)}&withVat=0"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);
        AssertProductContract(envelope.Body, netUid);
    }

    [Fact]
    public async Task Products_vendorcodes_all_returns_matching_vendor_code() {
        JsonElement searchProduct = await GetFirstSearchProductAsync();
        string vendorCode = ApiAssertions.RequiredString(searchProduct, "VendorCode");

        using HttpResponseMessage response = await _client.GetAsync(
            _config.ApiPath($"products/vendorcodes/all?vendorCodes={Uri.EscapeDataString(vendorCode)}&limit=10&offset=0&withVat=0"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);
        Assert.Equal(JsonValueKind.Array, envelope.Body.ValueKind);

        JsonElement[] products = envelope.Body.EnumerateArray().ToArray();
        Assert.Contains(products, product =>
            product.TryGetProperty("VendorCode", out JsonElement value)
            && value.ValueKind == JsonValueKind.String
            && string.Equals(value.GetString(), vendorCode, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("products/get/analogues")]
    [InlineData("products/get/components")]
    public async Task Product_related_product_endpoints_return_success_envelope_array(string relativePath) {
        JsonElement searchProduct = await GetFirstSearchProductAsync();
        Guid netUid = ApiAssertions.RequiredGuid(searchProduct, "NetUid");

        using HttpResponseMessage response = await _client.GetAsync(
            _config.ApiPath($"{relativePath}?netId={netUid}&withVat=0"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);
        Assert.Equal(JsonValueKind.Array, envelope.Body.ValueKind);
    }

    private async Task<JsonElement> GetFirstSearchProductAsync() {
        JsonElement[] products = await GetSearchProductsAsync(_config.SearchQuery, 10, 0);
        Assert.NotEmpty(products);

        return products[0];
    }

    private async Task<JsonElement[]> GetSearchProductsAsync(string value, long limit, long offset) {
        using HttpResponseMessage response = await _client.GetAsync(SearchUrl(value, limit, offset));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ApiEnvelope envelope = await ApiAssertions.ReadEnvelopeAsync(response);
        ApiAssertions.AssertSuccessEnvelope(envelope);
        Assert.Equal(JsonValueKind.Array, envelope.Body.ValueKind);

        return envelope.Body.EnumerateArray().ToArray();
    }

    private string SearchUrl(string value, long limit, long offset) {
        return _config.ApiPath(
            $"products/search?value={Uri.EscapeDataString(value)}&limit={limit}&offset={offset}&withVat=0");
    }

    private static void AssertProductContract(JsonElement product, Guid? expectedNetUid = null) {
        Assert.Equal(JsonValueKind.Object, product.ValueKind);
        Assert.True(ApiAssertions.RequiredInt32(product, "Id") > 0);

        Guid netUid = ApiAssertions.RequiredGuid(product, "NetUid");
        if (expectedNetUid.HasValue) Assert.Equal(expectedNetUid.Value, netUid);

        ApiAssertions.RequiredString(product, "VendorCode");
        ApiAssertions.RequiredString(product, "Name");

        AssertNumberProperty(product, "AvailableQtyUk");
        AssertNumberProperty(product, "AvailableQtyPl");

        if (product.TryGetProperty("ProductSlug", out JsonElement slug) && slug.ValueKind == JsonValueKind.Object) {
            ApiAssertions.RequiredString(slug, "Url");
            ApiAssertions.RequiredString(slug, "Locale");
        }
    }

    private static bool TryGetProductSlugUrl(JsonElement product, out string? slug) {
        slug = null;

        if (!product.TryGetProperty("ProductSlug", out JsonElement productSlug)
            || productSlug.ValueKind != JsonValueKind.Object
            || !productSlug.TryGetProperty("Url", out JsonElement url)
            || url.ValueKind != JsonValueKind.String) {
            return false;
        }

        slug = url.GetString();
        return !string.IsNullOrWhiteSpace(slug);
    }

    private static void AssertNumberProperty(JsonElement element, string propertyName) {
        Assert.True(element.TryGetProperty(propertyName, out JsonElement property), $"Missing '{propertyName}' property.");
        Assert.Equal(JsonValueKind.Number, property.ValueKind);
    }
}
