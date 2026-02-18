using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using GBA.Search.Configuration;
using GBA.Search.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GBA.Search.Services.Synonyms;

public sealed class FileSynonymProvider(
    IOptionsMonitor<SearchSynonymsSettings> settings,
    SearchTextProcessor textProcessor,
    IHostEnvironment hostEnvironment,
    ILogger<FileSynonymProvider> logger)
    : ISynonymProvider, IDisposable {
    private readonly ReaderWriterLockSlim _lock = new();
    private DateTime _lastLoadUtc = DateTime.MinValue;
    private DateTime _lastFileWriteUtc = DateTime.MinValue;
    private Dictionary<string, string> _map = new(StringComparer.Ordinal);
    private int _maxPhraseLen = 1;

    public string Apply(string normalizedQuery) {
        if (string.IsNullOrWhiteSpace(normalizedQuery)) {
            return normalizedQuery;
        }

        EnsureLoaded();

        _lock.EnterReadLock();
        try {
            if (_map.Count == 0) {
                return normalizedQuery;
            }

            IReadOnlyList<string> tokens = textProcessor.Tokenize(normalizedQuery);
            if (tokens.Count == 0) {
                return normalizedQuery;
            }

            List<string> output = new List<string>(tokens.Count);
            int i = 0;
            while (i < tokens.Count) {
                bool matched = false;
                int maxLen = Math.Min(_maxPhraseLen, tokens.Count - i);

                for (int len = maxLen; len >= 1; len--) {
                    string key = string.Join(' ', tokens.Skip(i).Take(len));
                    if (_map.TryGetValue(key, out string? canonical)) {
                        output.AddRange(canonical.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                        i += len;
                        matched = true;
                        break;
                    }
                }

                if (!matched) {
                    output.Add(tokens[i]);
                    i++;
                }
            }

            return string.Join(' ', output);
        } finally {
            _lock.ExitReadLock();
        }
    }

    private void EnsureLoaded() {
        SearchSynonymsSettings settings1 = settings.CurrentValue;
        if (!settings1.Enabled) {
            return;
        }

        DateTime now = DateTime.UtcNow;

        _lock.EnterReadLock();
        try {
            if ((now - _lastLoadUtc).TotalSeconds < settings1.ReloadIntervalSeconds) {
                return;
            }
        } finally {
            _lock.ExitReadLock();
        }

        _lock.EnterWriteLock();
        try {
            if ((DateTime.UtcNow - _lastLoadUtc).TotalSeconds < settings1.ReloadIntervalSeconds) {
                return;
            }

            _lastLoadUtc = DateTime.UtcNow;
            TryReload(settings1);
        } finally {
            _lock.ExitWriteLock();
        }
    }

    private void TryReload(SearchSynonymsSettings settings) {
        try {
            string path = ResolvePath(settings.FilePath);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) {
                _map = new Dictionary<string, string>(StringComparer.Ordinal);
                _maxPhraseLen = 1;
                return;
            }

            FileInfo fileInfo = new FileInfo(path);
            if (fileInfo.LastWriteTimeUtc <= _lastFileWriteUtc && _map.Count > 0) {
                return;
            }

            string content = ReadAllTextWithBom(path);
            Dictionary<string, string> map = Parse(content);

            _map = map;
            _maxPhraseLen = GetMaxPhraseLen(map.Keys);
            _lastFileWriteUtc = fileInfo.LastWriteTimeUtc;

            logger.LogInformation("Loaded {Count} synonym mappings from {Path}", _map.Count, path);
        } catch (Exception ex) {
            logger.LogWarning(ex, "Failed to load synonym file");
        }
    }

    private string ResolvePath(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return string.Empty;
        }

        if (Path.IsPathRooted(path)) {
            return path;
        }

        return Path.Combine(hostEnvironment.ContentRootPath, path);
    }

    private Dictionary<string, string> Parse(string content) {
        Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(content)) {
            return map;
        }

        string[] lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (string rawLine in lines) {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("//")) {
                continue;
            }

            string canonical;
            IEnumerable<string> variants;

            if (line.Contains("=>")) {
                string[] parts = line.Split("=>", 2, StringSplitOptions.TrimEntries);
                canonical = parts[0];
                variants = SplitVariants(parts[1]);
            } else if (line.Contains('=')) {
                string[] parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
                canonical = parts[0];
                variants = SplitVariants(parts[1]);
            } else {
                // Format: "russian,ukrainian" - use LAST item (Ukrainian) as canonical
                // This way Russian searches get converted to Ukrainian for better UA product matching
                List<string> tokens = SplitVariants(line).ToList();
                if (tokens.Count == 0) {
                    continue;
                }
                canonical = tokens[^1]; // Use last item (Ukrainian) as canonical
                variants = tokens;
            }

            string canonicalNormalized = NormalizePhrase(canonical);
            if (string.IsNullOrWhiteSpace(canonicalNormalized)) {
                continue;
            }

            map[canonicalNormalized] = canonicalNormalized;

            foreach (string variant in variants) {
                string variantNormalized = NormalizePhrase(variant);
                if (string.IsNullOrWhiteSpace(variantNormalized)) {
                    continue;
                }
                map[variantNormalized] = canonicalNormalized;
            }
        }

        return map;
    }

    private IEnumerable<string> SplitVariants(string input) {
        return input.Split(['|', ',', ';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim())
            .Where(v => v.Length > 0);
    }

    private string NormalizePhrase(string phrase) {
        return textProcessor.NormalizeText(phrase);
    }

    private static int GetMaxPhraseLen(IEnumerable<string> keys) {
        int max = 1;
        foreach (string key in keys) {
            int len = key.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (len > max) {
                max = len;
            }
        }
        return max;
    }

    private static string ReadAllTextWithBom(string path) {
        using FileStream stream = File.OpenRead(path);
        Span<byte> bom = stackalloc byte[4];
        int read = stream.Read(bom);
        stream.Position = 0;

        Encoding encoding = Encoding.UTF8;
        if (read >= 2) {
            if (bom[0] == 0xFF && bom[1] == 0xFE) {
                encoding = Encoding.Unicode;
            } else if (bom[0] == 0xFE && bom[1] == 0xFF) {
                encoding = Encoding.BigEndianUnicode;
            }
        }
        if (read >= 3) {
            if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF) {
                encoding = Encoding.UTF8;
            }
        }

        using StreamReader reader = new StreamReader(stream, encoding, true);
        return reader.ReadToEnd();
    }

    public void Dispose() {
        _lock.Dispose();
    }
}
