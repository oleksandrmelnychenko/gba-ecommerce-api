using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace GBA.Services.Infrastructure.Checkout;

public static class CheckoutRetry {
    public const int MaxAttempts = 3;

    private static readonly JsonSerializerOptions InputJsonOptions = new() {
        PropertyNamingPolicy = null,
        DictionaryKeyPolicy = null,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public static Action<TimeSpan> Wait { get; set; } = Thread.Sleep;

    public static TimeSpan Backoff(int attempt) =>
        TimeSpan.FromMilliseconds(25 * (1 << Math.Clamp(attempt - 1, 0, 4)) + Random.Shared.Next(0, 75));

    public static Func<T> AttemptInput<T>(T original) where T : class {
        if (original == null) return () => null;

        string snapshot = JsonSerializer.Serialize(original, InputJsonOptions);
        int attempts = 0;

        return () => Interlocked.Increment(ref attempts) == 1
            ? original
            : JsonSerializer.Deserialize<T>(snapshot, InputJsonOptions);
    }
}
