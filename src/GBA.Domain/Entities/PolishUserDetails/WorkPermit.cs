using System;
using System.Collections.Generic;

namespace GBA.Domain.Entities.PolishUserDetails;

public sealed class WorkPermit : EntityBase {
    public WorkPermit() {
        UsersDetails = new HashSet<UserDetails>();
    }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public ICollection<UserDetails> UsersDetails { get; set; }
}