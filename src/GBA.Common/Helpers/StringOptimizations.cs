using System;

namespace GBA.Common.Helpers;

/// <summary>
/// High-performance string manipulation utilities using Span for reduced allocations.
/// </summary>
public static class StringOptimizations {
    /// <summary>
    /// Removes Polish diacritical characters in a single pass using Span.
    /// Replaces: ą→a, ć→c, ę→e, ł→l, ń→n, ó→o, ś→s, ź→z, ż→z
    /// Also handles uppercase variants.
    /// </summary>
    /// <param name="input">The input string to process</param>
    /// <returns>A new string with Polish diacritics replaced</returns>
    public static string RemovePolishDiacritics(string? input) {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return RemovePolishDiacritics(input.AsSpan());
    }

    /// <summary>
    /// Removes Polish diacritical characters in a single pass using Span.
    /// Uses stackalloc for small strings (≤256 chars) to avoid heap allocation.
    /// </summary>
    /// <param name="input">The input span to process</param>
    /// <returns>A new string with Polish diacritics replaced</returns>
    public static string RemovePolishDiacritics(ReadOnlySpan<char> input) {
        if (input.IsEmpty)
            return string.Empty;

        // Use stackalloc for small strings to avoid heap allocation
        Span<char> buffer = input.Length <= 256
            ? stackalloc char[input.Length]
            : new char[input.Length];

        for (int i = 0; i < input.Length; i++) buffer[i] = ReplaceDiacritic(input[i]);

        return new string(buffer);
    }

    /// <summary>
    /// Normalizes a string for search: trims, lowercases, and removes Polish diacritics.
    /// Performs all operations in a single pass to minimize allocations.
    /// </summary>
    /// <param name="input">The input string to normalize</param>
    /// <returns>A normalized string suitable for search operations</returns>
    public static string NormalizeForSearch(string? input) {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        ReadOnlySpan<char> trimmed = input.AsSpan().Trim();
        if (trimmed.IsEmpty)
            return string.Empty;

        Span<char> buffer = trimmed.Length <= 256
            ? stackalloc char[trimmed.Length]
            : new char[trimmed.Length];

        for (int i = 0; i < trimmed.Length; i++) {
            char c = char.ToLowerInvariant(trimmed[i]);
            buffer[i] = ReplaceDiacritic(c);
        }

        return new string(buffer);
    }

    /// <summary>
    /// Replaces a single Polish diacritical character with its ASCII equivalent.
    /// </summary>
    private static char ReplaceDiacritic(char c) {
        return c switch {
            'ą' or 'Ą' => 'a',
            'ć' or 'Ć' => 'c',
            'ę' or 'Ę' => 'e',
            'ł' or 'Ł' => 'l',
            'ń' or 'Ń' => 'n',
            'ó' or 'Ó' => 'o',
            'ś' or 'Ś' => 's',
            'ź' or 'Ź' or 'ż' or 'Ż' => 'z',
            _ => c
        };
    }

    /// <summary>
    /// Gets a specific field from a delimited string without allocating a string array.
    /// Uses Span for zero-allocation parsing.
    /// </summary>
    /// <param name="input">The delimited string to parse</param>
    /// <param name="delimiter">The delimiter character (e.g., ',')</param>
    /// <param name="fieldIndex">The zero-based index of the field to retrieve</param>
    /// <returns>The field value as a ReadOnlySpan, or empty if index is out of bounds</returns>
    public static ReadOnlySpan<char> GetField(ReadOnlySpan<char> input, char delimiter, int fieldIndex) {
        int currentIndex = 0;
        int startPos = 0;

        for (int i = 0; i <= input.Length; i++)
            if (i == input.Length || input[i] == delimiter) {
                if (currentIndex == fieldIndex) return input.Slice(startPos, i - startPos);
                currentIndex++;
                startPos = i + 1;
            }

        return ReadOnlySpan<char>.Empty;
    }

    /// <summary>
    /// Gets a specific field from a delimited string as a string.
    /// </summary>
    /// <param name="input">The delimited string to parse</param>
    /// <param name="delimiter">The delimiter character</param>
    /// <param name="fieldIndex">The zero-based index of the field to retrieve</param>
    /// <returns>The field value as a string</returns>
    public static string GetFieldAsString(string input, char delimiter, int fieldIndex) {
        ReadOnlySpan<char> field = GetField(input.AsSpan(), delimiter, fieldIndex);
        return field.IsEmpty ? string.Empty : new string(field);
    }

    /// <summary>
    /// Parses up to 8 fields from a delimited string using stackalloc.
    /// Returns the number of fields found.
    /// </summary>
    /// <param name="input">The delimited string to parse</param>
    /// <param name="delimiter">The delimiter character</param>
    /// <param name="fields">Output span to receive the field ranges (max 8)</param>
    /// <returns>The number of fields found</returns>
    public static int ParseFields(ReadOnlySpan<char> input, char delimiter, Span<Range> fields) {
        int fieldCount = 0;
        int startPos = 0;
        int maxFields = fields.Length;

        for (int i = 0; i <= input.Length && fieldCount < maxFields; i++)
            if (i == input.Length || input[i] == delimiter) {
                fields[fieldCount++] = new Range(startPos, i);
                startPos = i + 1;
            }

        return fieldCount;
    }

    /// <summary>
    /// Removes specified characters from a string in a single pass.
    /// More efficient than chained Replace() calls.
    /// </summary>
    /// <param name="input">The input string</param>
    /// <param name="charsToRemove">Characters to remove</param>
    /// <returns>String with specified characters removed</returns>
    public static string RemoveChars(string? input, ReadOnlySpan<char> charsToRemove) {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        ReadOnlySpan<char> inputSpan = input.AsSpan();

        // First pass: count characters to keep
        int keepCount = 0;
        for (int i = 0; i < inputSpan.Length; i++)
            if (!charsToRemove.Contains(inputSpan[i]))
                keepCount++;

        if (keepCount == inputSpan.Length)
            return input; // No characters to remove

        if (keepCount == 0)
            return string.Empty;

        // Second pass: build result
        Span<char> buffer = keepCount <= 256
            ? stackalloc char[keepCount]
            : new char[keepCount];

        int writeIndex = 0;
        for (int i = 0; i < inputSpan.Length; i++)
            if (!charsToRemove.Contains(inputSpan[i]))
                buffer[writeIndex++] = inputSpan[i];

        return new string(buffer);
    }
}