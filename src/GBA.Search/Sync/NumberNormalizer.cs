using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GBA.Search.Sync;

public static partial class NumberNormalizer {
    private static readonly Dictionary<char, char> CyrillicToLatinMap = new() {
        { 'а', 'a' },
        { 'е', 'e' },
        { 'і', 'i' },
        { 'о', 'o' },
        { 'р', 'p' },
        { 'с', 'c' },
        { 'у', 'y' },
        { 'х', 'x' },
        { 'к', 'k' },
        { 'м', 'm' },
        { 'н', 'n' },
        { 'т', 't' },
        { 'А', 'a' },
        { 'Е', 'e' },
        { 'І', 'i' },
        { 'О', 'o' },
        { 'Р', 'p' },
        { 'С', 'c' },
        { 'У', 'y' },
        { 'Х', 'x' },
        { 'К', 'k' },
        { 'М', 'm' },
        { 'Н', 'n' },
        { 'Т', 't' }
    };

    [GeneratedRegex("[^a-zA-Z0-9а-яА-ЯіІїЇєЄґҐ]", RegexOptions.Compiled)]
    private static partial Regex NonAlphanumericRegex();

    public static string Normalize(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return string.Empty;
        }

        if (!ContainsSpecialChars(value)) {
            return value.ToLowerInvariant();
        }

        string result = NonAlphanumericRegex().Replace(value, string.Empty);
        return result.ToLowerInvariant();
    }

    public static string NormalizeQuery(string? query) {
        if (string.IsNullOrWhiteSpace(query)) {
            return string.Empty;
        }

        StringBuilder result = new(query.Length);
        bool lastWasSpace = true;
        bool inNumber = false;
        bool isStartOfCyrillicWord = true;

        foreach (char c in query) {
            if (c == ' ') {
                if (!lastWasSpace && result.Length > 0) {
                    result.Append(' ');
                    lastWasSpace = true;
                    isStartOfCyrillicWord = true;
                }
                inNumber = false;
            } else if (char.IsDigit(c)) {
                result.Append(c);
                lastWasSpace = false;
                isStartOfCyrillicWord = false;
                inNumber = true;
            } else if (char.IsLetter(c)) {
                bool isCyrillic = IsCyrillicChar(c);

                if (CyrillicToLatinMap.TryGetValue(c, out char latinChar)) {
                    if (inNumber || IsLikelyPartNumber(result)) {
                        result.Append(latinChar);
                        isStartOfCyrillicWord = false;
                    } else if (isCyrillic) {
                        if (isStartOfCyrillicWord) {
                            result.Append(char.ToUpperInvariant(c));
                            isStartOfCyrillicWord = false;
                        } else {
                            result.Append(char.ToLowerInvariant(c));
                        }
                    } else {
                        result.Append(char.ToLowerInvariant(c));
                        isStartOfCyrillicWord = false;
                    }
                } else if (isCyrillic) {
                    if (isStartOfCyrillicWord) {
                        result.Append(char.ToUpperInvariant(c));
                        isStartOfCyrillicWord = false;
                    } else {
                        result.Append(char.ToLowerInvariant(c));
                    }
                } else {
                    result.Append(char.ToLowerInvariant(c));
                    isStartOfCyrillicWord = false;
                }
                lastWasSpace = false;
            }
        }

        if (result.Length > 0 && result[^1] == ' ') {
            result.Length--;
        }

        return result.ToString();
    }

    private static bool IsCyrillicChar(char c) {
        return c is >= 'а' and <= 'я' || (c >= 'А' && c <= 'Я') ||
               c == 'і' || c == 'І' || c == 'ї' || c == 'Ї' ||
               c == 'є' || c == 'Є' || c == 'ґ' || c == 'Ґ';
    }

    private static bool IsLikelyPartNumber(StringBuilder sb) {
        for (int i = sb.Length - 1; i >= 0 && i >= sb.Length - 10; i--) {
            if (sb[i] == ' ') break;
            if (char.IsDigit(sb[i])) return true;
        }
        return false;
    }

    private static bool ContainsSpecialChars(string value) {
        return value.Any(c => !char.IsLetterOrDigit(c));
    }
}
