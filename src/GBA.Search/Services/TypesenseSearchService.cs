using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GBA.Search.Configuration;
using GBA.Search.Models;
using GBA.Search.Services.Synonyms;
using GBA.Search.Sync;
using GBA.Search.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GBA.Search.Services;

public sealed partial class TypesenseSearchService : IProductSearchService, IProductSearchDebugService {
    private readonly HttpClient _http;
    private readonly TypesenseSettings _settings;
    private readonly SyncSettings _syncSettings;
    private readonly SearchTuningSettings _tuning;
    private readonly SearchTextProcessor _textProcessor;
    private readonly ISynonymProvider _synonyms;
    private readonly ILogger<TypesenseSearchService> _log;

    private DateTime _aliasResolvedAt;
    private string? _resolvedCollection;

    private const int MaxQueryLength = 500;

    private static readonly JsonSerializerOptions Json = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string IncludeFields = "id,mainOriginalNumberClean,vendorCodeClean,vendorCode,nameUA,name,descriptionUA,description,searchNameUA,searchName,originalNumbersClean,available,searchDescriptionUA,searchDescription,sizeClean";
    private const string IncludeFieldsFull = "id,netUid,vendorCode,vendorCodeClean,name,nameUA,description,descriptionUA,mainOriginalNumber,mainOriginalNumberClean,originalNumbers,originalNumbersClean,size,sizeClean,packingStandard,orderStandard,ucgfea,volume,top,weight,hasAnalogue,hasComponent,hasImage,image,measureUnitId,available,availableQtyUk,availableQtyUkVat,availableQtyPl,availableQtyPlVat,availableQty,isForWeb,isForSale,isForZeroSale,slugId,slugNetUid,slugUrl,slugLocale,searchNameUA,searchName,searchDescriptionUA,searchDescription";
    private const string SortBy = "_text_match:desc,available:desc,availableQtyUk:desc";
    private const string FilterBy = "isForWeb:=true";
    private const int AliasCacheSec = 10;

    [GeneratedRegex(@"\b([DdHhLlWwBb])=(\d+(?:[.,]\d+)?)\b", RegexOptions.Compiled)]
    private static partial Regex SizeSpecRegex();

    public TypesenseSearchService(
        HttpClient httpClient,
        IOptions<TypesenseSettings> settings,
        IOptions<SyncSettings> syncSettings,
        IOptions<SearchTuningSettings> tuning,
        SearchTextProcessor textProcessor,
        ISynonymProvider synonymProvider,
        ILogger<TypesenseSearchService> logger) {
        _http = httpClient;
        _settings = settings.Value;
        _syncSettings = syncSettings.Value;
        _tuning = tuning.Value;
        _textProcessor = textProcessor;
        _synonyms = synonymProvider;
        _log = logger;
    }

    public async Task<ProductSearchResult> SearchAsync(
        string query, string locale = "uk", int limit = 20, int offset = 0, CancellationToken ct = default) {
        if (string.IsNullOrWhiteSpace(query)) return ProductSearchResult.Empty;
        if (query.Length > MaxQueryLength)
            throw new ArgumentException($"Query exceeds maximum length of {MaxQueryLength} characters", nameof(query));

        limit = Math.Max(1, limit);
        offset = Math.Max(0, offset);

        try {
            var ctx = PrepareContext(query, locale);
            if (ctx.IsEmpty) return ProductSearchResult.Empty;

            var responses = await ExecuteAllPassesAsync(ctx, ct);
            var ranked = RankAndMerge(ctx.SearchQuery.AsSpan(), responses);
            var paged = Paginate(ranked, offset, limit);

            return new ProductSearchResult {
                ProductIds = paged,
                TotalCount = responses.Max(r => r?.Found ?? 0),
                SearchTimeMs = responses.Sum(r => r?.SearchTimeMs ?? 0),
                IsFallback = false
            };
        } catch (Exception ex) {
            _log.LogError(ex, "Search failed: {Query}", query);
            throw;
        }
    }

    public async Task<ProductSearchResultWithDocs> SearchWithDocsAsync(
        string query, string locale = "uk", int limit = 20, int offset = 0, CancellationToken ct = default) {
        if (string.IsNullOrWhiteSpace(query)) return ProductSearchResultWithDocs.Empty;
        if (query.Length > MaxQueryLength)
            throw new ArgumentException($"Query exceeds maximum length of {MaxQueryLength} characters", nameof(query));

        limit = Math.Max(1, limit);
        offset = Math.Max(0, offset);

        try {
            var ctx = PrepareContext(query, locale);
            if (ctx.IsEmpty) return ProductSearchResultWithDocs.Empty;

            var responses = await ExecuteAllPassesFullAsync(ctx, ct);
            var (ranked, docs) = RankAndMergeWithDocs(ctx.SearchQuery.AsSpan(), responses, locale);
            var pagedIds = Paginate(ranked, offset, limit);

            var pagedDocs = pagedIds
                .Where(id => docs.ContainsKey(id))
                .Select(id => docs[id])
                .ToList();

            return new ProductSearchResultWithDocs {
                Documents = pagedDocs,
                TotalCount = responses.Max(r => r?.Found ?? 0),
                SearchTimeMs = responses.Sum(r => r?.SearchTimeMs ?? 0),
                IsFallback = false
            };
        } catch (Exception ex) {
            _log.LogError(ex, "Search with docs failed: {Query}", query);
            throw;
        }
    }

