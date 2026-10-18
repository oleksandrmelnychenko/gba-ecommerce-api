using System;
using System.Data;

namespace GBA.Services.Infrastructure.Checkout;

public enum CheckoutClaim {
    Claimed = 1,
    AlreadyRecorded = 0,
    Busy = -1
}

public interface ICheckoutLedger {
    CheckoutLedgerEntry Find(IDbConnection connection, Guid operationNetUid);

    CheckoutClaim TryClaim(IDbConnection connection, CheckoutRequest request);

    void Complete(IDbConnection connection, CheckoutRequest request, string responsePayload);
}
