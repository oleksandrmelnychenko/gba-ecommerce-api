using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GBA.Domain.EntityHelpers;
using GBA.Search.Models;
using GBA.Search.Services;
using GBA.Search.Sync;
using GBA.Search.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GBA.Search.Elasticsearch;

public interface IElasticsearchProductSearchService : IProductSearchService {
    Task<ElasticsearchDebugResult> SearchDebugAsync(string query, string locale = "uk", int limit = 20, int offset = 0, CancellationToken ct = default);
}

public sealed class ElasticsearchProductSearchService : IElasticsearchProductSearchService {
    public const int MaxQueryLength = 256;
    public const int MaxResultWindow = 10_000;
    public const int MaxPageSize = 1_000;
    private const int DefaultPageSize = 20;

    private readonly HttpClient _http;
    private readonly ElasticsearchSettings _settings;
    private readonly ISearchServingGenerationResolver _servingGenerationResolver;
    private readonly IElasticsearchIndexService _indexService;
    private readonly SearchTextProcessor _textProcessor;
    private readonly ILogger<ElasticsearchProductSearchService> _log;

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly Regex SpecialCharsRegex = new(@"[+\-=&|><!(){}\[\]^""~*?:\\/]", RegexOptions.Compiled);

    private static readonly string[] SourceFields = {
        "id", "netUid", "vendorCode", "vendorCodeClean", "name", "nameUA",
        "description", "descriptionUA", "searchName", "searchNameUA",
        "searchDescription", "searchDescriptionUA", "mainOriginalNumber",
        "mainOriginalNumberClean", "originalNumbers", "originalNumbersClean",
        "size", "sizeClean", "packingStandard", "orderStandard", "ucgfea",
        "volume", "top", "weight", "hasAnalogue", "hasComponent", "hasImage",
        "image", "measureUnitId", "available", "availableQtyUk",
        "availableQtyUkVat", "availableQtyPl", "availableQtyPlVat",
        "availableQty", "isForWeb", "isForSale", "isForZeroSale", "slugId",
        "slugNetUid", "slugUrl", "slugLocale", "retailPrice", "retailPriceVat",
        "retailCurrencyCode", "retailCurrencyCodeVat", "indexedProductPricingRevision",
        "indexedPricingHierarchyRevision", "indexedDiscountRevision",
        "indexedExchangeRateRevision", "catalogOrganizationIdNonVat",
        "catalogOrganizationIdVat", "catalogAgreementSourceNonVat", "catalogAgreementSourceVat",
        "productSourceFenix", "productSourceAmg", "isCanonicalFenix",
        "isCanonicalAmg", "catalogScopes",
        "catalogSourceSystemNonVat", "catalogSourceSystemVat",
        "catalogAgreementNetUidNonVat", "catalogAgreementNetUidVat",
        "catalogPricingIdNonVat", "catalogPricingIdVat", "catalogCurrencyIdNonVat",
        "catalogCurrencyIdVat", "hasNonVatCatalogAvailability",
        "hasVatCatalogAvailability", "hasNonVatCatalogSource",
        "hasVatCatalogSource", "updatedAt"
    };

    public ElasticsearchProductSearchService(
        HttpClient httpClient,
        IOptions<ElasticsearchSettings> settings,
        ISearchServingGenerationResolver servingGenerationResolver,
        IElasticsearchIndexService indexService,
        SearchTextProcessor textProcessor,
        ILogger<ElasticsearchProductSearchService> logger) {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = (settings ?? throw new ArgumentNullException(nameof(settings))).Value;
        SearchSyncStorage.ValidateBaseIndexName(_settings.IndexName);
        _servingGenerationResolver = servingGenerationResolver
            ?? throw new ArgumentNullException(nameof(servingGenerationResolver));
        _indexService = indexService ?? throw new ArgumentNullException(nameof(indexService));
        _textProcessor = textProcessor ?? throw new ArgumentNullException(nameof(textProcessor));
        _log = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<ProductSearchResult> SearchAsync(
        string query, string locale = "uk", int limit = 20, int offset = 0, CancellationToken ct = default) {

        // A context-free product query cannot prove catalog ownership.
        return Task.FromResult(ProductSearchResult.Empty);
    }

    public async Task<ProductSearchResult> SearchAsync(
        string query,
        ProductSearchCatalogContext catalogContext,
        string locale = "uk",
        int limit = 20,
        int offset = 0,
        CancellationToken ct = default) {

        if (!IsSafeQuery(query) || catalogContext?.IsValid != true)
            return ProductSearchResult.Empty;

        (limit, offset) = NormalizeWindow(limit, offset);

        object esQuery = BuildSearchQuery(query, locale, limit, offset, catalogContext);
        SearchResult result = await ExecuteSearchAsync(esQuery, ct, catalogContext);

        return new ProductSearchResult {
            ProductIds = result.Ids,
            TotalCount = result.Total,
            SearchTimeMs = result.TookMs,
            IsFallback = false
        };
    }

    public Task<ProductSearchResultWithDocs> SearchWithDocsAsync(
        string query, string locale = "uk", int limit = 20, int offset = 0, CancellationToken ct = default) {

        return Task.FromResult(ProductSearchResultWithDocs.Empty);
    }

    public async Task<ProductSearchResultWithDocs> SearchWithDocsAsync(
        string query,
        ProductSearchCatalogContext catalogContext,
        string locale = "uk",
        int limit = 20,
        int offset = 0,
        CancellationToken ct = default) {

        if (!IsSafeQuery(query) || catalogContext?.IsValid != true)
            return ProductSearchResultWithDocs.Empty;

        (limit, offset) = NormalizeWindow(limit, offset);

        object esQuery = BuildSearchQuery(query, locale, limit, offset, catalogContext);
        SearchResultWithDocs result = await ExecuteSearchWithDocsAsync(esQuery, catalogContext, ct);

        return new ProductSearchResultWithDocs {
            Documents = result.Documents,
            TotalCount = result.Total,
            SearchTimeMs = result.TookMs,
            IsFallback = false
        };
    }

    public Task<bool> IsHealthyAsync(CancellationToken ct = default) =>
        _indexService.IsHealthyAsync(ct);

    public async Task<ElasticsearchDebugResult> SearchDebugAsync(
        string query, string locale = "uk", int limit = 20, int offset = 0, CancellationToken ct = default) {

        ElasticsearchDebugResult debug = new ElasticsearchDebugResult {
            OriginalQuery = query,
            Locale = locale
        };

        if (!IsSafeQuery(query)) {
            return debug;
        }

        (limit, offset) = NormalizeWindow(limit, offset);

        string normalized = NumberNormalizer.NormalizeQuery(query);
        string[] terms = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        debug.NormalizedQuery = normalized;
        debug.Terms = terms.ToList();

        object esQuery = BuildSearchQuery(query, locale, limit, offset);
        debug.ElasticsearchQuery = JsonSerializer.Serialize(esQuery, new JsonSerializerOptions { WriteIndented = true });

        SearchResult result = await ExecuteSearchAsync(esQuery, ct);

        debug.TotalFound = result.Total;
        debug.SearchTimeMs = result.TookMs;
        debug.ProductIds = result.Ids;

        return debug;
    }

    private static bool IsSafeQuery(string? query) {
        return !string.IsNullOrWhiteSpace(query) && query.Length <= MaxQueryLength;
    }

    private static int NormalizeLimit(int limit) {
        return limit <= 0 ? DefaultPageSize : Math.Min(limit, MaxPageSize);
    }

    private static (int Limit, int Offset) NormalizeWindow(int limit, int offset) {
        int boundedOffset = Math.Clamp(offset, 0, MaxResultWindow - 1);
        int boundedLimit = Math.Min(NormalizeLimit(limit), MaxResultWindow - boundedOffset);
        return (boundedLimit, boundedOffset);
    }

    private object BuildSearchQuery(
        string query,
        string locale,
        int limit,
        int offset,
        ProductSearchCatalogContext? catalogContext = null) {
        string normalized = NumberNormalizer.NormalizeQuery(query);
        string normalizedLower = normalized.ToLowerInvariant();
        string[] terms = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => EscapeElasticsearchQuery(t.ToLowerInvariant()))
            .ToArray();
        // Keep original terms for size field (preserves "=" and other special chars)
        string[] originalTerms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.ToLowerInvariant())
            .ToArray();

