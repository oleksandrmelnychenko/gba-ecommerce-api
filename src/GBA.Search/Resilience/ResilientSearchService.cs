using System;
using System.Threading;
using System.Threading.Tasks;
using GBA.Search.Configuration;
using GBA.Search.Models;
using GBA.Search.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;

namespace GBA.Search.Resilience;

public sealed class ResilientSearchService : IProductSearchService, IProductSearchDebugService {
    private readonly IProductSearchService _typesenseService;
    private readonly IProductSearchService _fallbackService;
    private readonly TypesenseSearchService _typesenseDebugService;
    private readonly ResilienceSettings _settings;
    private readonly ILogger<ResilientSearchService> _logger;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

    public ResilientSearchService(
        TypesenseSearchService typesenseService,
        SqlFallbackSearchService fallbackService,
        IOptions<ResilienceSettings> settings,
        ILogger<ResilientSearchService> logger) {
        _typesenseService = typesenseService;
        _fallbackService = fallbackService;
        _typesenseDebugService = typesenseService;
        _settings = settings.Value;
        _logger = logger;

        _circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: _settings.FailureThreshold,
                durationOfBreak: TimeSpan.FromSeconds(_settings.CircuitBreakDurationSeconds),
                onBreak: (ex, duration) => {
                    _logger.LogWarning(ex,
                        "Search circuit breaker opened for {Duration}s due to failures",
                        duration.TotalSeconds);
                },
                onReset: () => {
                    _logger.LogInformation("Search circuit breaker reset - Typesense is available again");
                },
                onHalfOpen: () => {
                    _logger.LogInformation("Search circuit breaker half-open - testing Typesense availability");
                });
    }

    public async Task<ProductSearchResult> SearchAsync(
        string query,
        string locale = "uk",
        int limit = 20,
        int offset = 0,
        CancellationToken cancellationToken = default) {

        if (string.IsNullOrWhiteSpace(query)) {
            return ProductSearchResult.Empty;
        }

        if (_circuitBreaker.CircuitState == CircuitState.Open) {
            _logger.LogDebug("Circuit is open, using SQL fallback directly");
            return await ExecuteFallbackAsync(query, locale, limit, offset, cancellationToken);
        }

        try {
            ProductSearchResult? result = await _circuitBreaker.ExecuteAsync(async ct => {
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

                return await _typesenseService.SearchAsync(query, locale, limit, offset, cts.Token);
            }, cancellationToken);

            return result;
        } catch (BrokenCircuitException) {
            _logger.LogDebug("Circuit breaker prevented Typesense call, using fallback");
            return await ExecuteFallbackAsync(query, locale, limit, offset, cancellationToken);
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            _logger.LogWarning("Typesense search timed out for query: {Query}", query);
            return await ExecuteFallbackAsync(query, locale, limit, offset, cancellationToken);
        } catch (Exception ex) {
            _logger.LogError(ex, "Typesense search failed for query: {Query}, falling back to SQL", query);

            if (_settings.EnableFallback) {
                return await ExecuteFallbackAsync(query, locale, limit, offset, cancellationToken);
            }

            throw;
        }
    }

    public async Task<ProductSearchResultWithDocs> SearchWithDocsAsync(
        string query,
        string locale = "uk",
        int limit = 20,
        int offset = 0,
        CancellationToken cancellationToken = default) {

        if (string.IsNullOrWhiteSpace(query)) {
            return ProductSearchResultWithDocs.Empty;
        }

        if (_circuitBreaker.CircuitState == CircuitState.Open) {
            _logger.LogWarning("SearchWithDocs unavailable - circuit breaker is open, returning empty");
            return ProductSearchResultWithDocs.Empty;
        }

        try {
            ProductSearchResultWithDocs result = await _circuitBreaker.ExecuteAsync(async ct => {
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

                return await _typesenseDebugService.SearchWithDocsAsync(query, locale, limit, offset, cts.Token);
            }, cancellationToken);

            return result;
        } catch (Exception ex) {
            _logger.LogError(ex, "Typesense SearchWithDocs failed for query: {Query}", query);
            return ProductSearchResultWithDocs.Empty;
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default) {
        if (_circuitBreaker.CircuitState == CircuitState.Open) {
            return false;
        }

        return await _typesenseService.IsHealthyAsync(cancellationToken);
    }

    public Task<ProductSearchDebugResult> SearchDebugAsync(
        string query,
        string locale = "uk",
        int limit = 20,
        int offset = 0,
        CancellationToken cancellationToken = default) {

        if (_circuitBreaker.CircuitState == CircuitState.Open) {
            _logger.LogWarning("Debug search unavailable - circuit breaker is open");
            return Task.FromResult(new ProductSearchDebugResult {
                OriginalQuery = query ?? "",
                Error = "Search service unavailable - circuit breaker is open"
            });
        }

        return _typesenseDebugService.SearchDebugAsync(query, locale, limit, offset, cancellationToken);
    }

    private async Task<ProductSearchResult> ExecuteFallbackAsync(
        string query,
        string locale,
        int limit,
        int offset,
        CancellationToken cancellationToken) {

        if (!_settings.EnableFallback) {
            _logger.LogWarning("Fallback is disabled, returning empty result");
            return ProductSearchResult.Empty;
        }

        return await _fallbackService.SearchAsync(query, locale, limit, offset, cancellationToken);
    }

    public CircuitState GetCircuitState() => _circuitBreaker.CircuitState;
}
