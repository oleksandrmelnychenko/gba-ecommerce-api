using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace GBA.Services.Infrastructure;

public static class SalesMutationRequestKey {
    public const string HeaderName = "Idempotency-Key";
    public const string BodyPropertyName = "OperationNetUid";

    public readonly record struct PreparedSalesMutation(Guid OperationNetUid, string Payload);

    public static Guid AddTo(HttpRequestMessage request, string operationName, string frozenPayload) {
        ArgumentNullException.ThrowIfNull(request);
        Guid operationNetUid = Create(operationName, frozenPayload);
        AddTo(request, operationNetUid);
        return operationNetUid;
    }

    public static void AddTo(HttpRequestMessage request, Guid operationNetUid) {
        ArgumentNullException.ThrowIfNull(request);
        if (operationNetUid == Guid.Empty)
            throw new ArgumentException("A non-empty sales mutation operation key is required.", nameof(operationNetUid));

        request.Headers.Remove(HeaderName);
        request.Headers.TryAddWithoutValidation(HeaderName, operationNetUid.ToString("D"));
    }

    public static Guid Create(string operationName, string frozenPayload) {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(frozenPayload);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{operationName.Trim()}\n{frozenPayload}"));
        byte[] guidBytes = new byte[16];
        Buffer.BlockCopy(hash, 0, guidBytes, 0, guidBytes.Length);
        Guid operationNetUid = new(guidBytes);

        if (operationNetUid == Guid.Empty)
            throw new InvalidOperationException("Unable to create a non-empty sales mutation operation key.");

        return operationNetUid;
    }

    public static PreparedSalesMutation Prepare(string operationName, string frozenPayload) {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(frozenPayload);

        JsonObject payload = ParsePayload(frozenPayload);
        string keyPayload = payload.ToJsonString();
        return AddOperationKey(payload, Create(operationName, keyPayload));
    }

    public static PreparedSalesMutation Prepare(
        string operationName,
        string frozenPayload,
        Guid operationNetUid) {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(frozenPayload);
        if (operationNetUid == Guid.Empty)
            throw new ArgumentException(
                "A non-empty sales mutation operation key is required.",
                nameof(operationNetUid));

        return AddOperationKey(ParsePayload(frozenPayload), operationNetUid);
    }

    private static JsonObject ParsePayload(string frozenPayload) {
        JsonObject payload = JsonNode.Parse(frozenPayload) as JsonObject ??
            throw new ArgumentException("A sales mutation payload must be a JSON object.", nameof(frozenPayload));

        foreach (string propertyName in payload
                     .Select(property => property.Key)
                     .Where(propertyName => string.Equals(
                         propertyName,
                         BodyPropertyName,
                         StringComparison.OrdinalIgnoreCase))
                     .ToArray())
            payload.Remove(propertyName);

        return payload;
    }

    private static PreparedSalesMutation AddOperationKey(
        JsonObject payload,
        Guid operationNetUid) {
        payload[BodyPropertyName] = JsonValue.Create(operationNetUid);
        return new PreparedSalesMutation(operationNetUid, payload.ToJsonString());
    }
}