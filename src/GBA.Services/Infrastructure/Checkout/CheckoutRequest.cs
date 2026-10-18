using System;

namespace GBA.Services.Infrastructure.Checkout;

public sealed class CheckoutRequest {
    public const int MaxOperationNameLength = 128;

    public CheckoutRequest(
        string operationName,
        CheckoutAttempt attempt,
        Guid principalNetUid,
        Guid clientNetUid,
        bool modeFlag) {
        ArgumentNullException.ThrowIfNull(attempt);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        if (operationName.Length > MaxOperationNameLength)
            throw new ArgumentException("The checkout operation name is too long.", nameof(operationName));
        if (principalNetUid == Guid.Empty)
            throw new ArgumentException("An authenticated principal is required.", nameof(principalNetUid));
        if (clientNetUid == Guid.Empty)
            throw new ArgumentException("A client identity is required.", nameof(clientNetUid));

        OperationName = operationName;
        OperationNetUid = attempt.IdempotencyKey;
        RequestFingerprint = attempt.RequestFingerprint;
        PrincipalNetUid = principalNetUid;
        ClientNetUid = clientNetUid;
        ModeFlag = modeFlag;
    }

    public Guid OperationNetUid { get; }

    public string OperationName { get; }

    public Guid PrincipalNetUid { get; }

    public Guid ClientNetUid { get; }

    public bool ModeFlag { get; }

    public byte[] RequestFingerprint { get; }

    public bool IsSameRequestAs(CheckoutLedgerEntry entry) {
        return entry != null &&
               entry.OperationNetUid == OperationNetUid &&
               string.Equals(entry.OperationName, OperationName, StringComparison.Ordinal) &&
               entry.PrincipalNetUid == PrincipalNetUid &&
               entry.ClientNetUid == ClientNetUid &&
               entry.ModeFlag == ModeFlag &&
               entry.RequestFingerprint != null &&
               entry.RequestFingerprint.AsSpan().SequenceEqual(RequestFingerprint);
    }
}
