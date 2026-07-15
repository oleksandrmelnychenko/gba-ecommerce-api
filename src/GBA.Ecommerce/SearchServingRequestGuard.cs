using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GBA.Common.ResponseBuilder;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Search.Elasticsearch;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GBA.Ecommerce;

internal static class SearchServingRequestGuard {
    public static async Task<IActionResult> ExecuteAsync(
        ISearchServingGenerationResolver resolver,
        Func<SearchActiveGeneration, Task<IActionResult>> execute,
        CancellationToken ct) {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(execute);

        SearchServingGenerationResolution resolution = await resolver.ResolveAsync(ct);
        if (!resolution.IsAvailable) {
            return ServiceUnavailable(resolution);
        }

        try {
            return await execute(resolution.Generation!);
        } catch (SearchServingUnavailableException exception) {
            return ServiceUnavailable(exception.Resolution);
        }
    }

    private static ObjectResult ServiceUnavailable(SearchServingGenerationResolution resolution) {
        IWebResponse response = new ErrorResponse {
            Message = SearchServingUnavailableException.DefaultMessage,
            StatusCode = HttpStatusCode.ServiceUnavailable,
            Body = new {
                syncStateReadable = resolution.SyncStateReadable,
                hasActiveGeneration = resolution.HasActiveGeneration,
                schemaCurrent = resolution.SchemaCurrent,
                hasWatermark = resolution.HasWatermark,
                lastSyncUtc = resolution.LastSyncUtc,
                lagSeconds = resolution.LagSeconds,
                stale = resolution.Stale,
                incrementalCatchUpRequired = resolution.IncrementalCatchUpRequired,
                reasons = resolution.Reasons
            }
        };
        return new ObjectResult(response) {
            StatusCode = StatusCodes.Status503ServiceUnavailable
        };
    }
}