    public async Task<ProductSearchDebugResult> SearchDebugAsync(
        string query, string locale = "uk", int limit = 20, int offset = 0, CancellationToken ct = default) {
        if (string.IsNullOrWhiteSpace(query))
            return new ProductSearchDebugResult { OriginalQuery = query ?? "" };
        if (query.Length > MaxQueryLength)
            throw new ArgumentException($"Query exceeds maximum length of {MaxQueryLength} characters", nameof(query));

        limit = Math.Max(1, limit);
        offset = Math.Max(0, offset);

        var ctx = PrepareContext(query, locale);
        if (ctx.IsEmpty)
            return new ProductSearchDebugResult { OriginalQuery = query, NormalizedQuery = ctx.NormalizedQuery };

        var collection = await ResolveCollectionAsync(ct);
        var exactResult = await ExecutePassAsync(ctx, PassType.Exact, collection, ct);

        var debug = new ProductSearchDebugResult {
            OriginalQuery = query,
            NormalizedQuery = ctx.NormalizedQuery,
            SynonymAppliedQuery = ctx.SearchQuery,
            QueryType = ctx.Type.ToString(),
            TargetCollection = collection,
            RequestedLimit = limit,
            RequestedOffset = offset,
            MergeLimit = _tuning.MaxMergeLimit,
            StemmingEnabled = _tuning.EnableStemming,
            StemmedQuery = ctx.StemmedQuery,
            ExactPass = BuildPassDebug(exactResult)
        };

        List<TypesenseResponse?> all = [exactResult.Response];

        if (ctx.HasStemPass) {
            var stem = await ExecutePassAsync(ctx, PassType.Stem, collection, ct);
            debug.StemPass = BuildPassDebug(stem);
            all.Add(stem.Response);
        }

        if (ctx.SizeSpecs.Count > 0)
            all.Add((await ExecutePassAsync(ctx, PassType.Size, collection, ct)).Response);

        if (ctx.Type != QType.PartNumber)
            all.Add((await ExecutePassAsync(ctx, PassType.Keywords, collection, ct)).Response);

        var ranked = RankAndMerge(ctx.SearchQuery.AsSpan(), all);
        debug.MergedIds = ranked;
        debug.PagedIds = Paginate(ranked, offset, limit);
        debug.TotalFoundMerged = ranked.Count;
        debug.TotalSearchTimeMs = all.Sum(r => r?.SearchTimeMs ?? 0);

        return debug;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default) {
        try {
            using var resp = await _http.GetAsync("health", ct);
            return resp.IsSuccessStatusCode;
        } catch {
            return false;
        }
    }

    private SearchContext PrepareContext(string query, string locale) {
        var (text, sizes) = ExtractSizeSpecs(query.AsSpan());
        var normalized = NumberNormalizer.NormalizeQuery(text);

        if (string.IsNullOrWhiteSpace(normalized) && sizes.Count == 0)
            return SearchContext.Empty;

        var qType = string.IsNullOrWhiteSpace(normalized) ? QType.Mixed : DetectType(normalized.AsSpan());
        var isUkr = qType == QType.ProductName && HasUkrainianChars(normalized.AsSpan());
        var searchQuery = isUkr ? normalized : _synonyms.Apply(normalized);
        var stemmed = _tuning.EnableStemming ? _textProcessor.StemText(searchQuery) : "";
        var hasStem = _tuning.EnableStemming && !string.IsNullOrWhiteSpace(stemmed) && stemmed != searchQuery;

        return new SearchContext {
            OriginalQuery = query,
            NormalizedQuery = normalized,
            SearchQuery = searchQuery,
            StemmedQuery = stemmed,
            Type = qType,
            Locale = locale,
            SizeSpecs = sizes,
            HasStemPass = hasStem,
            MergeLimit = _tuning.MaxMergeLimit
        };
    }

