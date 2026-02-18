using System;

namespace GBA.Domain.Messages.Consignments.Infos;

public sealed class GetClientMovementConsignmentInfoFilteredMessage {
    public GetClientMovementConsignmentInfoFilteredMessage(
        Guid clientNetId,
        DateTime from,
        DateTime to,
        int limit,
        int offset,
        long[] organizationIds,
        string article) {
        ClientNetId = clientNetId;

        OrganizationIds = organizationIds;

        Article = article ?? string.Empty;

        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from.Date;

        To = (to.Year.Equals(1) ? DateTime.UtcNow.Date : to.Date).AddHours(23).AddMinutes(59).AddSeconds(59);

        Limit = limit <= 0 ? 40 : limit;

        Offset = offset < 0 ? 0 : offset;
    }

    public Guid ClientNetId { get; }
    public long[] OrganizationIds { get; }

    public string Article { get; }

    public DateTime From { get; }

    public DateTime To { get; }

    public int Limit { get; }

    public int Offset { get; }
}