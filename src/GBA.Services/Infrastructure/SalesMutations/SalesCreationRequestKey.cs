#nullable enable

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GBA.Domain.Entities.Sales;

namespace GBA.Services.Infrastructure.SalesMutations;

/// <summary>Builds and validates the immutable binding for an inbound sales-creation request.</summary>
public static class SalesCreationRequestKey {
    /// <summary>Parses the required HTTP idempotency key.</summary>
    /// <param name="value">Raw <c>Idempotency-Key</c> header value.</param>
    /// <param name="operationNetUid">Parsed non-empty operation identifier.</param>
    /// <returns><see langword="true"/> when the value is a non-empty UUID.</returns>
    public static bool TryParseHeader(string? value, out Guid operationNetUid) =>
        Guid.TryParse(value?.Trim(), out operationNetUid) && operationNetUid != Guid.Empty;

    /// <summary>Resolves the inbound key while preserving the existing explicit <c>Sale.NetUid</c> contract.</summary>
    /// <param name="headerValue">Raw optional <c>Idempotency-Key</c> header.</param>
    /// <param name="saleNetUid">Explicit per-attempt key carried by the existing sale DTO.</param>
    /// <param name="operationNetUid">Resolved non-empty operation identifier.</param>
    /// <returns>
    /// <see langword="true"/> when a valid header or body key exists. A present but invalid header
    /// always fails instead of silently falling back to the body.
    /// </returns>
    public static bool TryResolveInboundKey(
        string? headerValue,
        Guid saleNetUid,
        out Guid operationNetUid) {
        if (headerValue != null)
            return TryParseHeader(headerValue, out operationNetUid);

        operationNetUid = saleNetUid;
        return operationNetUid != Guid.Empty;
    }

    /// <summary>Creates a durable binding for one sales-creation attempt.</summary>
    /// <param name="operationNetUid">Explicit idempotency key supplied by the caller.</param>
    /// <param name="operationName">Stable sales-creation flow name.</param>
    /// <param name="principalNetUid">Authenticated or retail principal identifier.</param>
    /// <param name="clientNetUid">Client identifier whose sale is being created.</param>
    /// <param name="modeFlag">Flow-specific mode, such as workplace or full-payment mode.</param>
    /// <param name="sale">Inbound sale payload before any server-side mutation.</param>
    /// <param name="attachmentFingerprint">Optional SHA-256 of the inbound invoice attachment.</param>
    /// <returns>An immutable request binding with a canonical SHA-256 payload fingerprint.</returns>
    public static SalesCreationRequest Create(
        Guid operationNetUid,
        string operationName,
        Guid principalNetUid,
        Guid clientNetUid,
        bool modeFlag,
        Sale sale,
        byte[]? attachmentFingerprint = null) {
        if (operationNetUid == Guid.Empty)
            throw new ArgumentException("A non-empty idempotency key is required.", nameof(operationNetUid));
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        if (principalNetUid == Guid.Empty)
            throw new ArgumentException("A principal key is required.", nameof(principalNetUid));
        if (clientNetUid == Guid.Empty)
            throw new ArgumentException("A client key is required.", nameof(clientNetUid));
        ArgumentNullException.ThrowIfNull(sale);
        if (attachmentFingerprint is not null and not { Length: 32 })
            throw new ArgumentException(
                "An attachment fingerprint must be SHA-256.",
                nameof(attachmentFingerprint));

        JsonNode salePayload = JsonSerializer.SerializeToNode(sale) ??
            throw new ArgumentException("The sale payload cannot be serialized.", nameof(sale));
        if (salePayload is JsonObject saleObject &&
            saleObject["CustomersOwnTtn"] is JsonObject ttnObject)
            ttnObject.Remove("TtnPDFPath");
        JsonObject inboundPayload = new() {
            ["Sale"] = salePayload,
            ["AttachmentSha256"] = attachmentFingerprint == null
                ? null
                : Convert.ToHexString(attachmentFingerprint)
        };
        string canonicalPayload = Canonicalize(inboundPayload)!.ToJsonString();

        return new SalesCreationRequest {
            OperationNetUid = operationNetUid,
            OperationName = operationName.Trim(),
            PrincipalNetUid = principalNetUid,
            ClientNetUid = clientNetUid,
            ModeFlag = modeFlag,
            RequestFingerprint = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayload))
        };
    }

    /// <summary>Rejects reuse of an idempotency key for a different immutable request.</summary>
    /// <param name="request">Current inbound request binding.</param>
    /// <param name="entry">Existing durable ledger entry.</param>
    /// <exception cref="SalesCreationIdempotencyConflictException">
    /// Thrown when operation, principal, client, mode, or payload differs.
    /// </exception>
    public static void EnsureMatches(
        SalesCreationRequest request,
        SalesCreationLedgerEntry entry) {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(entry);

        bool fingerprintMatches = request.RequestFingerprint is { Length: 32 } &&
                                  entry.RequestFingerprint is { Length: 32 } &&
                                  CryptographicOperations.FixedTimeEquals(
                                      request.RequestFingerprint,
                                      entry.RequestFingerprint);
        if (entry.OperationNetUid != request.OperationNetUid ||
            !string.Equals(entry.OperationName, request.OperationName, StringComparison.Ordinal) ||
            entry.PrincipalNetUid != request.PrincipalNetUid ||
            entry.ClientNetUid != request.ClientNetUid ||
            entry.ModeFlag != request.ModeFlag ||
            !fingerprintMatches)
            throw new SalesCreationIdempotencyConflictException(
                "Idempotency-Key is already bound to a different sales creation request.");
    }

    private static JsonNode? Canonicalize(JsonNode? node) {
        if (node is JsonObject jsonObject) {
            JsonObject canonicalObject = new();
            foreach ((string propertyName, JsonNode? value) in jsonObject
                         .OrderBy(property => property.Key, StringComparer.Ordinal))
                canonicalObject[propertyName] = Canonicalize(value);
            return canonicalObject;
        }

        if (node is JsonArray jsonArray) {
            JsonArray canonicalArray = new();
            foreach (JsonNode? item in jsonArray)
                canonicalArray.Add(Canonicalize(item));
            return canonicalArray;
        }

        return node == null ? null : JsonNode.Parse(node.ToJsonString());
    }
}
