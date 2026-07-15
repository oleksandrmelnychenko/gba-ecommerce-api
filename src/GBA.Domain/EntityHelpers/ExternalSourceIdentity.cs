#nullable enable

using System;

namespace GBA.Domain.EntityHelpers;

/// <summary>Identifies one entity in exactly one external source system.</summary>
public sealed class ExternalSourceIdentity {
    private ExternalSourceIdentity(
        string system,
        byte[]? externalId,
        long? externalCode) {
        System = system;
        ExternalId = externalId;
        ExternalCode = externalCode;

        string idPart = externalId is { Length: > 0 }
            ? $"id-{Convert.ToHexString(externalId)}"
            : string.Empty;
        string codePart = externalCode.HasValue
            ? $"code-{externalCode.Value}"
            : string.Empty;
        Value = $"{system}:{idPart}{(idPart.Length > 0 && codePart.Length > 0 ? "|" : string.Empty)}{codePart}";
    }

    /// <summary>The normalized source-system name.</summary>
    public string System { get; }

    /// <summary>The external binary identifier, when supplied by the source.</summary>
    public byte[]? ExternalId { get; }

    /// <summary>The external numeric code, when supplied by the source.</summary>
    public long? ExternalCode { get; }

    /// <summary>The canonical exact identity used in cache and search documents.</summary>
    public string Value { get; }

    /// <summary>Returns whether this identity belongs to Fenix.</summary>
    public bool IsFenix => string.Equals(System, "fenix", StringComparison.Ordinal);

    /// <summary>
    /// Creates an exact identity only when one and only one source system is present.
    /// Both ID and code are retained because either can distinguish same-provider sources.
    /// </summary>
    public static bool TryCreate(
        byte[]? fenixId,
        long? fenixCode,
        byte[]? amgId,
        long? amgCode,
        out ExternalSourceIdentity? identity) {
        bool hasFenix = fenixId is { Length: > 0 } || fenixCode.HasValue;
        bool hasAmg = amgId is { Length: > 0 } || amgCode.HasValue;

        if (hasFenix == hasAmg) {
            identity = null;
            return false;
        }

        identity = hasFenix
            ? new ExternalSourceIdentity("fenix", fenixId, fenixCode)
            : new ExternalSourceIdentity("amg", amgId, amgCode);
        return true;
    }

    /// <summary>Compares exact canonical identities.</summary>
    public bool EqualsExact(ExternalSourceIdentity? other) {
        return other != null && string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
