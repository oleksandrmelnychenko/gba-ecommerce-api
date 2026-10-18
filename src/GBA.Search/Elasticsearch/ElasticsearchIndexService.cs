using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using GBA.Search.Sync;
using Microsoft.Extensions.Options;

namespace GBA.Search.Elasticsearch;

public interface IElasticsearchIndexService {
    Task<bool> CreateIndexAsync(CancellationToken ct = default);
    Task<bool> DeleteIndexAsync(CancellationToken ct = default);
    Task<bool> IndexExistsAsync(CancellationToken ct = default);
    Task<bool> IsHealthyAsync(CancellationToken ct = default);

    /// <summary>Creates a new uniquely-named index (with mappings) and returns its name, for alias-swap rebuilds.</summary>
    Task<string?> CreateVersionedIndexAsync(CancellationToken ct = default);

    /// <summary>Atomically points the search alias (IndexName) at <paramref name="targetIndex"/>, removing it from any previous index.</summary>
    Task<bool> SwapAliasAsync(string targetIndex, CancellationToken ct = default);

    /// <summary>Deletes old versioned indices, keeping the newest <paramref name="keep"/>. Returns the number deleted.</summary>
    Task<int> CleanupOldVersionedIndicesAsync(int keep, CancellationToken ct = default);

    Task<bool> AliasExistsAsync(CancellationToken ct = default);

    Task<long> CountDocumentsAsync(CancellationToken ct = default);

    Task<List<long>> GetIndexedProductIdsAsync(CancellationToken ct = default);

    Task<string?> GetIndexSchemaVersionAsync(CancellationToken ct = default);
}

public sealed class ElasticsearchIndexService : IElasticsearchIndexService {
    private readonly HttpClient _http;
    private readonly ElasticsearchSettings _settings;
    private readonly ILogger<ElasticsearchIndexService> _log;

    private const int IdScanPageSize = 10000;

