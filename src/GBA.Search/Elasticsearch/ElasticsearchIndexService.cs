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
}

public sealed class ElasticsearchIndexService : IElasticsearchIndexService {
    private readonly HttpClient _http;
    private readonly ElasticsearchSettings _settings;
    private readonly ILogger<ElasticsearchIndexService> _log;

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
            return response.IsSuccessStatusCode;
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
            .Select(e => e.GetProperty("index").GetString())
            .Where(name => !string.IsNullOrEmpty(name))
            .OrderByDescending(name => name, StringComparer.Ordinal)
            .ToList()!;

        int deleted = 0;
        foreach (string old in indices.Skip(Math.Max(0, keep))) {
            HttpResponseMessage del = await _http.DeleteAsync(old, ct);
            if (del.IsSuccessStatusCode) {
                deleted++;
                _log.LogInformation("Deleted old index {Index}", old);
            }
        }
        return deleted;
    }

    private static object BuildIndexSettings() {
        return new {
            settings = new {
                number_of_shards = 1,
                number_of_replicas = 0,
                max_ngram_diff = 15,
                analysis = new {
                    analyzer = new {
                        // Ngram analyzer for substring matching (LIKE '%term%')
                        ngram_analyzer = new {
                            type = "custom",
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
