using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace GBA.Common.Helpers;

/// <summary>
/// Shared StringBuilder pool to reduce allocations for SQL query building.
/// </summary>
public static class StringBuilderPool {
    private static readonly ObjectPool<StringBuilder> Pool =
        new DefaultObjectPoolProvider().CreateStringBuilderPool(
            initialCapacity: 4096,
            maximumRetainedCapacity: 32768);

    /// <summary>
    /// Gets a StringBuilder from the pool.
    /// </summary>
    public static StringBuilder Get() => Pool.Get();

    /// <summary>
    /// Returns a StringBuilder to the pool.
    /// </summary>
    public static void Return(StringBuilder builder) {
        builder.Clear();
        Pool.Return(builder);
    }

    /// <summary>
    /// Gets a StringBuilder, executes action, returns it to pool and returns the string result.
    /// </summary>
    public static string UseAndReturn(System.Action<StringBuilder> action) {
        StringBuilder sb = Pool.Get();
        try {
            action(sb);
            return sb.ToString();
        } finally {
            sb.Clear();
            Pool.Return(sb);
        }
    }
}
