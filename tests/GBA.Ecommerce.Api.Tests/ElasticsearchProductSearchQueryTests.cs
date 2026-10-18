using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using GBA.Search.Elasticsearch;
using GBA.Search.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GBA.Ecommerce.Api.Tests;

public sealed class ElasticsearchProductSearchQueryTests {
    [Theory]
    [InlineData("амортиза", false)]
    [InlineData("SEM18487", false)]
    [InlineData("D=45", true)]
    [InlineData("15x5", true)]
    public async Task Size_wildcards_are_used_only_for_dimension_queries(
        string query,
        bool expectsSizeWildcard) {
        CaptureSearchHandler handler = new();
        ElasticsearchProductSearchService service = CreateService(handler);

        await service.SearchWithDocsAsync(query);

        Assert.NotNull(handler.RequestJson);
        Assert.Equal(
            expectsSizeWildcard,
            handler.RequestJson.Contains("\"wildcard\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Complex_queries_keep_ngram_clause_generation_bounded() {
        CaptureSearchHandler handler = new();
        ElasticsearchProductSearchService service = CreateService(handler);
        string query = string.Join(
            ' ',
            Enumerable.Range(0, 64).Select(index => $"complexsearchterm{index:D2}suffix"));

        await service.SearchWithDocsAsync(query);

        Assert.NotNull(handler.RequestJson);
        using JsonDocument request = JsonDocument.Parse(handler.RequestJson);
        JsonElement functionScore = request.RootElement
            .GetProperty("query")
            .GetProperty("function_score");
        JsonElement must = functionScore
            .GetProperty("query")
            .GetProperty("bool")
            .GetProperty("must");

        Assert.Equal(16, must.GetArrayLength());

        JsonElement[] ngramMatches = EnumerateNgramMatchOptions(request.RootElement).ToArray();
        Assert.InRange(ngramMatches.Length, 1, 224);
        Assert.All(ngramMatches, options => {
            Assert.Equal("lowercase_analyzer", options.GetProperty("analyzer").GetString());
            Assert.InRange(options.GetProperty("query").GetString()!.Length, 1, 15);
        });
    }

    [Theory]
    [InlineData("2434 03", "243403")]
    [InlineData("29167 30", "2916730")]
    [InlineData("W 950/26", "w95026")]
    [InlineData("464 006 002 0", "4640060020")]
    public async Task Codes_typed_with_separators_also_match_as_one_compact_code(
        string query,
        string compactCode) {
        CaptureSearchHandler handler = new();
        ElasticsearchProductSearchService service = CreateService(handler);

        await service.SearchWithDocsAsync(query);

        using JsonDocument request = JsonDocument.Parse(handler.RequestJson!);
        string[] ngramQueries = EnumerateNgramMatchOptions(request.RootElement)
            .Select(options => options.GetProperty("query").GetString()!)
            .ToArray();
        Assert.Contains(compactCode, ngramQueries);
        Assert.All(ngramQueries, value => Assert.True(value.Length >= 3, value));
        Assert.Contains(
            ExactMatchQueries(request.RootElement, "vendorCodeClean"),
            value => value == compactCode);
        Assert.Contains(
            ExactMatchQueries(request.RootElement, "mainOriginalNumberClean"),
            value => value == compactCode);
    }

    [Fact]
    public async Task Short_terms_do_not_make_a_longer_query_unmatchable() {
        CaptureSearchHandler handler = new();
        ElasticsearchProductSearchService service = CreateService(handler);

        await service.SearchWithDocsAsync("болт M8");

        using JsonDocument request = JsonDocument.Parse(handler.RequestJson!);
        string[] ngramQueries = EnumerateNgramMatchOptions(request.RootElement)
            .Select(options => options.GetProperty("query").GetString()!)
            .ToArray();
        Assert.Contains("болт", ngramQueries);
        Assert.DoesNotContain("m8", ngramQueries);
        Assert.DoesNotContain("\"prefix\"", handler.RequestJson!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_query_of_only_short_terms_falls_back_to_code_and_word_prefixes() {
        CaptureSearchHandler handler = new();
        ElasticsearchProductSearchService service = CreateService(handler);

        await service.SearchWithDocsAsync("M8");

        Assert.Contains("\"prefix\":{\"vendorCodeClean\":\"m8\"}", handler.RequestJson!, StringComparison.Ordinal);
        Assert.Contains("\"prefix\":{\"nameUA\":\"m8\"}", handler.RequestJson!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Short_dimension_terms_keep_their_size_match() {
        CaptureSearchHandler handler = new();
        ElasticsearchProductSearchService service = CreateService(handler);

        await service.SearchWithDocsAsync("шайба d=4");

        Assert.Contains("*d=4*", handler.RequestJson!, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ISS1004C")]
    [InlineData("ISS1004С")]
    public async Task Cyrillic_and_latin_lookalikes_in_codes_produce_the_same_query(string query) {
        CaptureSearchHandler handler = new();
        ElasticsearchProductSearchService service = CreateService(handler);

        await service.SearchWithDocsAsync(query);

        using JsonDocument request = JsonDocument.Parse(handler.RequestJson!);
        Assert.Contains(
            ExactMatchQueries(request.RootElement, "vendorCodeClean"),
            value => value == "iss1004c");
    }

    [Fact]
    public async Task Zero_hits_retries_with_a_fuzzy_name_query() {
        SequencedSearchHandler handler = new(0, 1);
        ElasticsearchProductSearchService service = CreateService(handler);

        var result = await service.SearchWithDocsAsync("амартизатор");

        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("\"fuzziness\":\"AUTO\"", handler.Requests[1], StringComparison.Ordinal);
        Assert.True(result.IsFallback);
        Assert.Single(result.Documents);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("SEM")]
    public async Task Fuzzy_fallback_is_not_used_for_codes_or_very_short_words(string query) {
        SequencedSearchHandler handler = new(0, 1);
        ElasticsearchProductSearchService service = CreateService(handler);

        var result = await service.SearchWithDocsAsync(query);

        Assert.Single(handler.Requests);
        Assert.False(result.IsFallback);
    }

    [Fact]
    public async Task Fuzzy_fallback_is_skipped_when_the_exact_query_found_products() {
        SequencedSearchHandler handler = new(1);
        ElasticsearchProductSearchService service = CreateService(handler);

        var result = await service.SearchWithDocsAsync("амортизатор");

        Assert.Single(handler.Requests);
        Assert.False(result.IsFallback);
    }

    private static IEnumerable<string> ExactMatchQueries(JsonElement element, string field) {
        if (element.ValueKind == JsonValueKind.Object) {
            if (element.TryGetProperty("match", out JsonElement match)
                && match.TryGetProperty(field, out JsonElement options)
                && options.ValueKind == JsonValueKind.Object
                && options.TryGetProperty("query", out JsonElement query)) {
                yield return query.GetString()!;
            }

            foreach (JsonProperty property in element.EnumerateObject()) {
                foreach (string value in ExactMatchQueries(property.Value, field)) {
                    yield return value;
                }
            }
        } else if (element.ValueKind == JsonValueKind.Array) {
            foreach (JsonElement item in element.EnumerateArray()) {
                foreach (string value in ExactMatchQueries(item, field)) {
                    yield return value;
                }
            }
        }
    }

    private static ElasticsearchProductSearchService CreateService(
        HttpMessageHandler handler) {
        HttpClient client = new(handler) {
            BaseAddress = new Uri("http://elasticsearch/")
        };

        return new ElasticsearchProductSearchService(
            client,
            Options.Create(new ElasticsearchSettings { IndexName = "products" }),
            new SearchTextProcessor(),
            NullLogger<ElasticsearchProductSearchService>.Instance);
    }

    private static IEnumerable<JsonElement> EnumerateNgramMatchOptions(JsonElement element) {
        if (element.ValueKind == JsonValueKind.Object) {
            if (element.TryGetProperty("match", out JsonElement match)) {
                foreach (JsonProperty field in match.EnumerateObject()) {
                    if (field.Name.EndsWith(".ngram", StringComparison.Ordinal)) {
                        yield return field.Value;
                    }
                }
            }

            foreach (JsonProperty property in element.EnumerateObject()) {
                foreach (JsonElement descendant in EnumerateNgramMatchOptions(property.Value)) {
                    yield return descendant;
                }
            }
        } else if (element.ValueKind == JsonValueKind.Array) {
            foreach (JsonElement item in element.EnumerateArray()) {
                foreach (JsonElement descendant in EnumerateNgramMatchOptions(item)) {
                    yield return descendant;
                }
            }
        }
    }

    private sealed class SequencedSearchHandler(params int[] totals) : HttpMessageHandler {
        public List<string> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            Requests.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            int total = totals[Math.Min(Requests.Count, totals.Length) - 1];
            string hits = total == 0
                ? "[]"
                : "[{\"_source\":{\"id\":38301714,\"vendorCode\":\"SEM18257\",\"isForWeb\":true}}]";

            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(
                    $"{{\"took\":1,\"hits\":{{\"total\":{{\"value\":{total}}},\"hits\":{hits}}}}}",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }

    private sealed class CaptureSearchHandler : HttpMessageHandler {
        public string? RequestJson { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            RequestJson ??= await request.Content!.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(
                    """
                    {
                      "took": 1,
                      "hits": {
                        "total": { "value": 0 },
                        "hits": []
                      }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
