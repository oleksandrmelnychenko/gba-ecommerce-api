using System;
using GBA.Common.Helpers;

namespace GBA.Domain.Messages.Supplies.Invoices;

public sealed class UploadProductSpecificationForInvoiceMessage {
    public UploadProductSpecificationForInvoiceMessage(
        string pathToFile,
        ProductSpecificationParseConfiguration parseConfiguration,
        Guid invoiceNetId,
        Guid userNetId) {
        PathToFile = pathToFile;
        ParseConfiguration = parseConfiguration;
        InvoiceNetId = invoiceNetId;
        UserNetId = userNetId;
    }

    public string PathToFile { get; }
    public ProductSpecificationParseConfiguration ParseConfiguration { get; }
    public Guid InvoiceNetId { get; }
    public Guid UserNetId { get; }
}