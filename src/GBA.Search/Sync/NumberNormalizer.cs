using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GBA.Search.Sync;

public static partial class NumberNormalizer {
    private static readonly Dictionary<char, char> LookalikeToLatinMap = new() {
        { 'а', 'a' },
        { 'в', 'b' },
        { 'е', 'e' },
        { 'і', 'i' },
        { 'к', 'k' },
        { 'м', 'm' },
        { 'н', 'h' },
        { 'о', 'o' },
        { 'р', 'p' },
        { 'с', 'c' },
        { 'т', 't' },
        { 'у', 'y' },
        { 'х', 'x' }
    };

    public static IEnumerable<string> LookalikeCharFilterMappings() {
        foreach ((char cyrillic, char latin) in LookalikeToLatinMap) {
            yield return $"{cyrillic} => {latin}";
            yield return $"{char.ToUpperInvariant(cyrillic)} => {latin}";
        }
    }

    public static string FoldLookalikes(string value) {
        if (string.IsNullOrEmpty(value)) {
            return string.Empty;
        }

        StringBuilder result = new(value.Length);
        foreach (char c in value) {
            char lower = char.ToLowerInvariant(c);
            result.Append(LookalikeToLatinMap.TryGetValue(lower, out char latin) ? latin : lower);
        }

        return result.ToString();
    }

    [GeneratedRegex("[^a-zA-Z0-9а-яА-ЯіІїЇєЄґҐ]", RegexOptions.Compiled)]
    private static partial Regex NonAlphanumericRegex();

    public static string Normalize(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return string.Empty;
        }

        if (!ContainsSpecialChars(value)) {
            return FoldLookalikes(value);
        }

        string result = NonAlphanumericRegex().Replace(value, string.Empty);
        return FoldLookalikes(result);
    }

    public static string NormalizeQuery(string? query) {
        if (string.IsNullOrWhiteSpace(query)) {
            return string.Empty;
        }

        StringBuilder result = new(query.Length);
        bool lastWasSpace = true;
        bool isStartOfCyrillicWord = true;

        foreach (char c in query) {
            if (char.IsWhiteSpace(c)) {
                if (!lastWasSpace && result.Length > 0) {
                    result.Append(' ');
                    lastWasSpace = true;
                    isStartOfCyrillicWord = true;
                }
            } else if (char.IsDigit(c)) {
                result.Append(c);
                lastWasSpace = false;
                isStartOfCyrillicWord = false;
            } else if (char.IsLetter(c)) {
                if (IsCyrillicChar(c) && isStartOfCyrillicWord) {
                    result.Append(char.ToUpperInvariant(c));
                } else {
                    result.Append(char.ToLowerInvariant(c));
                }
                isStartOfCyrillicWord = false;
                lastWasSpace = false;
            }
        }

        if (result.Length > 0 && result[^1] == ' ') {
            result.Length--;
        }

        string[] tokens = result.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < tokens.Length; i++) {
            if (tokens[i].Any(char.IsDigit)) {
                tokens[i] = FoldLookalikes(tokens[i]);
            }
        }

        return string.Join(' ', tokens);
    }

    private static bool IsCyrillicChar(char c) {
        return c is >= 'а' and <= 'я' || (c >= 'А' && c <= 'Я') ||
               c == 'і' || c == 'І' || c == 'ї' || c == 'Ї' ||
               c == 'є' || c == 'Є' || c == 'ґ' || c == 'Ґ';
    }

    private static bool ContainsSpecialChars(string value) {
        return value.Any(c => !char.IsLetterOrDigit(c));
    }
}
