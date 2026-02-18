using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GBA.Domain.EntityHelpers;
using GBA.Search.Models;
using GBA.Services.Services.Products;
using Microsoft.Extensions.Logging;

namespace GBA.Search.Services;

public sealed class SqlFallbackSearchService(
    Func<IDbConnection> connectionFactory,
    ILogger<SqlFallbackSearchService> logger) : IProductSearchService {
    public async Task<ProductSearchResult> SearchAsync(
        string query,
        string locale = "uk",
        int limit = 20,
        int offset = 0,
        CancellationToken cancellationToken = default) {

        if (string.IsNullOrWhiteSpace(query)) {
            return ProductSearchResult.Empty;
        }

        Stopwatch sw = Stopwatch.StartNew();

        try {
            List<SearchResult>? result = await Task.Run(() => {
                using IDbConnection connection = connectionFactory();
                connection.Open();

                ProductSearchServiceOptimized searchService = new GBA.Services.Services.Products.ProductSearchServiceOptimized();
                List<SearchResult>? searchResults = searchService.GetSearchResults(connection, query, limit, offset);

                return searchResults;
            }, cancellationToken);

            sw.Stop();

            List<long> productIds = new System.Collections.Generic.List<long>(result.Count);
            foreach (SearchResult r in result) {
                productIds.Add(r.Id);
            }

            logger.LogWarning(
                "SQL fallback search executed for query '{Query}' in {ElapsedMs}ms, found {Count} results",
                query, sw.ElapsedMilliseconds, productIds.Count);

            return new ProductSearchResult {
                ProductIds = productIds,
                TotalCount = productIds.Count,
                SearchTimeMs = (int)sw.ElapsedMilliseconds,
                IsFallback = true
            };
        } catch (Exception ex) {
            logger.LogError(ex, "SQL fallback search failed for query: {Query}", query);
            throw;
        }
    }

    public Task<ProductSearchResultWithDocs> SearchWithDocsAsync(
        string query,
        string locale = "uk",
        int limit = 20,
        int offset = 0,
        CancellationToken cancellationToken = default) {
        // SQL fallback doesn't support returning full documents
        // This should only be called when Typesense is down
        logger.LogWarning("SearchWithDocs called on SQL fallback - returning empty");
        return Task.FromResult(ProductSearchResultWithDocs.Empty);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default) {
        return Task.FromResult(true);
    }
}
