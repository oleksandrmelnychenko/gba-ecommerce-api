using System;

namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public class GetAllPaymentRegistersIsForRetailMessage {
    public GetAllPaymentRegistersIsForRetailMessage(Guid? organizationNetUid) {
        OrganizationNetUid = organizationNetUid;
    }

    public Guid? OrganizationNetUid { get; }
}