    public const string IndexSchemaVersion = "products-v2-lookalike-fold";

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public ElasticsearchIndexService(
        HttpClient httpClient,
        IOptions<ElasticsearchSettings> settings,
        ILogger<ElasticsearchIndexService> logger) {
        _http = httpClient;
        _settings = settings.Value;
        _log = logger;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default) {
        try {
            HttpResponseMessage response = await _http.GetAsync("_cluster/health", ct);
            if (!response.IsSuccessStatusCode) return false;

            string responseBody = await response.Content.ReadAsStringAsync(ct);
            using JsonDocument document = JsonDocument.Parse(responseBody);
            JsonElement root = document.RootElement;
            if (!root.TryGetProperty("status", out JsonElement statusElement)) {
                return false;
            }

            string? status = statusElement.GetString();
            bool timedOut = root.TryGetProperty(
                                "timed_out",
                                out JsonElement timedOutElement)
                            && timedOutElement.ValueKind == JsonValueKind.True;
            return !timedOut && status is "green" or "yellow";
        } catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            throw;
        } catch {
            return false;
        }
    }

    public async Task<bool> IndexExistsAsync(CancellationToken ct = default) {
        HttpResponseMessage response = await _http.GetAsync(_settings.IndexName, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteIndexAsync(CancellationToken ct = default) {
        HttpResponseMessage response = await _http.DeleteAsync(_settings.IndexName, ct);
        if (response.IsSuccessStatusCode) {
            _log.LogInformation("Deleted index {Index}", _settings.IndexName);
            return true;
        }
        return false;
    }

    public Task<bool> CreateIndexAsync(CancellationToken ct = default) => CreateIndexAsync(_settings.IndexName, ct);

    private async Task<bool> CreateIndexAsync(string indexName, CancellationToken ct) {
        object indexSettings = BuildIndexSettings();
        string json = JsonSerializer.Serialize(indexSettings, JsonOptions);
        StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _http.PutAsync(indexName, content, ct);

        if (response.IsSuccessStatusCode) {
            _log.LogInformation("Created index {Index}", indexName);
            return true;
        }

        string error = await response.Content.ReadAsStringAsync(ct);
        _log.LogError("Failed to create index {Index}: {Error}", indexName, error);
        return false;
    }

    public async Task<string?> CreateVersionedIndexAsync(CancellationToken ct = default) {
        string indexName = $"{_settings.IndexName}_{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        return await CreateIndexAsync(indexName, ct) ? indexName : null;
    }

    public async Task<bool> SwapAliasAsync(string targetIndex, CancellationToken ct = default) {
        string alias = _settings.IndexName;

        // One-time migration: if a concrete index still occupies the alias name, drop it
        // (an alias and a concrete index cannot share a name). Steady-state swaps skip this.
        HttpResponseMessage aliasProbe = await _http.GetAsync($"_alias/{alias}", ct);
        if (aliasProbe.StatusCode == HttpStatusCode.NotFound) {
            HttpResponseMessage indexProbe = await _http.GetAsync(alias, ct);
            if (indexProbe.IsSuccessStatusCode) {
                await _http.DeleteAsync(alias, ct);
                _log.LogInformation("Removed legacy concrete index {Index} to free the alias name", alias);
            }
        }

        var body = new {
            actions = new object[] {
                new { remove = new { index = $"{alias}_*", alias, must_exist = false } },
                new { add = new { index = targetIndex, alias } }
            }
        };
        StringContent content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await _http.PostAsync("_aliases", content, ct);

        if (response.IsSuccessStatusCode) {
            _log.LogInformation("Alias {Alias} now points to {Index}", alias, targetIndex);
            return true;
        }

        string error = await response.Content.ReadAsStringAsync(ct);
        _log.LogError("Failed to swap alias {Alias} -> {Index}: {Error}", alias, targetIndex, error);
        return false;
    }

    public async Task<int> CleanupOldVersionedIndicesAsync(int keep, CancellationToken ct = default) {
        HttpResponseMessage response = await _http.GetAsync($"_cat/indices/{_settings.IndexName}_*?h=index&format=json", ct);
        if (!response.IsSuccessStatusCode) return 0;

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        List<string> indices = doc.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("index").GetString() ?? string.Empty)
            .ToList();
        HashSet<string> aliasTargets = await GetAliasTargetsAsync(ct);

        int deleted = 0;
        foreach (string old in SelectIndicesToDelete(_settings.IndexName, indices, aliasTargets, keep)) {
            HttpResponseMessage del = await _http.DeleteAsync(old, ct);
            if (del.IsSuccessStatusCode) {
                deleted++;
                _log.LogInformation("Deleted old index {Index}", old);
            }
        }
        return deleted;
    }

    internal static IReadOnlyList<string> SelectIndicesToDelete(
        string alias,
        IEnumerable<string> indices,
        IReadOnlySet<string> aliasTargets,
        int keep) {
        List<string> versioned = indices
            .Where(name => IsVersionedIndexName(alias, name))
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(name => name, StringComparer.Ordinal)
            .ToList();

        return versioned
            .Skip(Math.Max(1, keep))
            .Where(name => !aliasTargets.Contains(name))
            .ToList();
    }

    internal static bool IsVersionedIndexName(string alias, string? name) {
        if (string.IsNullOrEmpty(name) || !name.StartsWith(alias + "_", StringComparison.Ordinal)) {
            return false;
        }

        string suffix = name[(alias.Length + 1)..];
        int digits = 0;
        while (digits < suffix.Length && char.IsAsciiDigit(suffix[digits])) digits++;
        return digits >= 14 && (digits == suffix.Length || suffix[digits] == '_');
    }

    private async Task<HashSet<string>> GetAliasTargetsAsync(CancellationToken ct) {
        HttpResponseMessage response = await _http.GetAsync($"_alias/{_settings.IndexName}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return new HashSet<string>(StringComparer.Ordinal);

        string body = await response.Content.ReadAsStringAsync(ct);
        ElasticsearchSyncService.EnsureSuccessfulHttpResponse(response, "alias lookup", body);
        using JsonDocument doc = JsonDocument.Parse(body);
        return doc.RootElement.EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
    }

    public async Task<bool> AliasExistsAsync(CancellationToken ct = default) {
        HttpResponseMessage response = await _http.GetAsync($"_alias/{_settings.IndexName}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return false;

        string body = await response.Content.ReadAsStringAsync(ct);
        ElasticsearchSyncService.EnsureSuccessfulHttpResponse(response, "alias lookup", body);
        return true;
    }

    public async Task<long> CountDocumentsAsync(CancellationToken ct = default) {
        HttpResponseMessage response = await _http.GetAsync($"{_settings.IndexName}/_count", ct);
        string body = await response.Content.ReadAsStringAsync(ct);
        ElasticsearchSyncService.EnsureSuccessfulHttpResponse(response, "document count", body);

        using JsonDocument doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("count", out JsonElement count) && count.TryGetInt64(out long value)) {
            return value;
        }

        throw new InvalidOperationException("Elasticsearch count response does not contain a count.");
    }

    public async Task<string?> GetIndexSchemaVersionAsync(CancellationToken ct = default) {
        HttpResponseMessage response = await _http.GetAsync($"{_settings.IndexName}/_mapping", ct);
        string body = await response.Content.ReadAsStringAsync(ct);
        ElasticsearchSyncService.EnsureSuccessfulHttpResponse(response, "mapping read", body);

        using JsonDocument doc = JsonDocument.Parse(body);
        foreach (JsonProperty index in doc.RootElement.EnumerateObject()) {
            return index.Value.TryGetProperty("mappings", out JsonElement mappings)
                   && mappings.TryGetProperty("_meta", out JsonElement meta)
                   && meta.TryGetProperty("schemaVersion", out JsonElement version)
                   && version.ValueKind == JsonValueKind.String
                ? version.GetString()
                : null;
        }

        return null;
    }

    public async Task<List<long>> GetIndexedProductIdsAsync(CancellationToken ct = default) {
        List<long> ids = new();
        long? searchAfter = null;

        while (true) {
            Dictionary<string, object> request = new() {
                ["size"] = IdScanPageSize,
                ["_source"] = false,
                ["track_total_hits"] = false,
                ["sort"] = new object[] { new Dictionary<string, object> { ["id"] = "asc" } }
            };
            if (searchAfter.HasValue) {
                request["search_after"] = new object[] { searchAfter.Value };
            }

            StringContent content = new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _http.PostAsync($"{_settings.IndexName}/_search", content, ct);
            string body = await response.Content.ReadAsStringAsync(ct);
            ElasticsearchSyncService.EnsureSuccessfulHttpResponse(response, "id scan", body);

            using JsonDocument doc = JsonDocument.Parse(body);
            JsonElement hits = doc.RootElement.GetProperty("hits").GetProperty("hits");
            int pageCount = 0;
            foreach (JsonElement hit in hits.EnumerateArray()) {
                long id = hit.GetProperty("sort")[0].GetInt64();
                ids.Add(id);
                searchAfter = id;
                pageCount++;
            }

            if (pageCount < IdScanPageSize) return ids;
        }
    }

    private static object BuildIndexSettings() {
        return new {
            settings = new {
                number_of_shards = 1,
                number_of_replicas = 0,
                // Full rebuilds bulk-load into a hidden versioned index. Refresh once,
                // immediately before the alias swap, instead of after every second.
                refresh_interval = "-1",
                max_ngram_diff = 15,
                analysis = new {
                    char_filter = new {
                        lookalike_fold = new {
                            type = "mapping",
                            mappings = NumberNormalizer.LookalikeCharFilterMappings().ToArray()
                        }
                    },
                    analyzer = new {
                        // Ngram analyzer for substring matching (LIKE '%term%')
                        ngram_analyzer = new {
                            type = "custom",
                            char_filter = new[] { "lookalike_fold" },
                            tokenizer = "ngram_tokenizer",
                            filter = new[] { "lowercase" }
                        },
                        // Ukrainian analyzer for morphology
                        ukrainian_analyzer = new {
                            type = "custom",
                            tokenizer = "standard",
                            filter = new[] { "lowercase", "ukrainian_stemmer" }
                        },
                        // Simple lowercase analyzer
                        lowercase_analyzer = new {
                            type = "custom",
                            char_filter = new[] { "lookalike_fold" },
                            tokenizer = "keyword",
                            filter = new[] { "lowercase" }
                        }
                    },
                    tokenizer = new {
                        ngram_tokenizer = new {
                            type = "ngram",
                            min_gram = 3,
                            max_gram = 15,
                            token_chars = new[] { "letter", "digit" }
                        }
                    },
                    filter = new {
                        ukrainian_stemmer = new {
                            type = "stemmer",
                            language = "russian" // Elasticsearch doesn't have Ukrainian, Russian is closest
                        }
                    }
                }
            },
            mappings = new {
                _meta = new { schemaVersion = IndexSchemaVersion },
                properties = new {
                    id = new { type = "long" },
                    netUid = new { type = "keyword" },

                    // Vendor code - exact and substring
                    vendorCode = new { type = "keyword" },
                    vendorCodeClean = new {
                        type = "text",
                        analyzer = "lowercase_analyzer",
                        fields = new {
                            ngram = new {
                                type = "text",
                                analyzer = "ngram_analyzer"
                            }
                        }
                    },

                    // Names - full text, morphology, and substring
                    name = new {
                        type = "text",
                        analyzer = "standard",
                        fields = new {
                            keyword = new { type = "keyword" },
                            ukrainian = new {
                                type = "text",
                                analyzer = "ukrainian_analyzer"
                            }
                        }
                    },
                    nameUA = new {
                        type = "text",
                        analyzer = "standard",
                        fields = new {
                            keyword = new { type = "keyword" },
                            ukrainian = new {
                                type = "text",
                                analyzer = "ukrainian_analyzer"
                            }
                        }
                    },

                    // Search names (no spaces) - for PATINDEX-like matching
                    searchName = new {
                        type = "text",
                        analyzer = "lowercase_analyzer",
                        fields = new {
                            ngram = new {
                                type = "text",
                                analyzer = "ngram_analyzer"
                            }
                        }
                    },
                    searchNameUA = new {
                        type = "text",
                        analyzer = "lowercase_analyzer",
                        fields = new {
                            ngram = new {
                                type = "text",
                                analyzer = "ngram_analyzer"
                            }
                        }
                    },

                    // Descriptions
                    description = new {
                        type = "text",
                        analyzer = "standard",
                        fields = new {
                            ukrainian = new {
                                type = "text",
                                analyzer = "ukrainian_analyzer"
                            }
                        }
                    },
                    descriptionUA = new {
                        type = "text",
                        analyzer = "standard",
                        fields = new {
                            ukrainian = new {
                                type = "text",
                                analyzer = "ukrainian_analyzer"
                            }
                        }
                    },
                    searchDescription = new {
                        type = "text",
                        analyzer = "lowercase_analyzer",
                        fields = new {
                            ngram = new {
                                type = "text",
                                analyzer = "ngram_analyzer"
                            }
                        }
                    },
                    searchDescriptionUA = new {
                        type = "text",
                        analyzer = "lowercase_analyzer",
                        fields = new {
                            ngram = new {
                                type = "text",
                                analyzer = "ngram_analyzer"
                            }
                        }
                    },

                    // Original numbers - exact and substring
                    mainOriginalNumber = new { type = "keyword" },
                    mainOriginalNumberClean = new {
                        type = "text",
                        analyzer = "lowercase_analyzer",
                        fields = new {
                            ngram = new {
                                type = "text",
                                analyzer = "ngram_analyzer"
                            }
                        }
                    },
                    originalNumbers = new { type = "keyword" },
                    originalNumbersClean = new {
                        type = "text",
                        analyzer = "lowercase_analyzer",
                        fields = new {
                            ngram = new {
                                type = "text",
                                analyzer = "ngram_analyzer"
                            }
                        }
                    },

                    // Size
                    size = new { type = "keyword" },
                    sizeClean = new {
                        type = "text",
                        analyzer = "lowercase_analyzer",
                        fields = new {
                            ngram = new {
                                type = "text",
                                analyzer = "ngram_analyzer"
                            }
                        }
                    },

                    // Product details
                    packingStandard = new { type = "keyword" },
                    orderStandard = new { type = "keyword" },
                    ucgfea = new { type = "keyword" },
                    volume = new { type = "keyword" },
                    top = new { type = "keyword" },
                    weight = new { type = "float" },
                    hasAnalogue = new { type = "boolean" },
                    hasComponent = new { type = "boolean" },
                    hasImage = new { type = "boolean" },
                    image = new { type = "keyword" },
                    measureUnitId = new { type = "long" },

                    // Availability
                    available = new { type = "boolean" },
                    availableQtyUk = new { type = "float" },
                    availableQtyUkVat = new { type = "float" },
                    availableQtyPl = new { type = "float" },
                    availableQtyPlVat = new { type = "float" },
                    availableQty = new { type = "float" },

                    // Flags
                    isForWeb = new { type = "boolean" },
                    isForSale = new { type = "boolean" },
                    isForZeroSale = new { type = "boolean" },

                    // Slug
                    slugId = new { type = "long" },
                    slugNetUid = new { type = "keyword" },
                    slugUrl = new { type = "keyword" },
                    slugLocale = new { type = "keyword" },

                    updatedAt = new { type = "date" }
                }
            }
        };
    }
}
