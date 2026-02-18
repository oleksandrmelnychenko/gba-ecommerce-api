using System;

namespace GBA.Domain.EntityHelpers.Accounting;

public sealed class JoinService {
    public JoinService() { }

    public JoinService(long id, JoinServiceType type) {
        Id = id;

        Type = type;

        Paid = false;
    }

    public JoinService(long id, JoinServiceType type, bool paid) {
        Id = id;

        Type = type;

        Paid = paid;
    }

    public long Id { get; set; }

    public JoinServiceType Type { get; set; }

    public bool Paid { get; set; }

    public DateTime FromDate { get; set; }

    public string ResponsibleName { get; set; }
}