        if (terms.Length == 0)
            return BuildMatchAllQuery(limit, offset, catalogContext);

        // Build must clauses - each term must match somewhere (AND logic between terms)
        List<object> mustClauses = new List<object>();
        for (int i = 0; i < terms.Length; i++) {
            string originalTerm = i < originalTerms.Length ? originalTerms[i] : terms[i];
            mustClauses.Add(BuildTermMatchQuery(terms[i], locale, originalTerm));
        }

        // Build scoring functions to match SQL V1 ranking:
        // SQL V1 ORDER BY:
        // 1. MainOriginalNumberExact DESC
        // 2. Available DESC
        // 3. HundredPercentMatch DESC (exact on SearchNameUA/VendorCode/MainOriginalNumber)
        // 4. OriginalNumber_Match DESC
        // 5. SearchVendorCode_Match DESC
        // 6. (OriginalNumber OR SearchName OR SearchNameUA) DESC
        // 7. (SearchDescription OR SearchDescriptionUA) DESC
        // 8. SearchSize_Match DESC
        List<object> functions = new List<object>();

        // Analyze terms to determine scoring strategy
        bool hasCyrillicTerms = terms.Any(t => t.Any(c =>
            (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я') ||
            c == 'і' || c == 'ї' || c == 'є' || c == 'ґ'));

        // Detect dimension-like queries (d=45, 15x5, M14x1.5)
        bool isDimensionQuery = originalTerms.Any(t =>
            t.Contains('=') || t.Contains('x') || t.Contains('X') ||
            (t.Length <= 6 && t.Any(char.IsDigit) && !t.All(char.IsDigit)));

        // 1. Exact match on MainOriginalNumber
        // Lower weight for dimension queries - we don't want "d=45" to boost products with "D45" as their main number
        int mainOriginalNumberWeight = isDimensionQuery ? 5000 : 100000;
        functions.Add(new {
            filter = new { term = new Dictionary<string, object> { ["mainOriginalNumberClean"] = normalizedLower } },
            weight = mainOriginalNumberWeight
        });

        // 2. Available (products in stock)
        functions.Add(new {
            filter = new { term = new { available = true } },
            weight = 50000
        });

        // 3. Exact match on SearchNameUA (for Cyrillic queries)
        if (hasCyrillicTerms) {
            functions.Add(new {
                filter = new { term = new Dictionary<string, object> { ["searchNameUA"] = normalizedLower.Replace(" ", "") } },
                weight = 40000
            });
        }

        // 3b. Exact match on VendorCode
        functions.Add(new {
            filter = new { term = new Dictionary<string, object> { ["vendorCodeClean"] = normalizedLower } },
            weight = 40000
        });

        (string primarySearchName, string secondarySearchName) = locale == "uk"
            ? ("searchNameUA", "searchName")
            : ("searchName", "searchNameUA");
        (string primaryDesc, string secondaryDesc) = locale == "uk"
            ? ("searchDescriptionUA", "searchDescription")
            : ("searchDescription", "searchDescriptionUA");

        // Per-term scoring with different weights based on term type
        for (int i = 0; i < terms.Length; i++) {
            string term = terms[i];
            string originalTerm = i < originalTerms.Length ? originalTerms[i] : term;
            string termLower = term.ToLowerInvariant();
            bool isCyrillic = term.Any(c =>
                (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я') ||
                c == 'і' || c == 'ї' || c == 'є' || c == 'ґ' ||
                c == 'І' || c == 'Ї' || c == 'Є' || c == 'Ґ');
            bool hasDigits = term.Any(char.IsDigit);

            if (isCyrillic) {
                functions.Add(new {
                    filter = new { match = new Dictionary<string, object> { [$"{primarySearchName}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                    weight = 3000
                });
                functions.Add(new {
                    filter = new { match = new Dictionary<string, object> { [$"{secondarySearchName}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                    weight = 2500
                });
                functions.Add(new {
                    filter = new { match = new Dictionary<string, object> { [$"{primaryDesc}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                    weight = 500
                });
                functions.Add(new {
                    filter = new { match = new Dictionary<string, object> { [$"{secondaryDesc}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                    weight = 400
                });
            } else {
                functions.Add(new {
                    filter = new { match = new Dictionary<string, object> { ["vendorCodeClean.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                    weight = 5000
                });
                functions.Add(new {
                    filter = new {
                        @bool = new {
                            should = new object[] {
                                new { match = new Dictionary<string, object> { ["mainOriginalNumberClean.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                                new { match = new Dictionary<string, object> { ["originalNumbersClean.ngram"] = new { query = termLower, minimum_should_match = "80%" } } }
                            }
                        }
                    },
                    weight = 3000
                });
                functions.Add(new {
                    filter = new { match = new Dictionary<string, object> { [$"{primarySearchName}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                    weight = 1500
                });
                functions.Add(new {
                    filter = new { match = new Dictionary<string, object> { [$"{secondarySearchName}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                    weight = 1200
                });
                functions.Add(new {
                    filter = new { match = new Dictionary<string, object> { [$"{primaryDesc}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                    weight = 300
                });
                functions.Add(new {
                    filter = new { match = new Dictionary<string, object> { [$"{secondaryDesc}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                    weight = 200
                });
            }

            // 8. Size match (for all terms)
            // Higher weight for dimension-like terms (e.g., "d=45", "15x5", "M14x1.5")
            bool isDimensionTerm = term.Contains('=') || term.Contains('x') ||
                (hasDigits && term.Length <= 10 && !isCyrillic);
            // Very high weight for size when it looks like a dimension query - must beat originalNumbers (3000)
            int sizeWeight = isDimensionTerm ? 8000 : 100;

            // Search original size field using originalTerm (preserves "=", "x" etc.)
            string originalTermLowerForSize = originalTerm.ToLowerInvariant();
            functions.Add(new {
                filter = new { wildcard = new Dictionary<string, object> { ["size"] = new { value = $"*{originalTermLowerForSize}*", case_insensitive = true } } },
                weight = sizeWeight
            });
            // Also search sizeClean with cleaned term
            string termCleanForSize = Regex.Replace(termLower, @"[^a-z0-9а-яіїєґ]", "");
            if (!string.IsNullOrEmpty(termCleanForSize)) {
                functions.Add(new {
                    filter = new { wildcard = new Dictionary<string, object> { ["sizeClean"] = new { value = $"*{termCleanForSize}*" } } },
                    weight = sizeWeight
                });
            }
        }

        Dictionary<string, object> request = new() {
            ["from"] = offset,
            ["size"] = limit,
            ["track_total_hits"] = true,
            ["_source"] = SourceFields,
            ["query"] = new {
                function_score = new {
                    query = new {
                        @bool = new {
                            must = mustClauses,
                            filter = BuildCatalogFilters(catalogContext)
                        }
                    },
                    functions = functions,
                    score_mode = "sum",
                    boost_mode = "replace"
                }
            },
            ["sort"] = new object[] {
                "_score",
                new Dictionary<string, object> { ["nameUA.keyword"] = new { order = "asc" } },
                new { id = new { order = "asc" } }
            }
        };
        AddCanonicalSourceCollapse(request, catalogContext);
        return request;
    }

    private static object BuildTermMatchQuery(string term, string locale, string? originalTerm = null) {
        // Each term can match in ANY of these fields (OR logic within term)
        // Using wildcard for PATINDEX-like behavior (substring anywhere in field)
        List<object> shouldClauses = new List<object>();

        // Lowercase for case-insensitive matching
        string termLower = term.ToLowerInvariant();
        string originalTermLower = (originalTerm ?? term).ToLowerInvariant();

        shouldClauses.Add(new { match = new Dictionary<string, object> { ["vendorCodeClean.ngram"] = new { query = termLower, minimum_should_match = "80%" } } });

        shouldClauses.Add(new { match = new Dictionary<string, object> { ["mainOriginalNumberClean.ngram"] = new { query = termLower, minimum_should_match = "80%" } } });
        shouldClauses.Add(new { match = new Dictionary<string, object> { ["originalNumbersClean.ngram"] = new { query = termLower, minimum_should_match = "80%" } } });

        (string primaryName, string secondaryName) = locale == "uk"
            ? ("searchNameUA", "searchName")
            : ("searchName", "searchNameUA");
        shouldClauses.Add(new { match = new Dictionary<string, object> { [$"{primaryName}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } });
        shouldClauses.Add(new { match = new Dictionary<string, object> { [$"{secondaryName}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } });

        (string primaryDesc, string secondaryDesc) = locale == "uk"
            ? ("searchDescriptionUA", "searchDescription")
            : ("searchDescription", "searchDescriptionUA");
        shouldClauses.Add(new { match = new Dictionary<string, object> { [$"{primaryDesc}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } });
        shouldClauses.Add(new { match = new Dictionary<string, object> { [$"{secondaryDesc}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } });

        // Size - search both original (with special chars like "=") and clean version
        // For dimension queries like "d=45", the original size field "D=45 h=104" is more relevant
        // Use originalTermLower to preserve "=" and other special chars in size search
        shouldClauses.Add(new { wildcard = new Dictionary<string, object> { ["size"] = new { value = $"*{originalTermLower}*", case_insensitive = true } } });
        // Also search sizeClean for normalized matches (without special chars)
        string termClean = Regex.Replace(termLower, @"[^a-z0-9а-яіїєґ]", "");
        if (!string.IsNullOrEmpty(termClean)) {
            shouldClauses.Add(new { wildcard = new Dictionary<string, object> { ["sizeClean"] = new { value = $"*{termClean}*" } } });
        }

        return new {
            @bool = new {
                should = shouldClauses,
                minimum_should_match = 1
            }
        };
    }

    private static object BuildMatchAllQuery(
        int limit,
        int offset,
        ProductSearchCatalogContext? catalogContext) {
        Dictionary<string, object> request = new() {
            ["from"] = offset,
            ["size"] = limit,
            ["track_total_hits"] = true,
            ["_source"] = SourceFields,
            ["query"] = new {
                @bool = new {
                    filter = BuildCatalogFilters(catalogContext)
                }
            },
            ["sort"] = new object[] {
                new { available = new { order = "desc" } },
                new { availableQtyUk = new { order = "desc" } },
                new { id = new { order = "asc" } }
            }
        };
        AddCanonicalSourceCollapse(request, catalogContext);
        return request;
    }

    private static void AddCanonicalSourceCollapse(
        IDictionary<string, object> request,
        ProductSearchCatalogContext? catalogContext) {
        if (catalogContext == null) return;

        request["collapse"] = new {
            field = catalogContext.Source == ProductSourceIdentitySql.Amg
                ? "productSourceAmg"
                : "productSourceFenix"
        };
    }

    private static object[] BuildCatalogFilters(ProductSearchCatalogContext? catalogContext) {
        List<object> filters = [new { term = new { isForWeb = true } }];
        if (catalogContext == null) return filters.ToArray();

        string sourceSystem = catalogContext.Source;
        string productSourceField = sourceSystem == ProductSourceIdentitySql.Amg
            ? "productSourceAmg"
            : "productSourceFenix";
        string canonicalField = sourceSystem == ProductSourceIdentitySql.Amg
            ? "isCanonicalAmg"
            : "isCanonicalFenix";
        filters.Add(new {
            prefix = new Dictionary<string, object> {
                [productSourceField] = $"{sourceSystem}:"
            }
        });
        filters.Add(new {
            term = new Dictionary<string, object> {
                [canonicalField] = true
            }
        });

        if (!catalogContext.UseIndexedRetailPrice) {
            filters.Add(BuildCatalogScopeFilter(catalogContext, sourceSystem));
            return filters.ToArray();
        }

        string variant = catalogContext.WithVat ? "Vat" : "NonVat";
        string sourceSystemField = $"catalogSourceSystem{variant}";
        string sourceEligibilityField = $"has{variant}CatalogSource";
        string availabilityField = $"has{variant}CatalogAvailability";
        string organizationField = $"catalogOrganizationId{variant}";

        filters.Add(new {
            term = new Dictionary<string, object> {
                [organizationField] = catalogContext.OrganizationId
            }
        });
        filters.Add(new {
            term = new Dictionary<string, object> {
                [sourceSystemField] = sourceSystem
            }
        });
        filters.Add(new {
            term = new Dictionary<string, object> {
                [sourceEligibilityField] = true
            }
        });
        filters.Add(new {
            term = new Dictionary<string, object> {
                [availabilityField] = true
            }
        });

        string agreementField = $"catalogAgreementNetUid{variant}.keyword";
        string pricingField = $"catalogPricingId{variant}";
        string currencyField = $"catalogCurrencyId{variant}";
        string priceField = catalogContext.WithVat ? "retailPriceVat" : "retailPrice";

        filters.Add(new {
            term = new Dictionary<string, object> {
                [agreementField] = catalogContext.ClientAgreementNetId.ToString()
            }
        });
        filters.Add(new {
            term = new Dictionary<string, object> {
                [pricingField] = catalogContext.PricingId
            }
        });
        filters.Add(new {
            term = new Dictionary<string, object> {
                [currencyField] = catalogContext.CurrencyId
            }
        });
        filters.Add(new {
            range = new Dictionary<string, object> {
                [priceField] = new { gt = 0 }
            }
        });
        filters.Add(new {
            term = new Dictionary<string, object> {
                ["indexedProductPricingRevision"] = catalogContext.PricingRevisions!.ProductPricing
            }
        });
        filters.Add(new {
            term = new Dictionary<string, object> {
                ["indexedPricingHierarchyRevision"] = catalogContext.PricingRevisions!.PricingHierarchy
            }
        });
        filters.Add(new {
            term = new Dictionary<string, object> {
                ["indexedDiscountRevision"] = catalogContext.PricingRevisions!.Discounts
            }
        });
        filters.Add(new {
            term = new Dictionary<string, object> {
                ["indexedExchangeRateRevision"] = catalogContext.PricingRevisions!.ExchangeRates
            }
        });

        return filters.ToArray();
    }

    private static object BuildCatalogScopeFilter(
        ProductSearchCatalogContext catalogContext,
        string sourceSystem) {
        return new {
            nested = new {
                path = "catalogScopes",
                score_mode = "none",
                query = new {
                    @bool = new {
                        filter = new object[] {
                            new {
                                term = new Dictionary<string, object> {
                                    ["catalogScopes.organizationId"] = catalogContext.OrganizationId
                                }
                            },
                            new {
                                term = new Dictionary<string, object> {
                                    ["catalogScopes.sourceSystem"] = sourceSystem
                                }
                            },
                            new {
                                term = new Dictionary<string, object> {
                                    ["catalogScopes.withVat"] = catalogContext.WithVat
                                }
                            },
                            new {
                                range = new Dictionary<string, object> {
                                    ["catalogScopes.availableQty"] = new { gt = 0 }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    private async Task<SearchResult> ExecuteSearchAsync(
        object query,
        CancellationToken ct,
        ProductSearchCatalogContext? catalogContext = null) {
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(query, JsonOptions);
        ByteArrayContent content = new ByteArrayContent(json);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        string? activeIndex = await ResolveActiveIndexAsync(catalogContext, ct);
        if (activeIndex == null) return new SearchResult([], 0, 0);

        HttpResponseMessage response = await _http.PostAsync($"{activeIndex}/_search", content, ct);

        if (!response.IsSuccessStatusCode) {
            string error = await response.Content.ReadAsStringAsync(ct);
            _log.LogError("Elasticsearch search failed: {Error}", error);
            return new SearchResult([], 0, 0);
        }

        await using Stream responseStream = await response.Content.ReadAsStreamAsync(ct);
        using JsonDocument doc = await JsonDocument.ParseAsync(responseStream, cancellationToken: ct);
        JsonElement root = doc.RootElement;

        int took = root.GetProperty("took").GetInt32();
        JsonElement hitsRoot = root.GetProperty("hits");
        if (!TryReadExactTotal(hitsRoot, out int total)) {
            _log.LogError("Elasticsearch returned a non-exact or invalid total; failing search closed");
            return new SearchResult([], 0, took);
        }
        JsonElement hits = hitsRoot.GetProperty("hits");

        List<long> ids = new List<long>();
        foreach (JsonElement hit in hits.EnumerateArray()) {
            if (!hit.TryGetProperty("_source", out JsonElement source)) {
                if (catalogContext != null) return new SearchResult([], 0, took);
                continue;
            }

            if (catalogContext != null) {
                ProductSearchDocument document = ParseDocument(source);
                if (!ApplyCatalogContext(document, catalogContext)) {
                    _log.LogWarning(
                        "Elasticsearch returned product {ProductId} outside requested catalog context; failing search closed",
                        document.Id);
                    return new SearchResult([], 0, took);
                }
                ids.Add(document.Id);
            } else if (source.TryGetProperty("id", out JsonElement idProp)) {
                ids.Add(idProp.GetInt64());
            }
        }

        return new SearchResult(ids, total, took);
    }

    private async Task<SearchResultWithDocs> ExecuteSearchWithDocsAsync(
        object query,
        ProductSearchCatalogContext catalogContext,
        CancellationToken ct) {
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(query, JsonOptions);
        ByteArrayContent content = new ByteArrayContent(json);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        string? activeIndex = await ResolveActiveIndexAsync(catalogContext, ct);
        if (activeIndex == null) return new SearchResultWithDocs([], 0, 0);

        HttpResponseMessage response = await _http.PostAsync($"{activeIndex}/_search", content, ct);

        if (!response.IsSuccessStatusCode) {
            string error = await response.Content.ReadAsStringAsync(ct);
            _log.LogError("Elasticsearch search failed: {Error}", error);
            return new SearchResultWithDocs([], 0, 0);
        }

        await using Stream responseStream = await response.Content.ReadAsStreamAsync(ct);
        using JsonDocument doc = await JsonDocument.ParseAsync(responseStream, cancellationToken: ct);
        JsonElement root = doc.RootElement;

        int took = root.GetProperty("took").GetInt32();
        JsonElement hitsRoot = root.GetProperty("hits");
        if (!TryReadExactTotal(hitsRoot, out int total)) {
            _log.LogError("Elasticsearch returned a non-exact or invalid total; failing search closed");
            return new SearchResultWithDocs([], 0, took);
        }
        JsonElement hits = hitsRoot.GetProperty("hits");

        List<ProductSearchDocument> documents = new List<ProductSearchDocument>();
        foreach (JsonElement hit in hits.EnumerateArray()) {
            if (!hit.TryGetProperty("_source", out JsonElement source)) {
                return new SearchResultWithDocs([], 0, took);
            }

            ProductSearchDocument document = ParseDocument(source);
            if (!ApplyCatalogContext(document, catalogContext)) {
                _log.LogWarning(
                    "Elasticsearch returned product {ProductId} outside requested catalog context; failing search closed",
                    document.Id);
                return new SearchResultWithDocs([], 0, took);
            }
            documents.Add(document);
        }

        return new SearchResultWithDocs(documents, total, took);
    }

    private async Task<string?> ResolveActiveIndexAsync(
        ProductSearchCatalogContext? catalogContext,
        CancellationToken ct) {
        SearchActiveGeneration generation =
            await _servingGenerationResolver.GetRequiredGenerationAsync(ct);

        if (catalogContext?.UseIndexedRetailPrice == true
            && !generation.HasExactIndexedPricingRevisions(catalogContext.PricingRevisions)) {
            _log.LogWarning(
                "Active search generation pricing revision does not match the request; failing indexed-price search closed");
            return null;
        }

        return generation.IndexName;
    }

    private static ProductSearchDocument ParseDocument(JsonElement source) {
        return new ProductSearchDocument {
            Id = source.TryGetProperty("id", out JsonElement id) ? id.GetInt64() : 0,
            NetUid = source.TryGetProperty("netUid", out JsonElement netUid) ? netUid.GetString() ?? "" : "",
            VendorCode = source.TryGetProperty("vendorCode", out JsonElement vc) ? vc.GetString() ?? "" : "",
            VendorCodeClean = source.TryGetProperty("vendorCodeClean", out JsonElement vcc) ? vcc.GetString() ?? "" : "",
            Name = source.TryGetProperty("name", out JsonElement name) ? name.GetString() ?? "" : "",
            NameUA = source.TryGetProperty("nameUA", out JsonElement nameUA) ? nameUA.GetString() ?? "" : "",
            Description = source.TryGetProperty("description", out JsonElement desc) ? desc.GetString() ?? "" : "",
            DescriptionUA = source.TryGetProperty("descriptionUA", out JsonElement descUA) ? descUA.GetString() ?? "" : "",
            SearchName = source.TryGetProperty("searchName", out JsonElement sn) ? sn.GetString() ?? "" : "",
            SearchNameUA = source.TryGetProperty("searchNameUA", out JsonElement snUA) ? snUA.GetString() ?? "" : "",
            SearchDescription = source.TryGetProperty("searchDescription", out JsonElement sd) ? sd.GetString() ?? "" : "",
            SearchDescriptionUA = source.TryGetProperty("searchDescriptionUA", out JsonElement sdUA) ? sdUA.GetString() ?? "" : "",
            MainOriginalNumber = source.TryGetProperty("mainOriginalNumber", out JsonElement mon) ? mon.GetString() ?? "" : "",
            MainOriginalNumberClean = source.TryGetProperty("mainOriginalNumberClean", out JsonElement monc) ? monc.GetString() ?? "" : "",
            OriginalNumbers = ParseStringArray(source, "originalNumbers"),
            OriginalNumbersClean = ParseStringArray(source, "originalNumbersClean"),
            Size = source.TryGetProperty("size", out JsonElement size) ? size.GetString() ?? "" : "",
            SizeClean = source.TryGetProperty("sizeClean", out JsonElement sc) ? sc.GetString() ?? "" : "",
            PackingStandard = source.TryGetProperty("packingStandard", out JsonElement ps) ? ps.GetString() ?? "" : "",
            OrderStandard = source.TryGetProperty("orderStandard", out JsonElement os) ? os.GetString() ?? "" : "",
            Ucgfea = source.TryGetProperty("ucgfea", out JsonElement ucg) ? ucg.GetString() ?? "" : "",
            Volume = source.TryGetProperty("volume", out JsonElement vol) ? vol.GetString() ?? "" : "",
            Top = source.TryGetProperty("top", out JsonElement top) ? top.GetString() ?? "" : "",
            Weight = source.TryGetProperty("weight", out JsonElement weight) ? weight.GetDouble() : 0,
            HasAnalogue = source.TryGetProperty("hasAnalogue", out JsonElement ha) && ha.GetBoolean(),
            HasComponent = source.TryGetProperty("hasComponent", out JsonElement hc) && hc.GetBoolean(),
            HasImage = source.TryGetProperty("hasImage", out JsonElement hi) && hi.GetBoolean(),
            Image = source.TryGetProperty("image", out JsonElement img) ? img.GetString() ?? "" : "",
            MeasureUnitId = source.TryGetProperty("measureUnitId", out JsonElement mui) ? mui.GetInt64() : 0,
            Available = source.TryGetProperty("available", out JsonElement avail) && avail.GetBoolean(),
            AvailableQtyUk = source.TryGetProperty("availableQtyUk", out JsonElement aqUk) ? aqUk.GetDouble() : 0,
            AvailableQtyUkVat = source.TryGetProperty("availableQtyUkVat", out JsonElement aqUkVat) ? aqUkVat.GetDouble() : 0,
            AvailableQtyPl = source.TryGetProperty("availableQtyPl", out JsonElement aqPl) ? aqPl.GetDouble() : 0,
            AvailableQtyPlVat = source.TryGetProperty("availableQtyPlVat", out JsonElement aqPlVat) ? aqPlVat.GetDouble() : 0,
            AvailableQty = source.TryGetProperty("availableQty", out JsonElement aq) ? aq.GetDouble() : 0,
            IsForWeb = source.TryGetProperty("isForWeb", out JsonElement ifw) && ifw.GetBoolean(),
            IsForSale = source.TryGetProperty("isForSale", out JsonElement ifs) && ifs.GetBoolean(),
            IsForZeroSale = source.TryGetProperty("isForZeroSale", out JsonElement ifzs) && ifzs.GetBoolean(),
            SlugId = source.TryGetProperty("slugId", out JsonElement slugId) ? slugId.GetInt64() : 0,
            SlugNetUid = source.TryGetProperty("slugNetUid", out JsonElement slugNetUid) ? slugNetUid.GetString() ?? "" : "",
            SlugUrl = source.TryGetProperty("slugUrl", out JsonElement slugUrl) ? slugUrl.GetString() ?? "" : "",
            SlugLocale = source.TryGetProperty("slugLocale", out JsonElement slugLocale) ? slugLocale.GetString() ?? "" : "",
            RetailPrice = source.TryGetProperty("retailPrice", out JsonElement rp) ? rp.GetDecimal() : 0,
            RetailPriceVat = source.TryGetProperty("retailPriceVat", out JsonElement rpv) ? rpv.GetDecimal() : 0,
            RetailCurrencyCode = source.TryGetProperty("retailCurrencyCode", out JsonElement rcc) ? rcc.GetString() ?? "UAH" : "UAH",
            RetailCurrencyCodeVat = source.TryGetProperty("retailCurrencyCodeVat", out JsonElement rccVat) ? rccVat.GetString() ?? "UAH" : "UAH",
            IndexedProductPricingRevision = source.TryGetProperty("indexedProductPricingRevision", out JsonElement indexedProductPricingRevision)
                ? indexedProductPricingRevision.GetString() ?? string.Empty
                : string.Empty,
            IndexedPricingHierarchyRevision = source.TryGetProperty("indexedPricingHierarchyRevision", out JsonElement indexedPricingHierarchyRevision)
                ? indexedPricingHierarchyRevision.GetString() ?? string.Empty
                : string.Empty,
            IndexedDiscountRevision = source.TryGetProperty("indexedDiscountRevision", out JsonElement indexedDiscountRevision)
                ? indexedDiscountRevision.GetString() ?? string.Empty
                : string.Empty,
            IndexedExchangeRateRevision = source.TryGetProperty("indexedExchangeRateRevision", out JsonElement indexedExchangeRateRevision)
                ? indexedExchangeRateRevision.GetString() ?? string.Empty
                : string.Empty,
            CatalogOrganizationIdNonVat = source.TryGetProperty("catalogOrganizationIdNonVat", out JsonElement catalogOrganizationIdNonVat)
                ? catalogOrganizationIdNonVat.GetInt64()
                : 0,
            CatalogOrganizationIdVat = source.TryGetProperty("catalogOrganizationIdVat", out JsonElement catalogOrganizationIdVat)
                ? catalogOrganizationIdVat.GetInt64()
                : 0,
            CatalogAgreementSourceNonVat = source.TryGetProperty("catalogAgreementSourceNonVat", out JsonElement catalogAgreementSourceNonVat)
                ? catalogAgreementSourceNonVat.GetString() ?? ""
                : "",
            CatalogAgreementSourceVat = source.TryGetProperty("catalogAgreementSourceVat", out JsonElement catalogAgreementSourceVat)
                ? catalogAgreementSourceVat.GetString() ?? ""
                : "",
            ProductSourceFenix = source.TryGetProperty("productSourceFenix", out JsonElement productSourceFenix)
                ? productSourceFenix.GetString() ?? ""
                : "",
            ProductSourceAmg = source.TryGetProperty("productSourceAmg", out JsonElement productSourceAmg)
                ? productSourceAmg.GetString() ?? ""
                : "",
            IsCanonicalFenix = source.TryGetProperty("isCanonicalFenix", out JsonElement isCanonicalFenix)
                && isCanonicalFenix.GetBoolean(),
            IsCanonicalAmg = source.TryGetProperty("isCanonicalAmg", out JsonElement isCanonicalAmg)
                && isCanonicalAmg.GetBoolean(),
            CatalogScopes = ParseCatalogScopes(source),
            CatalogSourceSystemNonVat = source.TryGetProperty("catalogSourceSystemNonVat", out JsonElement catalogSourceSystemNonVat)
                ? catalogSourceSystemNonVat.GetString() ?? ""
                : "",
            CatalogSourceSystemVat = source.TryGetProperty("catalogSourceSystemVat", out JsonElement catalogSourceSystemVat)
                ? catalogSourceSystemVat.GetString() ?? ""
                : "",
            CatalogAgreementNetUidNonVat = source.TryGetProperty("catalogAgreementNetUidNonVat", out JsonElement catalogAgreementNetUidNonVat)
                ? catalogAgreementNetUidNonVat.GetString() ?? ""
                : "",
            CatalogAgreementNetUidVat = source.TryGetProperty("catalogAgreementNetUidVat", out JsonElement catalogAgreementNetUidVat)
                ? catalogAgreementNetUidVat.GetString() ?? ""
                : "",
            CatalogPricingIdNonVat = source.TryGetProperty("catalogPricingIdNonVat", out JsonElement catalogPricingIdNonVat)
                ? catalogPricingIdNonVat.GetInt64()
                : 0,
            CatalogPricingIdVat = source.TryGetProperty("catalogPricingIdVat", out JsonElement catalogPricingIdVat)
                ? catalogPricingIdVat.GetInt64()
                : 0,
            CatalogCurrencyIdNonVat = source.TryGetProperty("catalogCurrencyIdNonVat", out JsonElement catalogCurrencyIdNonVat)
                ? catalogCurrencyIdNonVat.GetInt64()
                : 0,
            CatalogCurrencyIdVat = source.TryGetProperty("catalogCurrencyIdVat", out JsonElement catalogCurrencyIdVat)
                ? catalogCurrencyIdVat.GetInt64()
                : 0,
            HasNonVatCatalogAvailability = source.TryGetProperty("hasNonVatCatalogAvailability", out JsonElement hasNonVatCatalogAvailability)
                                               && hasNonVatCatalogAvailability.GetBoolean(),
            HasVatCatalogAvailability = source.TryGetProperty("hasVatCatalogAvailability", out JsonElement hasVatCatalogAvailability)
                                            && hasVatCatalogAvailability.GetBoolean(),
            HasNonVatCatalogSource = source.TryGetProperty("hasNonVatCatalogSource", out JsonElement hasNonVatCatalogSource)
                                           && hasNonVatCatalogSource.GetBoolean(),
            HasVatCatalogSource = source.TryGetProperty("hasVatCatalogSource", out JsonElement hasVatCatalogSource)
                                        && hasVatCatalogSource.GetBoolean(),
            UpdatedAt = source.TryGetProperty("updatedAt", out JsonElement updatedAt) && updatedAt.TryGetDateTime(out DateTime dt)
                ? new DateTimeOffset(dt).ToUnixTimeSeconds()
                : 0
        };
    }

    private static bool ApplyCatalogContext(
        ProductSearchDocument document,
        ProductSearchCatalogContext catalogContext) {
        if (document.Id <= 0) return false;

        string requestedSourceSystem = catalogContext.Source;
        string productSource = requestedSourceSystem == "amg"
            ? document.ProductSourceAmg
            : document.ProductSourceFenix;
        bool isCanonical = requestedSourceSystem == "amg"
            ? document.IsCanonicalAmg
            : document.IsCanonicalFenix;
        if (!isCanonical) return false;
        if (!productSource.StartsWith(
                requestedSourceSystem + ":",
                StringComparison.Ordinal))
            return false;

        if (!catalogContext.UseIndexedRetailPrice) {
            List<ProductSearchCatalogScope> matchingScopes = document.CatalogScopes
                .Where(scope => scope.OrganizationId == catalogContext.OrganizationId
                                && scope.WithVat == catalogContext.WithVat
                                && string.Equals(
                                    scope.SourceSystem,
                                    requestedSourceSystem,
                                    StringComparison.Ordinal)
                                && scope.AvailableQty > 0)
                .Take(2)
                .ToList();
            if (matchingScopes.Count != 1) return false;

            ProductSearchCatalogScope scope = matchingScopes[0];
            document.Available = true;
            document.AvailableQty = scope.AvailableQty;
            document.AvailableQtyUk = catalogContext.WithVat ? 0 : scope.AvailableQtyUk;
            document.AvailableQtyUkVat = catalogContext.WithVat ? scope.AvailableQtyUk : 0;
            document.AvailableQtyPl = catalogContext.WithVat ? 0 : scope.AvailableQtyPl;
            document.AvailableQtyPlVat = catalogContext.WithVat ? scope.AvailableQtyPl : 0;
            return true;
        }

        if (!document.IndexedPricingRevisions.MatchesExactly(catalogContext.PricingRevisions))
            return false;

        long organizationId = catalogContext.WithVat
            ? document.CatalogOrganizationIdVat
            : document.CatalogOrganizationIdNonVat;
        if (organizationId != catalogContext.OrganizationId) return false;

        string sourceSystem = catalogContext.WithVat
            ? document.CatalogSourceSystemVat
            : document.CatalogSourceSystemNonVat;
        bool hasAvailability = catalogContext.WithVat
            ? document.HasVatCatalogAvailability
            : document.HasNonVatCatalogAvailability;
        bool hasSource = catalogContext.WithVat
            ? document.HasVatCatalogSource
            : document.HasNonVatCatalogSource;
        if (!hasAvailability
            || !hasSource
            || !string.Equals(sourceSystem, requestedSourceSystem, StringComparison.Ordinal))
            return false;

        string agreementNetUid = catalogContext.WithVat
            ? document.CatalogAgreementNetUidVat
            : document.CatalogAgreementNetUidNonVat;
        long pricingId = catalogContext.WithVat
            ? document.CatalogPricingIdVat
            : document.CatalogPricingIdNonVat;
        long currencyId = catalogContext.WithVat
            ? document.CatalogCurrencyIdVat
            : document.CatalogCurrencyIdNonVat;
        decimal price = catalogContext.WithVat ? document.RetailPriceVat : document.RetailPrice;

        return Guid.TryParse(agreementNetUid, out Guid indexedAgreementNetUid)
               && indexedAgreementNetUid == catalogContext.ClientAgreementNetId
               && pricingId == catalogContext.PricingId
               && currencyId == catalogContext.CurrencyId
               && price > 0;
    }

    private static List<ProductSearchCatalogScope> ParseCatalogScopes(JsonElement source) {
        if (!source.TryGetProperty("catalogScopes", out JsonElement scopes)
            || scopes.ValueKind != JsonValueKind.Array
            || scopes.GetArrayLength() > 1024) {
            return [];
        }

        List<ProductSearchCatalogScope> result = new(scopes.GetArrayLength());
        foreach (JsonElement scope in scopes.EnumerateArray()) {
            if (scope.ValueKind != JsonValueKind.Object
                || !scope.TryGetProperty("organizationId", out JsonElement organizationId)
                || !organizationId.TryGetInt64(out long parsedOrganizationId)
                || !scope.TryGetProperty("sourceSystem", out JsonElement sourceSystem)
                || sourceSystem.ValueKind != JsonValueKind.String
                || !scope.TryGetProperty("withVat", out JsonElement withVat)
                || withVat.ValueKind is not (JsonValueKind.True or JsonValueKind.False)
                || !scope.TryGetProperty("availableQtyUk", out JsonElement availableQtyUk)
                || !availableQtyUk.TryGetDouble(out double parsedAvailableQtyUk)
                || !scope.TryGetProperty("availableQtyPl", out JsonElement availableQtyPl)
                || !availableQtyPl.TryGetDouble(out double parsedAvailableQtyPl)
                || !scope.TryGetProperty("availableQty", out JsonElement availableQty)
                || !availableQty.TryGetDouble(out double parsedAvailableQty)) {
                return [];
            }

            result.Add(new ProductSearchCatalogScope {
                OrganizationId = parsedOrganizationId,
                SourceSystem = sourceSystem.GetString() ?? string.Empty,
                WithVat = withVat.GetBoolean(),
                AvailableQtyUk = parsedAvailableQtyUk,
                AvailableQtyPl = parsedAvailableQtyPl,
                AvailableQty = parsedAvailableQty
            });
        }

        return result;
    }

    private static bool TryReadExactTotal(JsonElement hitsRoot, out int total) {
        total = 0;
        if (!hitsRoot.TryGetProperty("total", out JsonElement totalElement)
            || totalElement.ValueKind != JsonValueKind.Object
            || !totalElement.TryGetProperty("relation", out JsonElement relation)
            || relation.ValueKind != JsonValueKind.String
            || !string.Equals(relation.GetString(), "eq", StringComparison.Ordinal)
            || !totalElement.TryGetProperty("value", out JsonElement value)
            || !value.TryGetInt32(out total)
            || total < 0) {
            total = 0;
            return false;
        }

        return true;
    }

    private static List<string> ParseStringArray(JsonElement source, string propertyName) {
        if (!source.TryGetProperty(propertyName, out JsonElement prop) || prop.ValueKind != JsonValueKind.Array)
            return [];

        List<string> result = new List<string>();
        foreach (JsonElement item in prop.EnumerateArray()) {
            if (item.ValueKind == JsonValueKind.String) {
                result.Add(item.GetString() ?? "");
            }
        }
        return result;
    }

    private static string EscapeElasticsearchQuery(string query) {
        return SpecialCharsRegex.Replace(query, @"\$0");
    }

    private sealed record SearchResult(List<long> Ids, int Total, int TookMs);
    private sealed record SearchResultWithDocs(List<ProductSearchDocument> Documents, int Total, int TookMs);
}

public sealed class ElasticsearchDebugResult {
    public string OriginalQuery { get; set; } = "";
    public string NormalizedQuery { get; set; } = "";
    public string Locale { get; set; } = "";
    public List<string> Terms { get; set; } = [];
    public string ElasticsearchQuery { get; set; } = "";
    public int TotalFound { get; set; }
    public int SearchTimeMs { get; set; }
    public List<long> ProductIds { get; set; } = [];
}
