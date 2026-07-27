using System.Net;
using GBA.Search.Elasticsearch;

namespace GBA.Ecommerce.Api.Tests;

public sealed class ElasticsearchSyncSafetyTests {
    [Fact]
    public void PartitionBulkItems_UsesConfiguredBatchSize() {
        int[] items = Enumerable.Range(1, 2501).ToArray();

        int[][] batches = ElasticsearchSyncService
            .PartitionBulkItems(items, 1000)
            .ToArray();

        Assert.Equal([1000, 1000, 501], batches.Select(batch => batch.Length));
        Assert.Equal(items, batches.SelectMany(batch => batch));
    }

    [Fact]
    public void EnsureSuccessfulHttpResponse_ThrowsForEmptyErrorResponse() {
        using HttpResponseMessage response = new(HttpStatusCode.RequestEntityTooLarge);

        HttpRequestException exception = Assert.Throws<HttpRequestException>(() =>
            ElasticsearchSyncService.EnsureSuccessfulHttpResponse(
                response,
                "bulk index",
                string.Empty));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, exception.StatusCode);
        Assert.Contains("HTTP 413", exception.Message);
    }

    [Fact]
    public void ValidateBulkResponse_ThrowsWhenAnyIndexItemFails() {
        const string response = """
            {
              "errors": true,
              "items": [
                { "index": { "status": 201 } },
                {
                  "index": {
                    "status": 400,
                    "error": { "type": "mapper_parsing_exception", "reason": "bad document" }
                  }
                }
              ]
            }
            """;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ElasticsearchSyncService.ValidateBulkResponse(response, "index", 2));

        Assert.Contains("1/2", exception.Message);
        Assert.Contains("mapper_parsing_exception", exception.Message);
    }

    [Fact]
    public void ValidateBulkResponse_AllowsIdempotentMissingDelete() {
        const string response = """
            {
              "errors": true,
              "items": [
                { "delete": { "status": 200 } },
                { "delete": { "status": 404, "result": "not_found" } }
              ]
            }
            """;

        int processed = ElasticsearchSyncService.ValidateBulkResponse(
            response,
            "delete",
            2,
            allowNotFound: true);

        Assert.Equal(2, processed);
    }
}
