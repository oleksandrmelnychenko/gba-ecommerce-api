using System;
using System.Globalization;

namespace GBA.Services.Infrastructure.Checkout;

public static class GeneratedSaleNumberPolicy {
    public static string GetNextValue(
        string prefix,
        string previousValue,
        int sequenceWidth) {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException(
                "The generated sale-number prefix is required.",
                nameof(prefix));
        if (sequenceWidth <= 0 || sequenceWidth > 18)
            throw new ArgumentOutOfRangeException(
                nameof(sequenceWidth),
                "The generated sale-number width must be between 1 and 18.");

        long previousSequence = 0;
        if (!string.IsNullOrEmpty(previousValue)) {
            if (!previousValue.StartsWith(prefix, StringComparison.Ordinal) ||
                previousValue.Length <= prefix.Length ||
                !long.TryParse(
                    previousValue.AsSpan(prefix.Length),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out previousSequence) ||
                previousSequence < 0)
                throw new InvalidOperationException(
                    "The previous generated sale number has an invalid format.");
        }

        long nextSequence = checked(previousSequence + 1);
        string sequence = nextSequence.ToString(
            CultureInfo.InvariantCulture);
        if (sequence.Length > sequenceWidth)
            throw new InvalidOperationException(
                "The generated sale-number sequence exceeded its configured width.");

        return prefix + sequence.PadLeft(sequenceWidth, '0');
    }
}