    private static (string text, List<string> sizes) ExtractSizeSpecs(ReadOnlySpan<char> query) {
        var str = query.ToString();
        var matches = SizeSpecRegex().Matches(str);
        if (matches.Count == 0) return (str, []);

        var sizes = new List<string>(matches.Count);
        var result = str;

        foreach (Match m in matches) {
            sizes.Add(m.Value.ToLowerInvariant().Replace("=", "").Replace(",", "."));
            result = result.Replace(m.Value, " ");
        }

        return (string.Join(' ', result.Split(' ', StringSplitOptions.RemoveEmptyEntries)), sizes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasUkrainianChars(ReadOnlySpan<char> s) {
        foreach (var c in s)
            if (c is 'і' or 'І' or 'ї' or 'Ї' or 'є' or 'Є' or 'ґ' or 'Ґ') return true;
        return false;
    }

    private static QType DetectType(ReadOnlySpan<char> query) {
        int cyr = 0, dig = 0, lat = 0, tot = 0;

        foreach (var c in query) {
            if (c == ' ') continue;
            tot++;
            if (char.IsAsciiDigit(c)) dig++;
            else if (IsCyrillic(c)) cyr++;
            else if (char.IsAsciiLetter(c)) lat++;
        }

        if (tot == 0) return QType.Mixed;

        var digR = (double)dig / tot;
        var cyrR = (double)cyr / tot;

        if (digR > 0.6) return QType.PartNumber;
        if (cyr > 0 && lat > 0) return QType.Mixed;
        if (cyrR > 0.7) return QType.ProductName;
        if (cyr > 0 && dig > 0) return QType.Mixed;
        if (lat > 0 && dig > 0) return QType.PartNumber;

        return QType.Mixed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsCyrillic(char c) =>
        c is (>= 'а' and <= 'я') or (>= 'А' and <= 'Я') or 'і' or 'І' or 'ї' or 'Ї' or 'є' or 'Є' or 'ґ' or 'Ґ';

    private async Task<List<TypesenseResponse?>> ExecuteAllPassesAsync(SearchContext ctx, CancellationToken ct) {
        var col = await ResolveCollectionAsync(ct);
        var tasks = new List<Task<ExecResult>> { ExecutePassAsync(ctx, PassType.Exact, col, ct) };

        if (ctx.HasStemPass) tasks.Add(ExecutePassAsync(ctx, PassType.Stem, col, ct));
        if (ctx.SizeSpecs.Count > 0) tasks.Add(ExecutePassAsync(ctx, PassType.Size, col, ct));
        if (ctx.Type != QType.PartNumber && !string.IsNullOrWhiteSpace(ctx.SearchQuery))
            tasks.Add(ExecutePassAsync(ctx, PassType.Keywords, col, ct));

        var results = await Task.WhenAll(tasks);
        return results.Select(r => r.Response).ToList();
    }

    private async Task<List<TypesenseResponse?>> ExecuteAllPassesFullAsync(SearchContext ctx, CancellationToken ct) {
        var col = await ResolveCollectionAsync(ct);
        var tasks = new List<Task<ExecResult>> { ExecutePassFullAsync(ctx, PassType.Exact, col, ct) };

        if (ctx.HasStemPass) tasks.Add(ExecutePassFullAsync(ctx, PassType.Stem, col, ct));
        if (ctx.SizeSpecs.Count > 0) tasks.Add(ExecutePassFullAsync(ctx, PassType.Size, col, ct));
        if (ctx.Type != QType.PartNumber && !string.IsNullOrWhiteSpace(ctx.SearchQuery))
            tasks.Add(ExecutePassFullAsync(ctx, PassType.Keywords, col, ct));

        var results = await Task.WhenAll(tasks);
        return results.Select(r => r.Response).ToList();
    }

    private async Task<ExecResult> ExecutePassAsync(SearchContext ctx, PassType pass, string col, CancellationToken ct) {
        var p = BuildParams(ctx, pass);
        var qs = string.Join("&", p.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        using var resp = await _http.GetAsync($"collections/{col}/documents/search?{qs}", ct);
        resp.EnsureSuccessStatusCode();

        var parsed = await resp.Content.ReadFromJsonAsync<TypesenseResponse>(Json, ct);
        return new ExecResult(col, parsed, p);
    }

    private async Task<ExecResult> ExecutePassFullAsync(SearchContext ctx, PassType pass, string col, CancellationToken ct) {
        var p = BuildParamsFull(ctx, pass);
        var qs = string.Join("&", p.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        using var resp = await _http.GetAsync($"collections/{col}/documents/search?{qs}", ct);
        resp.EnsureSuccessStatusCode();

        var parsed = await resp.Content.ReadFromJsonAsync<TypesenseResponse>(Json, ct);
        return new ExecResult(col, parsed, p);
    }

    private Dictionary<string, string> BuildParams(SearchContext ctx, PassType pass) => pass switch {
        PassType.Exact => BuildMainParams(ctx, false),
        PassType.Stem => BuildMainParams(ctx, true),
        PassType.Size => BuildSizeParams(ctx),
        PassType.Keywords => BuildKwParams(ctx),
        _ => throw new ArgumentOutOfRangeException(nameof(pass))
    };

    private Dictionary<string, string> BuildParamsFull(SearchContext ctx, PassType pass) {
        var p = BuildParams(ctx, pass);
        p["include_fields"] = IncludeFieldsFull;
        return p;
    }

    private Dictionary<string, string> BuildMainParams(SearchContext ctx, bool stem) {
        var q = stem ? ctx.StemmedQuery : ctx.SearchQuery;
        if (ctx.SizeSpecs.Count > 0 || ctx.Type == QType.Mixed) return BuildMixedParams(ctx, q, stem);
        return ctx.Type == QType.PartNumber ? BuildPartNumParams(q, ctx.MergeLimit) : BuildNameParams(ctx, q, stem);
    }

    private Dictionary<string, string> BuildPartNumParams(string q, int lim) => new() {
        ["q"] = q,
        ["query_by"] = "mainOriginalNumberClean,originalNumbersClean,vendorCodeClean,sizeClean",
        ["query_by_weights"] = "127,126,125,40",
        ["infix"] = "always,always,always,always",
        ["prefix"] = "true,true,true,true",
        ["num_typos"] = "0",
        ["per_page"] = lim.ToString(),
        ["page"] = "1",
        ["sort_by"] = SortBy,
        ["filter_by"] = FilterBy,
        ["include_fields"] = IncludeFields
    };

    private Dictionary<string, string> BuildNameParams(SearchContext ctx, string q, bool stem) {
        var (pri, sec) = ctx.Locale == "uk" ? ("nameUA", "name") : ("name", "nameUA");
        var (priSearch, secSearch) = ctx.Locale == "uk" ? ("searchNameUA", "searchName") : ("searchName", "searchNameUA");
        var qb = stem ? $"{pri}Stem,{sec}Stem,synonymsStem" : $"{priSearch},{secSearch},{pri},{sec},synonyms";

        return new Dictionary<string, string> {
            ["q"] = q,
            ["query_by"] = qb,
            ["query_by_weights"] = stem ? "110,90,60" : "130,125,127,100,80",
            ["prefix"] = stem ? "true,true,true" : "true,true,true,true,true",
            ["infix"] = stem ? "off,off,off" : "always,always,off,off,off",
            ["prioritize_exact_match"] = "true",
            ["token_order"] = _tuning.TokenOrder,
            ["num_typos"] = _tuning.NumTypos.ToString(),
            ["typo_tokens_threshold"] = _tuning.TypoTokensThreshold.ToString(),
            ["min_len_1typo"] = _tuning.MinLen1Typo.ToString(),
            ["min_len_2typo"] = _tuning.MinLen2Typo.ToString(),
            ["drop_tokens_threshold"] = _tuning.DropTokensThreshold.ToString(),
            ["per_page"] = ctx.MergeLimit.ToString(),
            ["page"] = "1",
            ["sort_by"] = SortBy,
            ["filter_by"] = FilterBy,
            ["include_fields"] = IncludeFields
        };
    }

    private Dictionary<string, string> BuildMixedParams(SearchContext ctx, string q, bool stem) {
        var (pn, sn) = ctx.Locale == "uk" ? ("nameUA", "name") : ("name", "nameUA");
        var (psn, ssn) = ctx.Locale == "uk" ? ("searchNameUA", "searchName") : ("searchName", "searchNameUA");
        var (pd, sd) = ctx.Locale == "uk" ? ("descriptionUA", "description") : ("description", "descriptionUA");
        var hasSize = ctx.SizeSpecs.Count > 0;
        var effQ = hasSize ? $"{q} {string.Join(' ', ctx.SizeSpecs)}".Trim() : q;

        var qb = stem
            ? $"{pn}Stem,{sn}Stem,synonymsStem,fullTextStem,{pd}Stem,{sd}Stem,sizeClean"
            : $"{psn},{ssn},{pn},{sn},synonyms,fullText,mainOriginalNumberClean,originalNumbersClean,vendorCodeClean,{pd},{sd},sizeClean";

        var w = stem
            ? (hasSize ? "110,90,60,100,20,15,150" : "110,90,60,100,20,15,30")
            : (hasSize ? "130,125,127,100,70,120,50,45,40,20,15,150" : "130,125,127,100,70,120,50,45,40,20,15,30");

        var pfx = stem ? "true,true,true,true,true,true,true" : "true,true,true,true,true,true,true,true,true,true,true,true";
        var inf = stem ? "off,off,off,off,off,off,always" : "always,always,off,off,off,off,always,always,always,off,off,always";
        var drop = hasSize ? Math.Max(_tuning.DropTokensThreshold, ctx.SizeSpecs.Count + 1) : _tuning.DropTokensThreshold;

        return new Dictionary<string, string> {
            ["q"] = effQ, ["query_by"] = qb, ["query_by_weights"] = w, ["prefix"] = pfx, ["infix"] = inf,
            ["prioritize_exact_match"] = "true", ["prioritize_token_position"] = "true",
            ["token_order"] = _tuning.TokenOrder, ["num_typos"] = _tuning.NumTypos.ToString(),
            ["typo_tokens_threshold"] = _tuning.TypoTokensThreshold.ToString(),
            ["min_len_1typo"] = _tuning.MinLen1Typo.ToString(), ["min_len_2typo"] = _tuning.MinLen2Typo.ToString(),
            ["drop_tokens_threshold"] = drop.ToString(), ["per_page"] = ctx.MergeLimit.ToString(), ["page"] = "1",
            ["sort_by"] = SortBy, ["filter_by"] = FilterBy, ["include_fields"] = IncludeFields
        };
    }

    private Dictionary<string, string> BuildSizeParams(SearchContext ctx) => new() {
        ["q"] = string.Join(' ', ctx.SizeSpecs), ["query_by"] = "sizeClean", ["query_by_weights"] = "100",
        ["infix"] = "always", ["prefix"] = "true", ["num_typos"] = "0",
        ["per_page"] = ctx.MergeLimit.ToString(), ["page"] = "1",
        ["sort_by"] = SortBy, ["filter_by"] = FilterBy, ["include_fields"] = IncludeFields
    };

    private Dictionary<string, string> BuildKwParams(SearchContext ctx) => new() {
        ["q"] = ctx.SearchQuery, ["query_by"] = "keywords", ["query_by_weights"] = "100",
        ["prefix"] = "true", ["num_typos"] = "1", ["drop_tokens_threshold"] = "0",
        ["per_page"] = ctx.MergeLimit.ToString(), ["page"] = "1",
        ["sort_by"] = SortBy, ["filter_by"] = FilterBy, ["include_fields"] = IncludeFields
    };

    private List<long> RankAndMerge(ReadOnlySpan<char> query, List<TypesenseResponse?> responses) {
        var unique = new Dictionary<string, Hit>(StringComparer.Ordinal);
        foreach (var r in responses) {
            if (r?.Hits == null) continue;
            foreach (var h in r.Hits)
                if (!string.IsNullOrEmpty(h.Document?.Id)) unique.TryAdd(h.Document.Id, h);
        }
        return RankProducts(unique.Values, query, _textProcessor);
    }

    private (List<long> ids, Dictionary<long, ProductSearchDocument> docs) RankAndMergeWithDocs(
        ReadOnlySpan<char> query, List<TypesenseResponse?> responses, string locale) {
        var unique = new Dictionary<string, Hit>(StringComparer.Ordinal);
        foreach (var r in responses) {
            if (r?.Hits == null) continue;
            foreach (var h in r.Hits)
                if (!string.IsNullOrEmpty(h.Document?.Id)) unique.TryAdd(h.Document.Id, h);
        }

        var ranked = RankProducts(unique.Values, query, _textProcessor);
        var docs = new Dictionary<long, ProductSearchDocument>();

        foreach (var hit in unique.Values) {
            if (hit.Document?.Id == null || !long.TryParse(hit.Document.Id, out var id)) continue;
            docs[id] = DocToSearchDocument(hit.Document, locale);
        }

        return (ranked, docs);
    }

    private static ProductSearchDocument DocToSearchDocument(Doc d, string locale) {
        var isUk = locale == "uk";
        return new ProductSearchDocument {
            Id = d.Id ?? "",
            NetUid = d.NetUid ?? "",
            VendorCode = d.VendorCode ?? "",
            Name = isUk ? (d.NameUA ?? d.Name ?? "") : (d.Name ?? d.NameUA ?? ""),
            NameUA = d.NameUA ?? "",
            Description = isUk ? (d.DescriptionUA ?? d.Description ?? "") : (d.Description ?? d.DescriptionUA ?? ""),
            DescriptionUA = d.DescriptionUA ?? "",
            MainOriginalNumber = d.MainOriginalNumber ?? "",
            OriginalNumbers = d.OriginalNumbers ?? [],
            Size = d.Size ?? "",
            SearchName = d.SearchName ?? "",
            SearchNameUA = d.SearchNameUA ?? "",
            SearchDescription = d.SearchDescription ?? "",
            SearchDescriptionUA = d.SearchDescriptionUA ?? "",
            PackingStandard = d.PackingStandard ?? "",
            OrderStandard = d.OrderStandard ?? "",
            Ucgfea = d.Ucgfea ?? "",
            Volume = d.Volume ?? "",
            Top = d.Top ?? "",
            Weight = d.Weight,
            HasAnalogue = d.HasAnalogue,
            HasComponent = d.HasComponent,
            HasImage = d.HasImage,
            Image = d.Image ?? "",
            MeasureUnitId = d.MeasureUnitId,
            Available = d.Available,
            AvailableQtyUk = d.AvailableQtyUk,
            AvailableQtyUkVat = d.AvailableQtyUkVat,
            AvailableQtyPl = d.AvailableQtyPl,
            AvailableQtyPlVat = d.AvailableQtyPlVat,
            AvailableQty = d.AvailableQty,
            IsForWeb = d.IsForWeb,
            IsForSale = d.IsForSale,
            IsForZeroSale = d.IsForZeroSale,
            SlugId = d.SlugId,
            SlugNetUid = d.SlugNetUid ?? "",
            SlugUrl = d.SlugUrl ?? "",
            SlugLocale = d.SlugLocale ?? ""
        };
    }

    private static List<long> RankProducts(IEnumerable<Hit> hits, ReadOnlySpan<char> querySpan, SearchTextProcessor textProcessor) {
        var query = querySpan.ToString().ToLowerInvariant();
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var stemmedTerms = textProcessor.StemText(query).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var ranked = new List<Ranked>();

        foreach (var h in hits) {
            if (h.Document?.Id == null || !long.TryParse(h.Document.Id, out var id)) continue;

            var f = new Fields(h.Document, textProcessor);
            if (!AllTermsPresent(f, terms, stemmedTerms)) continue;

            ranked.Add(new Ranked {
                Id = id,
                MainOrigExact = f.MainOrig.Span.SequenceEqual(query.AsSpan()) ? 1 : 0,
                HundredPct = ComputeHundredPct(f, query),
                Available = h.Document.Available ? 1 : 0,
                OrigMatch = HasAnyTerm(f.MainOrig.Span, terms) || HasOrigNumTerm(f.OrigNums, terms) ? 1 : 0,
                VendorMatch = HasAnyTerm(f.Vendor.Span, terms) ? 1 : 0,
                NameTermCount = CountTermsInName(f, terms, stemmedTerms),
                OrigOrName = HasAnyTerm(f.MainOrig.Span, terms) || HasAnyTerm(f.NameUA.Span, terms) || HasAnyTerm(f.Name.Span, terms) || HasOrigNumTerm(f.OrigNums, terms) ? 1 : 0,
                DescMatch = HasAnyTerm(f.DescUA.Span, terms) || HasAnyTerm(f.Desc.Span, terms) ? 1 : 0,
                SizeMatch = HasAnyTerm(f.Size.Span, terms) ? 1 : 0,
                SearchName = f.NameUA.ToString()
            });
        }

        return ranked
            .OrderByDescending(r => r.MainOrigExact)
            .ThenByDescending(r => r.HundredPct)
            .ThenByDescending(r => r.Available)
            .ThenByDescending(r => r.NameTermCount)
            .ThenByDescending(r => r.OrigMatch)
            .ThenByDescending(r => r.VendorMatch)
            .ThenByDescending(r => r.OrigOrName)
            .ThenByDescending(r => r.DescMatch)
            .ThenByDescending(r => r.SizeMatch)
            .ThenBy(r => r.SearchName)
            .ThenBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CountTermsInName(Fields f, string[] terms, string[] stemmedTerms) {
        int count = 0;
        for (int i = 0; i < terms.Length; i++) {
            var t = terms[i];
            var stemmed = i < stemmedTerms.Length ? stemmedTerms[i] : t;

            bool inName = f.SearchNameUA.Span.Contains(t.AsSpan(), StringComparison.Ordinal) ||
                          f.SearchName.Span.Contains(t.AsSpan(), StringComparison.Ordinal) ||
                          f.NameUA.Span.Contains(t.AsSpan(), StringComparison.Ordinal) ||
                          f.Name.Span.Contains(t.AsSpan(), StringComparison.Ordinal);

            if (!inName && !string.IsNullOrEmpty(stemmed) && stemmed != t) {
                inName = f.NameUAStem.Span.Contains(stemmed.AsSpan(), StringComparison.Ordinal) ||
                         f.NameStem.Span.Contains(stemmed.AsSpan(), StringComparison.Ordinal);
            }

            if (inName) count++;
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AllTermsPresent(Fields f, string[] terms, string[] stemmedTerms) {
        for (int i = 0; i < terms.Length; i++) {
            var t = terms[i];
            var ts = t.AsSpan();
            var stemmed = i < stemmedTerms.Length ? stemmedTerms[i] : t;
            var stemmedSpan = stemmed.AsSpan();

            bool found = f.MainOrig.Span.Contains(ts, StringComparison.Ordinal) ||
                         f.Vendor.Span.Contains(ts, StringComparison.Ordinal) ||
                         f.SearchNameUA.Span.Contains(ts, StringComparison.Ordinal) ||
                         f.SearchName.Span.Contains(ts, StringComparison.Ordinal) ||
                         f.NameUA.Span.Contains(ts, StringComparison.Ordinal) ||
                         f.Name.Span.Contains(ts, StringComparison.Ordinal) ||
                         f.SearchDescUA.Span.Contains(ts, StringComparison.Ordinal) ||
                         f.SearchDesc.Span.Contains(ts, StringComparison.Ordinal) ||
                         f.DescUA.Span.Contains(ts, StringComparison.Ordinal) ||
                         f.Desc.Span.Contains(ts, StringComparison.Ordinal) ||
                         f.Size.Span.Contains(ts, StringComparison.Ordinal) ||
                         HasOrigNumContains(f.OrigNums, t);

            if (!found && !string.IsNullOrEmpty(stemmed) && stemmed != t) {
                found = f.NameUAStem.Span.Contains(stemmedSpan, StringComparison.Ordinal) ||
                        f.NameStem.Span.Contains(stemmedSpan, StringComparison.Ordinal) ||
                        f.DescUAStem.Span.Contains(stemmedSpan, StringComparison.Ordinal) ||
                        f.DescStem.Span.Contains(stemmedSpan, StringComparison.Ordinal);
            }

            if (!found) return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasOrigNumContains(List<string> origNums, string term) {
        foreach (var n in origNums)
            if (n.AsSpan().Contains(term.AsSpan(), StringComparison.Ordinal)) return true;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasOrigNumTerm(List<string> origNums, string[] terms) {
        foreach (var n in origNums)
            foreach (var t in terms)
                if (n.AsSpan().Contains(t.AsSpan(), StringComparison.Ordinal)) return true;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ComputeHundredPct(Fields f, string q) {
        var qs = q.AsSpan();
        if (f.NameUA.Span.SequenceEqual(qs) || f.Vendor.Span.SequenceEqual(qs) || f.MainOrig.Span.SequenceEqual(qs))
            return 1;
        foreach (var n in f.OrigNums)
            if (n.AsSpan().SequenceEqual(qs)) return 1;
        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasAnyTerm(ReadOnlySpan<char> field, string[] terms) {
        foreach (var t in terms)
            if (field.Contains(t.AsSpan(), StringComparison.Ordinal)) return true;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static List<long> Paginate(List<long> ids, int off, int lim) {
        if (off <= 0 && ids.Count <= lim) return ids;
        var start = Math.Min(Math.Max(off, 0), ids.Count);
        var count = Math.Min(lim, Math.Max(0, ids.Count - start));
        return ids.GetRange(start, count);
    }

    private async Task<string> ResolveCollectionAsync(CancellationToken ct) {
        if (!_syncSettings.UseAliasSwap) return _settings.CollectionName;

        if (!string.IsNullOrWhiteSpace(_resolvedCollection) &&
            (DateTime.UtcNow - _aliasResolvedAt).TotalSeconds < AliasCacheSec)
            return _resolvedCollection!;

        using var resp = await _http.GetAsync($"aliases/{_settings.CollectionName}", ct);
        if (!resp.IsSuccessStatusCode) return _settings.CollectionName;

        var alias = await resp.Content.ReadFromJsonAsync<AliasResponse>(Json, ct);
        var resolved = string.IsNullOrWhiteSpace(alias?.CollectionName) ? _settings.CollectionName : alias.CollectionName;

        _resolvedCollection = resolved;
        _aliasResolvedAt = DateTime.UtcNow;
        return resolved;
    }

    private static DebugPassResult BuildPassDebug(ExecResult r) => new() {
        Query = r.Params.GetValueOrDefault("q", ""),
        Found = r.Response?.Found ?? 0,
        SearchTimeMs = r.Response?.SearchTimeMs ?? 0,
        Ids = r.Response?.Hits?.Select(h => long.TryParse(h.Document?.Id, out var id) ? id : 0).Where(id => id > 0).ToList() ?? [],
        QueryBy = r.Params.GetValueOrDefault("query_by", ""),
        Weights = r.Params.GetValueOrDefault("query_by_weights", ""),
        TokenOrder = r.Params.GetValueOrDefault("token_order", ""),
        NumTypos = r.Params.GetValueOrDefault("num_typos", ""),
        DropTokensThreshold = r.Params.GetValueOrDefault("drop_tokens_threshold", ""),
        FilterBy = r.Params.GetValueOrDefault("filter_by", ""),
        SortBy = r.Params.GetValueOrDefault("sort_by", "")
    };

    private enum QType { PartNumber, ProductName, Mixed }
    private enum PassType { Exact, Stem, Size, Keywords }

    private readonly struct SearchContext {
        public string OriginalQuery { get; init; }
        public string NormalizedQuery { get; init; }
        public string SearchQuery { get; init; }
        public string StemmedQuery { get; init; }
        public QType Type { get; init; }
        public string Locale { get; init; }
        public List<string> SizeSpecs { get; init; }
        public bool HasStemPass { get; init; }
        public int MergeLimit { get; init; }
        public bool IsEmpty => string.IsNullOrWhiteSpace(SearchQuery) && (SizeSpecs?.Count ?? 0) == 0;
        public static SearchContext Empty => new() { SizeSpecs = [], MergeLimit = 500, Locale = "uk" };
    }

    private readonly struct Fields {
        public ReadOnlyMemory<char> MainOrig { get; }
        public ReadOnlyMemory<char> Vendor { get; }
        public ReadOnlyMemory<char> NameUA { get; }
        public ReadOnlyMemory<char> Name { get; }
        public ReadOnlyMemory<char> DescUA { get; }
        public ReadOnlyMemory<char> Desc { get; }
        public ReadOnlyMemory<char> Size { get; }
        public ReadOnlyMemory<char> SearchNameUA { get; }
        public ReadOnlyMemory<char> SearchName { get; }
        public ReadOnlyMemory<char> SearchDescUA { get; }
        public ReadOnlyMemory<char> SearchDesc { get; }
        public ReadOnlyMemory<char> NameUAStem { get; }
        public ReadOnlyMemory<char> NameStem { get; }
        public ReadOnlyMemory<char> DescUAStem { get; }
        public ReadOnlyMemory<char> DescStem { get; }
        public List<string> OrigNums { get; }

        public Fields(Doc d, SearchTextProcessor textProcessor) {
            MainOrig = (d.MainOriginalNumberClean ?? "").ToLowerInvariant().AsMemory();
            Vendor = (d.VendorCodeClean ?? d.VendorCode ?? "").ToLowerInvariant().AsMemory();
            var nameUA = d.NameUA ?? "";
            var name = d.Name ?? "";
            var descUA = d.DescriptionUA ?? "";
            var desc = d.Description ?? "";
            var searchNameUA = d.SearchNameUA ?? "";
            var searchName = d.SearchName ?? "";
            var searchDescUA = d.SearchDescriptionUA ?? "";
            var searchDesc = d.SearchDescription ?? "";
            NameUA = nameUA.ToLowerInvariant().AsMemory();
            Name = name.ToLowerInvariant().AsMemory();
            DescUA = descUA.ToLowerInvariant().AsMemory();
            Desc = desc.ToLowerInvariant().AsMemory();
            SearchNameUA = searchNameUA.ToLowerInvariant().AsMemory();
            SearchName = searchName.ToLowerInvariant().AsMemory();
            SearchDescUA = searchDescUA.ToLowerInvariant().AsMemory();
            SearchDesc = searchDesc.ToLowerInvariant().AsMemory();
            Size = (d.SizeClean ?? "").ToLowerInvariant().AsMemory();
            NameUAStem = textProcessor.StemText(nameUA).AsMemory();
            NameStem = textProcessor.StemText(name).AsMemory();
            DescUAStem = textProcessor.StemText(descUA).AsMemory();
            DescStem = textProcessor.StemText(desc).AsMemory();
            OrigNums = d.OriginalNumbersClean?.Select(n => n.ToLowerInvariant()).ToList() ?? [];
        }
    }

    private sealed class Ranked {
        public long Id { get; init; }
        public int MainOrigExact { get; init; }
        public int HundredPct { get; init; }
        public int Available { get; init; }
        public int NameTermCount { get; init; }
        public int OrigMatch { get; init; }
        public int VendorMatch { get; init; }
        public int OrigOrName { get; init; }
        public int DescMatch { get; init; }
        public int SizeMatch { get; init; }
        public string SearchName { get; init; } = "";
    }

    private sealed record ExecResult(string Collection, TypesenseResponse? Response, Dictionary<string, string> Params);

    private sealed class TypesenseResponse {
        [JsonPropertyName("found")] public int Found { get; set; }
        [JsonPropertyName("hits")] public List<Hit>? Hits { get; set; }
        [JsonPropertyName("search_time_ms")] public int SearchTimeMs { get; set; }
    }

    private sealed class Hit {
        [JsonPropertyName("document")] public Doc? Document { get; set; }
        [JsonPropertyName("text_match")] public long TextMatch { get; set; }
    }

    private sealed class Doc {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("netUid")] public string? NetUid { get; set; }
        [JsonPropertyName("vendorCode")] public string? VendorCode { get; set; }
        [JsonPropertyName("vendorCodeClean")] public string? VendorCodeClean { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("nameUA")] public string? NameUA { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("descriptionUA")] public string? DescriptionUA { get; set; }
        [JsonPropertyName("mainOriginalNumber")] public string? MainOriginalNumber { get; set; }
        [JsonPropertyName("mainOriginalNumberClean")] public string? MainOriginalNumberClean { get; set; }
        [JsonPropertyName("originalNumbers")] public List<string>? OriginalNumbers { get; set; }
        [JsonPropertyName("originalNumbersClean")] public List<string>? OriginalNumbersClean { get; set; }
        [JsonPropertyName("size")] public string? Size { get; set; }
        [JsonPropertyName("sizeClean")] public string? SizeClean { get; set; }
        [JsonPropertyName("searchNameUA")] public string? SearchNameUA { get; set; }
        [JsonPropertyName("searchName")] public string? SearchName { get; set; }
        [JsonPropertyName("searchDescriptionUA")] public string? SearchDescriptionUA { get; set; }
        [JsonPropertyName("searchDescription")] public string? SearchDescription { get; set; }
        // Product details
        [JsonPropertyName("packingStandard")] public string? PackingStandard { get; set; }
        [JsonPropertyName("orderStandard")] public string? OrderStandard { get; set; }
        [JsonPropertyName("ucgfea")] public string? Ucgfea { get; set; }
        [JsonPropertyName("volume")] public string? Volume { get; set; }
        [JsonPropertyName("top")] public string? Top { get; set; }
        [JsonPropertyName("weight")] public double Weight { get; set; }
        [JsonPropertyName("hasAnalogue")] public bool HasAnalogue { get; set; }
        [JsonPropertyName("hasComponent")] public bool HasComponent { get; set; }
        [JsonPropertyName("hasImage")] public bool HasImage { get; set; }
        [JsonPropertyName("image")] public string? Image { get; set; }
        [JsonPropertyName("measureUnitId")] public long MeasureUnitId { get; set; }
        // Availability
        [JsonPropertyName("available")] public bool Available { get; set; }
        [JsonPropertyName("availableQtyUk")] public double AvailableQtyUk { get; set; }
        [JsonPropertyName("availableQtyUkVat")] public double AvailableQtyUkVat { get; set; }
        [JsonPropertyName("availableQtyPl")] public double AvailableQtyPl { get; set; }
        [JsonPropertyName("availableQtyPlVat")] public double AvailableQtyPlVat { get; set; }
        [JsonPropertyName("availableQty")] public double AvailableQty { get; set; }
        // Flags
        [JsonPropertyName("isForWeb")] public bool IsForWeb { get; set; }
        [JsonPropertyName("isForSale")] public bool IsForSale { get; set; }
        [JsonPropertyName("isForZeroSale")] public bool IsForZeroSale { get; set; }
        // Slug
        [JsonPropertyName("slugId")] public long SlugId { get; set; }
        [JsonPropertyName("slugNetUid")] public string? SlugNetUid { get; set; }
        [JsonPropertyName("slugUrl")] public string? SlugUrl { get; set; }
        [JsonPropertyName("slugLocale")] public string? SlugLocale { get; set; }
    }

    private sealed class AliasResponse {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("collection_name")] public string? CollectionName { get; set; }
    }
}
