using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GBA.Search.Text;

public sealed class SearchTextProcessor {
    private static readonly Regex TokenRegex = new(@"[\p{L}\p{Nd}]+", RegexOptions.Compiled);
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal) {
        "і","й","та","але","або","у","в","на","для","з","із","до","по","при","без","над","під","що","є",
        "и","й","или","а","но","в","во","на","для","с","со","к","по","при","без","над","под","что","это"
    };
    private readonly UkrainianLightStemmer _uaStemmer = new();

    public string NormalizeText(string? input) {
        if (string.IsNullOrWhiteSpace(input)) {
            return string.Empty;
        }

        StringBuilder sb = new StringBuilder(input.Length);
        bool lastWasSpace = false;

        foreach (char c in input) {
            if (char.IsLetterOrDigit(c)) {
                char normalized = NormalizeChar(c);
                sb.Append(char.ToLowerInvariant(normalized));
                lastWasSpace = false;
                continue;
            }

            if (!lastWasSpace) {
                sb.Append(' ');
                lastWasSpace = true;
            }
        }

        return sb.ToString().Trim();
    }

    public IReadOnlyList<string> Tokenize(string? input) {
        if (string.IsNullOrWhiteSpace(input)) {
            return [];
        }

        string normalized = NormalizeText(input);
        if (string.IsNullOrWhiteSpace(normalized)) {
            return [];
        }

        List<string> tokens = [];
        foreach (Match match in TokenRegex.Matches(normalized)) {
            if (!string.IsNullOrWhiteSpace(match.Value)) {
                tokens.Add(match.Value);
            }
        }

        return tokens;
    }

    public string StemText(string? input) {
        IReadOnlyList<string> tokens = Tokenize(input);
        if (tokens.Count == 0) {
            return string.Empty;
        }

        List<string> stemmed = new List<string>(tokens.Count);
        foreach (string token in tokens) {
            string stem = StemToken(token);
            if (!string.IsNullOrWhiteSpace(stem) && !StopWords.Contains(stem)) {
                stemmed.Add(stem);
            }
        }

        return string.Join(' ', stemmed.Where(t => !string.IsNullOrWhiteSpace(t)));
    }

    private static char NormalizeChar(char c) {
        return c switch {
            'ё' => 'е',
            'Ё' => 'е',
            _ => c
        };
    }

    private string StemToken(string token) {
        if (string.IsNullOrWhiteSpace(token)) {
            return string.Empty;
        }

        if (token.Length < 3) {
            return token;
        }

        if (token.Any(char.IsDigit)) {
            return token;
        }

        if (IsCyrillic(token)) {
            return _uaStemmer.Stem(token);
        }

        return token;
    }

    private static bool IsCyrillic(string token) {
        foreach (char c in token) {
            if (c >= 'а' && c <= 'я' || c >= 'А' && c <= 'Я' ||
                c == 'і' || c == 'І' || c == 'ї' || c == 'Ї' ||
                c == 'є' || c == 'Є' || c == 'ґ' || c == 'Ґ') {
                return true;
            }
        }
        return false;
    }
}

internal sealed class UkrainianLightStemmer {
    private static readonly string[] Suffixes = [
        "ями", "ами", "ові", "еві", "ими", "ого", "ому", "ією", "ість",
        "ею", "єю", "ою", "ів", "їв", "ях", "ах", "ий", "ій", "ої", "их", "им", "ім", "ти", "ть", "ла", "ло", "ли",
        "а", "я", "о", "е", "и", "і", "ї", "й", "ь", "у", "ю"
    ];

    public string Stem(string token) {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 4) {
            return token;
        }

        foreach (string suffix in Suffixes) {
            if (token.EndsWith(suffix, StringComparison.Ordinal)) {
                int newLen = token.Length - suffix.Length;
                if (newLen >= 2) {
                    return token[..newLen];
                }
            }
        }

        return token;
    }
}
