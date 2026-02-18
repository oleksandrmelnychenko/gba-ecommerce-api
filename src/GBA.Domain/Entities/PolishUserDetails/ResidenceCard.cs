using System;
using System.Collections.Generic;

namespace GBA.Domain.Entities.PolishUserDetails;

public sealed class ResidenceCard : EntityBase {
    public ResidenceCard() {
        UsersDetails = new HashSet<UserDetails>();
    }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public ICollection<UserDetails> UsersDetails { get; set; }
}