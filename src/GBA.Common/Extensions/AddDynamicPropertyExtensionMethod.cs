using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.CompilerServices;

namespace GBA.Common.Extensions;

public static class AddDynamicPropertyExtensionMethod {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddProperty(this ExpandoObject expando, string propertyName, object propertyValue) {
        // Direct indexer access is faster than ContainsKey + Add
        ((IDictionary<string, object>)expando)[propertyName] = propertyValue;
    }
}