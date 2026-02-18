using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly HttpClient _http;
    private readonly ElasticsearchSettings _settings;
    private readonly SearchTextProcessor _textProcessor;
    private readonly ILogger<ElasticsearchProductSearchService> _log;

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly Regex SpecialCharsRegex = new(@"[+\-=&|><!(){}\[\]^""~*?:\\/]", RegexOptions.Compiled);

    public ElasticsearchProductSearchService(
        HttpClient httpClient,
        IOptions<ElasticsearchSettings> settings,
        SearchTextProcessor textProcessor,
        ILogger<ElasticsearchProductSearchService> logger) {
        _http = httpClient;
        _settings = settings.Value;
        _textProcessor = textProcessor;
        _log = logger;
    }

    public async Task<ProductSearchResult> SearchAsync(
        string query, string locale = "uk", int limit = 20, int offset = 0, CancellationToken ct = default) {

        if (string.IsNullOrWhiteSpace(query))
            return ProductSearchResult.Empty;

        var esQuery = BuildSearchQuery(query, locale, limit, offset);
        var result = await ExecuteSearchAsync(esQuery, ct);

        return new ProductSearchResult {
            ProductIds = result.Ids,
            TotalCount = result.Total,
            SearchTimeMs = result.TookMs,
            IsFallback = false
        };
    }

    public async Task<ProductSearchResultWithDocs> SearchWithDocsAsync(
        string query, string locale = "uk", int limit = 20, int offset = 0, CancellationToken ct = default) {

        if (string.IsNullOrWhiteSpace(query))
            return ProductSearchResultWithDocs.Empty;

        var esQuery = BuildSearchQuery(query, locale, limit, offset);
        var result = await ExecuteSearchWithDocsAsync(esQuery, ct);

        return new ProductSearchResultWithDocs {
            Documents = result.Documents,
            TotalCount = result.Total,
            SearchTimeMs = result.TookMs,
            IsFallback = false
        };
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default) {
        try {
            var response = await _http.GetAsync("_cluster/health", ct);
            return response.IsSuccessStatusCode;
        } catch {
            return false;
        }
    }

    public async Task<ElasticsearchDebugResult> SearchDebugAsync(
        string query, string locale = "uk", int limit = 20, int offset = 0, CancellationToken ct = default) {

        var debug = new ElasticsearchDebugResult {
            OriginalQuery = query,
            Locale = locale
        };

        if (string.IsNullOrWhiteSpace(query)) {
            return debug;
        }

        var normalized = NumberNormalizer.NormalizeQuery(query);
        var terms = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        debug.NormalizedQuery = normalized;
        debug.Terms = terms.ToList();

        var esQuery = BuildSearchQuery(query, locale, limit, offset);
        debug.ElasticsearchQuery = JsonSerializer.Serialize(esQuery, new JsonSerializerOptions { WriteIndented = true });

        var result = await ExecuteSearchAsync(esQuery, ct);

        debug.TotalFound = result.Total;
        debug.SearchTimeMs = result.TookMs;
        debug.ProductIds = result.Ids;

        return debug;
    }

    private object BuildSearchQuery(string query, string locale, int limit, int offset) {
        var normalized = NumberNormalizer.NormalizeQuery(query);
        var normalizedLower = normalized.ToLowerInvariant();
        var terms = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => EscapeElasticsearchQuery(t.ToLowerInvariant()))
            .ToArray();
        // Keep original terms for size field (preserves "=" and other special chars)
        var originalTerms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.ToLowerInvariant())
            .ToArray();

        if (terms.Length == 0)
            return BuildMatchAllQuery(limit, offset);

        // Build must clauses - each term must match somewhere (AND logic between terms)
        var mustClauses = new List<object>();
        for (int i = 0; i < terms.Length; i++) {
            var originalTerm = i < originalTerms.Length ? originalTerms[i] : terms[i];
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
        var functions = new List<object>();

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

        var (primarySearchName, secondarySearchName) = locale == "uk"
            ? ("searchNameUA", "searchName")
            : ("searchName", "searchNameUA");
        var (primaryDesc, secondaryDesc) = locale == "uk"
            ? ("searchDescriptionUA", "searchDescription")
            : ("searchDescription", "searchDescriptionUA");

        // Per-term scoring with different weights based on term type
        for (int i = 0; i < terms.Length; i++) {
            var term = terms[i];
            var originalTerm = i < originalTerms.Length ? originalTerms[i] : term;
            var termLower = term.ToLowerInvariant();
            bool isCyrillic = term.Any(c =>
                (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я') ||
                c == 'і' || c == 'ї' || c == 'є' || c == 'ґ' ||
                c == 'І' || c == 'Ї' || c == 'Є' || c == 'Ґ');
            bool hasDigits = term.Any(char.IsDigit);
            bool useWildcard = term.Length >= 3 && term.Length <= 6;
            string wildcardPattern = $"*{termLower}*";

            if (isCyrillic) {
                // Cyrillic term: prioritize name matches (like V1 SQL does with PATINDEX on SearchNameUA)
                // 6. Name match - highest priority for Cyrillic
                if (useWildcard) {
                    functions.Add(new {
                        filter = new { wildcard = new Dictionary<string, object> { [primarySearchName] = new { value = wildcardPattern, case_insensitive = true } } },
                        weight = 3000
                    });
                    functions.Add(new {
                        filter = new { wildcard = new Dictionary<string, object> { [secondarySearchName] = new { value = wildcardPattern, case_insensitive = true } } },
                        weight = 2500
                    });
                } else {
                    functions.Add(new {
                        filter = new { match = new Dictionary<string, object> { [$"{primarySearchName}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                        weight = 3000
                    });
                    functions.Add(new {
                        filter = new { match = new Dictionary<string, object> { [$"{secondarySearchName}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                        weight = 2500
                    });
                }

                // 7. Description match
                if (useWildcard) {
                    functions.Add(new {
                        filter = new { wildcard = new Dictionary<string, object> { [primaryDesc] = new { value = wildcardPattern, case_insensitive = true } } },
                        weight = 500
                    });
                    functions.Add(new {
                        filter = new { wildcard = new Dictionary<string, object> { [secondaryDesc] = new { value = wildcardPattern, case_insensitive = true } } },
                        weight = 400
                    });
                } else {
                    functions.Add(new {
                        filter = new { match = new Dictionary<string, object> { [$"{primaryDesc}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                        weight = 500
                    });
                    functions.Add(new {
                        filter = new { match = new Dictionary<string, object> { [$"{secondaryDesc}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                        weight = 400
                    });
                }
            } else {
                // Latin/numeric term: prioritize vendorCode higher than originalNumbers
                // V1 SQL actually puts VendorCode_Match before combined Name/OriginalNumber match
                // For brand names like VOLVO, MAN - vendorCode is more important

                // 4. VendorCode match - highest priority for Latin brand names!
                if (useWildcard) {
                    functions.Add(new {
                        filter = new { wildcard = new Dictionary<string, object> { ["vendorCodeClean"] = new { value = wildcardPattern } } },
                        weight = 5000
                    });
                } else {
                    functions.Add(new {
                        filter = new { match = new Dictionary<string, object> { ["vendorCodeClean.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                        weight = 5000
                    });
                }

                // 5. OriginalNumber match
                if (useWildcard) {
                    functions.Add(new {
                        filter = new {
                            @bool = new {
                                should = new object[] {
                                    new { wildcard = new Dictionary<string, object> { ["mainOriginalNumberClean"] = new { value = wildcardPattern } } },
                                    new { wildcard = new Dictionary<string, object> { ["originalNumbersClean"] = new { value = wildcardPattern } } }
                                }
                            }
                        },
                        weight = 3000
                    });
                } else {
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
                }

                // 6. Name match for Latin terms (brand names like BOSCH, MANN)
                if (useWildcard) {
                    functions.Add(new {
                        filter = new { wildcard = new Dictionary<string, object> { [primarySearchName] = new { value = wildcardPattern, case_insensitive = true } } },
                        weight = 1500
                    });
                    functions.Add(new {
                        filter = new { wildcard = new Dictionary<string, object> { [secondarySearchName] = new { value = wildcardPattern, case_insensitive = true } } },
                        weight = 1200
                    });
                } else {
                    functions.Add(new {
                        filter = new { match = new Dictionary<string, object> { [$"{primarySearchName}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                        weight = 1500
                    });
                    functions.Add(new {
                        filter = new { match = new Dictionary<string, object> { [$"{secondarySearchName}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                        weight = 1200
                    });
                }

                // 7. Description match for Latin terms (lowest priority)
                if (useWildcard) {
                    functions.Add(new {
                        filter = new { wildcard = new Dictionary<string, object> { [primaryDesc] = new { value = wildcardPattern, case_insensitive = true } } },
                        weight = 300
                    });
                    functions.Add(new {
                        filter = new { wildcard = new Dictionary<string, object> { [secondaryDesc] = new { value = wildcardPattern, case_insensitive = true } } },
                        weight = 200
                    });
                } else {
                    functions.Add(new {
                        filter = new { match = new Dictionary<string, object> { [$"{primaryDesc}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                        weight = 300
                    });
                    functions.Add(new {
                        filter = new { match = new Dictionary<string, object> { [$"{secondaryDesc}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } },
                        weight = 200
                    });
                }
            }

            // 8. Size match (for all terms)
            // Higher weight for dimension-like terms (e.g., "d=45", "15x5", "M14x1.5")
            bool isDimensionTerm = term.Contains('=') || term.Contains('x') ||
                (hasDigits && term.Length <= 10 && !isCyrillic);
            // Very high weight for size when it looks like a dimension query - must beat originalNumbers (3000)
            int sizeWeight = isDimensionTerm ? 8000 : 100;

            // Search original size field using originalTerm (preserves "=", "x" etc.)
            var originalTermLowerForSize = originalTerm.ToLowerInvariant();
            functions.Add(new {
                filter = new { wildcard = new Dictionary<string, object> { ["size"] = new { value = $"*{originalTermLowerForSize}*", case_insensitive = true } } },
                weight = sizeWeight
            });
            // Also search sizeClean with cleaned term
            var termCleanForSize = Regex.Replace(termLower, @"[^a-z0-9а-яіїєґ]", "");
            if (!string.IsNullOrEmpty(termCleanForSize)) {
                functions.Add(new {
                    filter = new { wildcard = new Dictionary<string, object> { ["sizeClean"] = new { value = $"*{termCleanForSize}*" } } },
                    weight = sizeWeight
                });
            }
        }

        return new {
            from = offset,
            size = limit,
            query = new {
                function_score = new {
                    query = new {
                        @bool = new {
                            must = mustClauses,
                            filter = new[] {
                                new { term = new { isForWeb = true } }
                            }
                        }
                    },
                    functions = functions,
                    score_mode = "sum",
                    boost_mode = "replace"
                }
            },
            sort = new object[] {
                "_score",
                new Dictionary<string, object> { ["nameUA.keyword"] = new { order = "asc" } },
                new { id = new { order = "asc" } }
            }
        };
    }

    private static object BuildTermMatchQuery(string term, string locale, string originalTerm = null) {
        // Each term can match in ANY of these fields (OR logic within term)
        // Using wildcard for PATINDEX-like behavior (substring anywhere in field)
        var shouldClauses = new List<object>();

        // Lowercase for case-insensitive matching
        var termLower = term.ToLowerInvariant();
        var originalTermLower = (originalTerm ?? term).ToLowerInvariant();

        // For short terms (< 4 chars), use wildcard for exact substring match
        // For longer terms, ngram is fine
        bool useWildcard = term.Length >= 3 && term.Length <= 6;
        string wildcardPattern = $"*{termLower}*";

        // VendorCode - wildcard for exact substring
        if (useWildcard) {
            shouldClauses.Add(new { wildcard = new Dictionary<string, object> { ["vendorCodeClean"] = new { value = wildcardPattern } } });
        } else {
            shouldClauses.Add(new { match = new Dictionary<string, object> { ["vendorCodeClean.ngram"] = new { query = termLower, minimum_should_match = "80%" } } });
        }

        // Original numbers
        if (useWildcard) {
            shouldClauses.Add(new { wildcard = new Dictionary<string, object> { ["mainOriginalNumberClean"] = new { value = wildcardPattern } } });
            shouldClauses.Add(new { wildcard = new Dictionary<string, object> { ["originalNumbersClean"] = new { value = wildcardPattern } } });
        } else {
            shouldClauses.Add(new { match = new Dictionary<string, object> { ["mainOriginalNumberClean.ngram"] = new { query = termLower, minimum_should_match = "80%" } } });
            shouldClauses.Add(new { match = new Dictionary<string, object> { ["originalNumbersClean.ngram"] = new { query = termLower, minimum_should_match = "80%" } } });
        }

        // Names - locale-aware, use wildcard for precise PATINDEX-like matching
        var (primaryName, secondaryName) = locale == "uk"
            ? ("searchNameUA", "searchName")
            : ("searchName", "searchNameUA");

        if (useWildcard) {
            shouldClauses.Add(new { wildcard = new Dictionary<string, object> { [primaryName] = new { value = wildcardPattern, case_insensitive = true } } });
            shouldClauses.Add(new { wildcard = new Dictionary<string, object> { [secondaryName] = new { value = wildcardPattern, case_insensitive = true } } });
        } else {
            shouldClauses.Add(new { match = new Dictionary<string, object> { [$"{primaryName}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } });
            shouldClauses.Add(new { match = new Dictionary<string, object> { [$"{secondaryName}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } });
        }

        // Descriptions
        var (primaryDesc, secondaryDesc) = locale == "uk"
            ? ("searchDescriptionUA", "searchDescription")
            : ("searchDescription", "searchDescriptionUA");

        if (useWildcard) {
            shouldClauses.Add(new { wildcard = new Dictionary<string, object> { [primaryDesc] = new { value = wildcardPattern, case_insensitive = true } } });
            shouldClauses.Add(new { wildcard = new Dictionary<string, object> { [secondaryDesc] = new { value = wildcardPattern, case_insensitive = true } } });
        } else {
            shouldClauses.Add(new { match = new Dictionary<string, object> { [$"{primaryDesc}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } });
            shouldClauses.Add(new { match = new Dictionary<string, object> { [$"{secondaryDesc}.ngram"] = new { query = termLower, minimum_should_match = "80%" } } });
        }

        // Size - search both original (with special chars like "=") and clean version
        // For dimension queries like "d=45", the original size field "D=45 h=104" is more relevant
        // Use originalTermLower to preserve "=" and other special chars in size search
        shouldClauses.Add(new { wildcard = new Dictionary<string, object> { ["size"] = new { value = $"*{originalTermLower}*", case_insensitive = true } } });
        // Also search sizeClean for normalized matches (without special chars)
        var termClean = Regex.Replace(termLower, @"[^a-z0-9а-яіїєґ]", "");
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

    private static object BuildMatchAllQuery(int limit, int offset) {
        return new {
            from = offset,
            size = limit,
            query = new {
                @bool = new {
                    filter = new[] {
                        new { term = new { isForWeb = true } }
                    }
                }
            },
            sort = new object[] {
                new { available = new { order = "desc" } },
                new { availableQtyUk = new { order = "desc" } }
            }
        };
    }

    private async Task<SearchResult> ExecuteSearchAsync(object query, CancellationToken ct) {
        var json = JsonSerializer.Serialize(query, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync($"{_settings.IndexName}/_search", content, ct);

        if (!response.IsSuccessStatusCode) {
            var error = await response.Content.ReadAsStringAsync(ct);
            _log.LogError("Elasticsearch search failed: {Error}", error);
            return new SearchResult([], 0, 0);
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        var took = root.GetProperty("took").GetInt32();
        var total = root.GetProperty("hits").GetProperty("total").GetProperty("value").GetInt32();
        var hits = root.GetProperty("hits").GetProperty("hits");

        var ids = new List<long>();
        foreach (var hit in hits.EnumerateArray()) {
            if (hit.TryGetProperty("_source", out var source) &&
                source.TryGetProperty("id", out var idProp)) {
                ids.Add(idProp.GetInt64());
            }
        }

        return new SearchResult(ids, total, took);
    }

    private async Task<SearchResultWithDocs> ExecuteSearchWithDocsAsync(object query, CancellationToken ct) {
        var json = JsonSerializer.Serialize(query, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync($"{_settings.IndexName}/_search", content, ct);

        if (!response.IsSuccessStatusCode) {
            var error = await response.Content.ReadAsStringAsync(ct);
            _log.LogError("Elasticsearch search failed: {Error}", error);
            return new SearchResultWithDocs([], 0, 0);
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        var took = root.GetProperty("took").GetInt32();
        var total = root.GetProperty("hits").GetProperty("total").GetProperty("value").GetInt32();
        var hits = root.GetProperty("hits").GetProperty("hits");

        var documents = new List<ProductSearchDocument>();
        foreach (var hit in hits.EnumerateArray()) {
            if (hit.TryGetProperty("_source", out var source)) {
                var document = ParseDocument(source);
                documents.Add(document);
            }
        }

        return new SearchResultWithDocs(documents, total, took);
    }

    private static ProductSearchDocument ParseDocument(JsonElement source) {
        return new ProductSearchDocument {
            Id = source.TryGetProperty("id", out var id) ? id.GetInt64().ToString() : "",
            NetUid = source.TryGetProperty("netUid", out var netUid) ? netUid.GetString() ?? "" : "",
            VendorCode = source.TryGetProperty("vendorCode", out var vc) ? vc.GetString() ?? "" : "",
            VendorCodeClean = source.TryGetProperty("vendorCodeClean", out var vcc) ? vcc.GetString() ?? "" : "",
            Name = source.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
            NameUA = source.TryGetProperty("nameUA", out var nameUA) ? nameUA.GetString() ?? "" : "",
            Description = source.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
            DescriptionUA = source.TryGetProperty("descriptionUA", out var descUA) ? descUA.GetString() ?? "" : "",
            SearchName = source.TryGetProperty("searchName", out var sn) ? sn.GetString() ?? "" : "",
            SearchNameUA = source.TryGetProperty("searchNameUA", out var snUA) ? snUA.GetString() ?? "" : "",
            SearchDescription = source.TryGetProperty("searchDescription", out var sd) ? sd.GetString() ?? "" : "",
            SearchDescriptionUA = source.TryGetProperty("searchDescriptionUA", out var sdUA) ? sdUA.GetString() ?? "" : "",
            MainOriginalNumber = source.TryGetProperty("mainOriginalNumber", out var mon) ? mon.GetString() ?? "" : "",
            MainOriginalNumberClean = source.TryGetProperty("mainOriginalNumberClean", out var monc) ? monc.GetString() ?? "" : "",
            OriginalNumbers = ParseStringArray(source, "originalNumbers"),
            OriginalNumbersClean = ParseStringArray(source, "originalNumbersClean"),
            Size = source.TryGetProperty("size", out var size) ? size.GetString() ?? "" : "",
            SizeClean = source.TryGetProperty("sizeClean", out var sc) ? sc.GetString() ?? "" : "",
            PackingStandard = source.TryGetProperty("packingStandard", out var ps) ? ps.GetString() ?? "" : "",
            OrderStandard = source.TryGetProperty("orderStandard", out var os) ? os.GetString() ?? "" : "",
            Ucgfea = source.TryGetProperty("ucgfea", out var ucg) ? ucg.GetString() ?? "" : "",
            Volume = source.TryGetProperty("volume", out var vol) ? vol.GetString() ?? "" : "",
            Top = source.TryGetProperty("top", out var top) ? top.GetString() ?? "" : "",
            Weight = source.TryGetProperty("weight", out var weight) ? weight.GetDouble() : 0,
            HasAnalogue = source.TryGetProperty("hasAnalogue", out var ha) && ha.GetBoolean(),
            HasComponent = source.TryGetProperty("hasComponent", out var hc) && hc.GetBoolean(),
            HasImage = source.TryGetProperty("hasImage", out var hi) && hi.GetBoolean(),
            Image = source.TryGetProperty("image", out var img) ? img.GetString() ?? "" : "",
            MeasureUnitId = source.TryGetProperty("measureUnitId", out var mui) ? mui.GetInt64() : 0,
            Available = source.TryGetProperty("available", out var avail) && avail.GetBoolean(),
            AvailableQtyUk = source.TryGetProperty("availableQtyUk", out var aqUk) ? aqUk.GetDouble() : 0,
            AvailableQtyUkVat = source.TryGetProperty("availableQtyUkVat", out var aqUkVat) ? aqUkVat.GetDouble() : 0,
            AvailableQtyPl = source.TryGetProperty("availableQtyPl", out var aqPl) ? aqPl.GetDouble() : 0,
            AvailableQtyPlVat = source.TryGetProperty("availableQtyPlVat", out var aqPlVat) ? aqPlVat.GetDouble() : 0,
            AvailableQty = source.TryGetProperty("availableQty", out var aq) ? aq.GetDouble() : 0,
            IsForWeb = source.TryGetProperty("isForWeb", out var ifw) && ifw.GetBoolean(),
            IsForSale = source.TryGetProperty("isForSale", out var ifs) && ifs.GetBoolean(),
            IsForZeroSale = source.TryGetProperty("isForZeroSale", out var ifzs) && ifzs.GetBoolean(),
            SlugId = source.TryGetProperty("slugId", out var slugId) ? slugId.GetInt64() : 0,
            SlugNetUid = source.TryGetProperty("slugNetUid", out var slugNetUid) ? slugNetUid.GetString() ?? "" : "",
            SlugUrl = source.TryGetProperty("slugUrl", out var slugUrl) ? slugUrl.GetString() ?? "" : "",
            SlugLocale = source.TryGetProperty("slugLocale", out var slugLocale) ? slugLocale.GetString() ?? "" : "",
            RetailPrice = source.TryGetProperty("retailPrice", out var rp) ? rp.GetDecimal() : 0,
            RetailPriceVat = source.TryGetProperty("retailPriceVat", out var rpv) ? rpv.GetDecimal() : 0,
            RetailCurrencyCode = source.TryGetProperty("retailCurrencyCode", out var rcc) ? rcc.GetString() ?? "UAH" : "UAH",
            UpdatedAt = source.TryGetProperty("updatedAt", out var updatedAt) && updatedAt.TryGetDateTime(out var dt)
                ? new DateTimeOffset(dt).ToUnixTimeSeconds()
                : 0
        };
    }

    private static List<string> ParseStringArray(JsonElement source, string propertyName) {
        if (!source.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.Array)
            return [];

        var result = new List<string>();
        foreach (var item in prop.EnumerateArray()) {
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
