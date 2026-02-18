using System;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public sealed class GetAllPaymentRegistersMessage {
    public GetAllPaymentRegistersMessage(PaymentRegisterType? type, string value, Guid? organizationNetId) {
        Type = type;

        Value = value;

        OrganizationNetId = organizationNetId;
    }

    public PaymentRegisterType? Type { get; set; }

    public string Value { get; set; }

    public Guid? OrganizationNetId { get; set; }
}