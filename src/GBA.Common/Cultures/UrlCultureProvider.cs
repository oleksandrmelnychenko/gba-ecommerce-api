using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace GBA.Common.Cultures;

public class UrlCultureProvider : IRequestCultureProvider {
    private readonly string _defaultCultureName;
    private readonly ProviderCultureResult _defaultResult;

    // Cache CultureInfo objects to avoid repeated allocations
    private static readonly FrozenDictionary<string, CultureInfo> CultureCache = new Dictionary<string, CultureInfo> {
        ["uk"] = new CultureInfo("uk"),
        ["en"] = new CultureInfo("en")
    }.ToFrozenDictionary();

    // Cache ProviderCultureResult objects
    private static readonly FrozenDictionary<string, ProviderCultureResult> ResultCache = new Dictionary<string, ProviderCultureResult> {
        ["uk"] = new ProviderCultureResult("uk", "uk"),
        ["en"] = new ProviderCultureResult("en", "en")
    }.ToFrozenDictionary();

    public UrlCultureProvider(RequestCulture requestCulture) {
        _defaultCultureName = requestCulture.Culture.TwoLetterISOLanguageName;
        _defaultResult = new ProviderCultureResult(_defaultCultureName);
    }

    public Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext) {
        ReadOnlySpan<char> path = httpContext.Request.Path.Value.AsSpan();

        if (path.Length <= 1)
            return Task.FromResult(_defaultResult);

        // Find the culture segment (4th part after /api/v1/{culture}/...)
        int slashCount = 0;
        int segmentStart = 0;
        int segmentEnd = 0;

        for (int i = 0; i < path.Length; i++) {
            if (path[i] == '/') {
                slashCount++;
                if (slashCount == 3) segmentStart = i + 1;
                else if (slashCount == 4) { segmentEnd = i; break; }
            }
        }

        if (slashCount < 3)
            return Task.FromResult(_defaultResult);

        if (segmentEnd == 0) segmentEnd = path.Length;

        ReadOnlySpan<char> cultureSegment = path[segmentStart..segmentEnd];

        // Fast path for known cultures
        string cultureName;
        if (cultureSegment.SequenceEqual("uk")) cultureName = "uk";
        else if (cultureSegment.SequenceEqual("en")) cultureName = "en";
        else
            cultureName = _defaultCultureName;

        // Use cached CultureInfo if available
        if (CultureCache.TryGetValue(cultureName, out CultureInfo cachedCulture)) {
            CultureInfo.CurrentCulture = cachedCulture;
            CultureInfo.CurrentUICulture = cachedCulture;
        } else {
            CultureInfo culture = new(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        // Use cached result if available
        return Task.FromResult(
            ResultCache.TryGetValue(cultureName, out ProviderCultureResult cachedResult)
                ? cachedResult
                : new ProviderCultureResult(cultureName, cultureName));
    }
}
