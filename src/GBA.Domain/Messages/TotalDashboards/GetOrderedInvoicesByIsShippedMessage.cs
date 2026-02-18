using GBA.Domain.EntityHelpers.TotalDashboards.SupplyInvoices;

namespace GBA.Domain.Messages.TotalDashboards;

public sealed class GetOrderedInvoicesByIsShippedMessage {
    public GetOrderedInvoicesByIsShippedMessage(
        TypeIsShippedInvoices type) {
        Type = type;
    }

    public TypeIsShippedInvoices Type { get; }
}