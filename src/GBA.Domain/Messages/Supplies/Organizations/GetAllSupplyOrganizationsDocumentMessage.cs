using System;

namespace GBA.Domain.Messages.Supplies.Organizations;

public sealed class GetAllSupplyOrganizationsDocumentMessage {
    public GetAllSupplyOrganizationsDocumentMessage(
        string value,
        Guid? organizationNetId,
        string saleInvoicesFolderPath) {
        Value = string.IsNullOrEmpty(value) ? string.Empty : value;
        OrganizationNetId = organizationNetId;
        SaleInvoicesFolderPath = saleInvoicesFolderPath;
    }

    public string SaleInvoicesFolderPath { get; }

    public string Value { get; }

    public Guid? OrganizationNetId { get; }
}