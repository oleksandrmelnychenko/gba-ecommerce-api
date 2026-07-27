using System.Net;
using System.Net.Http;
using System.Text;
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
