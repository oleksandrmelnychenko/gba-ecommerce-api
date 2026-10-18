using System;

namespace GBA.Services.Infrastructure.Checkout;

public sealed class CheckoutLedgerEntry {
    public Guid OperationNetUid { get; set; }

    public string OperationName { get; set; }

    public Guid PrincipalNetUid { get; set; }

    public Guid ClientNetUid { get; set; }

    public bool ModeFlag { get; set; }

    public byte[] RequestFingerprint { get; set; }

    public string ResponsePayload { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? CompletedUtc { get; set; }

    public bool IsCompleted => CompletedUtc.HasValue && !string.IsNullOrWhiteSpace(ResponsePayload);
}
