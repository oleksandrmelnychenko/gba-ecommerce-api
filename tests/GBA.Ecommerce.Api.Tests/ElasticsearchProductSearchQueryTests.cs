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

    private static ElasticsearchProductSearchService CreateService(
        CaptureSearchHandler handler) {
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

    private sealed class CaptureSearchHandler : HttpMessageHandler {
        public string? RequestJson { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            RequestJson = await request.Content!.ReadAsStringAsync(cancellationToken);

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
