using System;

namespace GBA.Domain.Messages.DepreciatedOrders;

public sealed class AddNewDepreciatedOrderFromActReconciliationItemMessage {
    public AddNewDepreciatedOrderFromActReconciliationItemMessage(
        Guid itemNetId,
        Guid storageNetId,
        Guid organizationNetId,
        Guid userNetId,
        double qty,
        string comment,
        string reason,
        DateTime fromDate
    ) {
        ItemNetId = itemNetId;

        StorageNetId = storageNetId;

        OrganizationNetId = organizationNetId;

        UserNetId = userNetId;

        Qty = qty;

        Comment = comment;

        Reason = reason;

        FromDate = fromDate;
    }

    public Guid ItemNetId { get; }

    public Guid StorageNetId { get; }

    public Guid OrganizationNetId { get; }

    public Guid UserNetId { get; }

    public double Qty { get; }

    public string Comment { get; }

    public string Reason { get; }

    public DateTime FromDate { get; }
}