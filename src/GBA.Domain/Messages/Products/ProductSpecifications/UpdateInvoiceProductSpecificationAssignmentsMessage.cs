using System;

namespace GBA.Domain.Messages.Products.ProductSpecifications;

public sealed class UpdateInvoiceProductSpecificationAssignmentsMessage {
    public UpdateInvoiceProductSpecificationAssignmentsMessage(Guid invoiceNetId) {
        InvoiceNetId = invoiceNetId;
    }

    public Guid InvoiceNetId { get; }
}